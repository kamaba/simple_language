/*
 * sl_mysql_lib.c —— SLMysql 模块原生实现（产物 sl_mysql_lib.dll）
 * ==========================================================================
 * 位于 MySQL Connector/C（libmysqlclient）之上的一层**扁平 C ABI 包装**，
 * 供 SLMysql.sl 通过 @DllStaticImport 静态绑定直接调用（opcode 118）：
 *
 *     @DllStaticImport( "sl_mysql_lib", "slm_query" )
 *     static bool query( Int64 conn, string sql ) { ret false }
 *
 * 设计取舍：
 *   1) **句柄化**：MYSQL* / MYSQL_RES* 从不在 SL 侧出现，一律换成 Int64 句柄
 *      （x64 下指针与 int64 等宽），避开 Ptr 无法由编译器推导 FFI sig 的问题。
 *   2) **静态链接 mysqlclient.lib**：官方 Connector/C 除动态 libmysql.dll 外
 *      还提供静态 mysqlclient.lib。这里选静态，使 sl_mysql_lib.dll 成为自包含
 *      产物——工程导出流水线（`vmDlls`）只会把这一个 DLL 拷到 module.json 同
 *      目录，若再依赖一个外部 libmysql.dll，Windows 默认 DLL 搜索序（不搜索
 *      加载方 DLL 自身所在目录）会导致运行期找不到依赖。
 *      静态链接无需显式 mysql_library_init：mysql.h 原文注释明确
 *      "mysql_server_init() is called by mysql_init()"。
 *   3) **行游标状态外置**：MYSQL_ROW 没有句柄，若把它直接暴露给 SL，就要求
 *      SL 侧严格"先取值再推进"。这里用一个 result -> row 的小型旁路表
 *      （key 为 MYSQL_RES*）把"当前行"记在本库这边，SL 侧因此可以先
 *      fetch_row 再随意按列取值，与 Python DB-API 的游标语义一致。
 *
 * 线程安全：每个 MYSQL / MYSQL_RES 实例本身由调用方串行使用；旁路表用
 *          临界区保护；转义缓冲为线程局部。
 */

#include "vm_sl_mysql.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <stdlib.h>
#include <string.h>

#include <mysql.h>
#include <errmsg.h>

/* ========================================================================
 * 句柄与工具
 * ======================================================================== */

static MYSQL*     to_conn( int64_t h ) { return (MYSQL*)(void*)(intptr_t)h; }
static MYSQL_RES* to_res ( int64_t h ) { return (MYSQL_RES*)(void*)(intptr_t)h; }
static int64_t    to_handle( const void* p ) { return (int64_t)(intptr_t)p; }

/* 空串归一化为 NULL：让 SL 侧可以用 "" 表示"不指定"。 */
static const char* opt_str( const char* s ) { return ( s != NULL && s[0] != '\0' ) ? s : NULL; }

/* ========================================================================
 * 当前行旁路表（MYSQL_RES* -> MYSQL_ROW）
 * ======================================================================== */

typedef struct _SLRowSlot
{
    MYSQL_RES*          res;
    MYSQL_ROW           row;       /* NULL = 无当前行（未 fetch / 已到末尾） */
    unsigned long*      lengths;   /* 与 row 同步；NULL 表示无长度信息 */
    unsigned int        count;
    struct _SLRowSlot*  next;
} SLRowSlot;

static SLRowSlot*  s_slots = NULL;
static CRITICAL_SECTION s_lock;

static SLRowSlot* slot_find( MYSQL_RES* res, int create )
{
    SLRowSlot* prev = NULL;
    SLRowSlot* cur  = s_slots;

    while ( cur != NULL )
    {
        if ( cur->res == res ) { return cur; }
        prev = cur;
        cur  = cur->next;
    }
    if ( !create ) { return NULL; }

    cur = (SLRowSlot*)calloc( 1, sizeof(SLRowSlot) );
    if ( cur == NULL ) { return NULL; }
    cur->res = res;
    if ( prev != NULL ) { prev->next = cur; } else { s_slots = cur; }
    return cur;
}

static void slot_bind( MYSQL_RES* res, MYSQL_ROW row, unsigned long* lengths, unsigned int count )
{
    SLRowSlot* s;
    EnterCriticalSection( &s_lock );
    s = slot_find( res, 1 );
    if ( s != NULL )
    {
        s->row     = row;
        s->lengths = lengths;
        s->count   = count;
    }
    LeaveCriticalSection( &s_lock );
}

static void slot_release( MYSQL_RES* res )
{
    SLRowSlot* prev = NULL;
    SLRowSlot* cur;

    EnterCriticalSection( &s_lock );
    cur = s_slots;
    while ( cur != NULL )
    {
        if ( cur->res == res )
        {
            if ( prev != NULL ) { prev->next = cur->next; } else { s_slots = cur->next; }
            free( cur );
            break;
        }
        prev = cur;
        cur  = cur->next;
    }
    LeaveCriticalSection( &s_lock );
}

BOOL WINAPI DllMain( HINSTANCE instance, DWORD reason, LPVOID reserved )
{
    (void)instance; (void)reserved;
    switch ( reason )
    {
        case DLL_PROCESS_ATTACH:
            InitializeCriticalSection( &s_lock );
            break;
        case DLL_PROCESS_DETACH:
            DeleteCriticalSection( &s_lock );
            break;
        default:
            break;
    }
    return TRUE;
}

/* ========================================================================
 * 线程局部临时缓冲（转义结果）
 * ======================================================================== */

static __declspec(thread) char*  t_buf = NULL;
static __declspec(thread) size_t t_cap = 0;

static char* scratch( size_t need )
{
    if ( t_cap < need )
    {
        size_t cap = ( t_cap != 0 ) ? t_cap : 256;
        char*  p;
        while ( cap < need ) { cap *= 2; }
        p = (char*)realloc( t_buf, cap );
        if ( p == NULL ) { return NULL; }
        t_buf = p;
        t_cap = cap;
    }
    return t_buf;
}

/* ========================================================================
 * 生命周期 / 连接
 * ======================================================================== */

SLM_API int64_t slm_init( void )
{
    /* 静态链接下 mysql_init 内部会自动完成 mysql_server_init()。 */
    return to_handle( mysql_init( NULL ) );
}

SLM_API void slm_close( int64_t conn )
{
    MYSQL* c = to_conn( conn );
    if ( c != NULL ) { mysql_close( c ); }
}

SLM_API int32_t slm_connect( int64_t conn, const char* host, const char* user,
                             const char* password, const char* database, int32_t port )
{
    MYSQL*       c = to_conn( conn );
    MYSQL*       ok;
    unsigned int p;

    if ( c == NULL ) { return 0; }
    p  = ( port > 0 ) ? (unsigned int)port : MYSQL_PORT_DEFAULT;
    ok = mysql_real_connect( c,
                             opt_str( host ),
                             opt_str( user ),
                             opt_str( password ),
                             opt_str( database ),
                             p, NULL, 0 );
    return ok != NULL ? 1 : 0;
}

SLM_API int32_t slm_select_db( int64_t conn, const char* database )
{
    MYSQL* c = to_conn( conn );
    if ( c == NULL ) { return 0; }
    return mysql_select_db( c, opt_str( database ) ) == 0 ? 1 : 0;
}

SLM_API int32_t slm_set_charset( int64_t conn, const char* charset )
{
    MYSQL* c = to_conn( conn );
    if ( c == NULL || charset == NULL || charset[0] == '\0' ) { return 0; }
    return mysql_set_character_set( c, charset ) == 0 ? 1 : 0;
}

SLM_API int32_t slm_ping( int64_t conn )
{
    MYSQL* c = to_conn( conn );
    if ( c == NULL ) { return 0; }
    return mysql_ping( c ) == 0 ? 1 : 0;
}

/* ========================================================================
 * 事务
 * ======================================================================== */

SLM_API int32_t slm_autocommit( int64_t conn, int32_t on )
{
    MYSQL* c = to_conn( conn );
    if ( c == NULL ) { return 0; }
    return mysql_autocommit( c, on != 0 ? 1 : 0 ) == 0 ? 1 : 0;
}

SLM_API int32_t slm_commit( int64_t conn )
{
    MYSQL* c = to_conn( conn );
    if ( c == NULL ) { return 0; }
    return mysql_commit( c ) == 0 ? 1 : 0;
}

SLM_API int32_t slm_rollback( int64_t conn )
{
    MYSQL* c = to_conn( conn );
    if ( c == NULL ) { return 0; }
    return mysql_rollback( c ) == 0 ? 1 : 0;
}

/* ========================================================================
 * 诊断
 * ======================================================================== */

SLM_API int32_t slm_errno( int64_t conn )
{
    MYSQL* c = to_conn( conn );
    return ( c == NULL ) ? CR_UNKNOWN_ERROR : (int32_t)mysql_errno( c );
}

SLM_API const char* slm_error( int64_t conn )
{
    MYSQL* c = to_conn( conn );
    return ( c == NULL ) ? "invalid connection handle" : mysql_error( c );
}

SLM_API int64_t slm_client_version( void )
{
    return (int64_t)mysql_get_client_version();
}

SLM_API const char* slm_client_info( void )
{
    return mysql_get_client_info();
}

/* ========================================================================
 * 执行
 * ======================================================================== */

SLM_API int32_t slm_query( int64_t conn, const char* sql )
{
    MYSQL* c = to_conn( conn );
    unsigned long len;

    if ( c == NULL || sql == NULL ) { return 0; }
    len = (unsigned long)strlen( sql );
    return mysql_real_query( c, sql, len ) == 0 ? 1 : 0;
}

SLM_API int64_t slm_store_result( int64_t conn )
{
    MYSQL*     c = to_conn( conn );
    MYSQL_RES* r;

    if ( c == NULL ) { return 0; }
    r = mysql_store_result( c );
    if ( r == NULL ) { return 0; }
    slot_bind( r, NULL, NULL, mysql_num_fields( r ) );
    return to_handle( r );
}

SLM_API int64_t slm_use_result( int64_t conn )
{
    MYSQL*     c = to_conn( conn );
    MYSQL_RES* r;

    if ( c == NULL ) { return 0; }
    r = mysql_use_result( c );
    if ( r == NULL ) { return 0; }
    slot_bind( r, NULL, NULL, mysql_num_fields( r ) );
    return to_handle( r );
}

SLM_API int64_t slm_affected_rows( int64_t conn )
{
    MYSQL* c = to_conn( conn );
    return ( c == NULL ) ? 0 : (int64_t)mysql_affected_rows( c );
}

SLM_API int64_t slm_insert_id( int64_t conn )
{
    MYSQL* c = to_conn( conn );
    return ( c == NULL ) ? 0 : (int64_t)mysql_insert_id( c );
}

SLM_API int32_t slm_field_count( int64_t conn )
{
    MYSQL* c = to_conn( conn );
    return ( c == NULL ) ? 0 : (int32_t)mysql_field_count( c );
}

/* ========================================================================
 * 结果集
 * ======================================================================== */

SLM_API void slm_free_result( int64_t res )
{
    MYSQL_RES* r = to_res( res );
    if ( r == NULL ) { return; }
    slot_release( r );
    mysql_free_result( r );
}

SLM_API int64_t slm_num_rows( int64_t res )
{
    MYSQL_RES* r = to_res( res );
    return ( r == NULL ) ? 0 : (int64_t)mysql_num_rows( r );
}

SLM_API int32_t slm_num_fields( int64_t res )
{
    MYSQL_RES* r = to_res( res );
    return ( r == NULL ) ? 0 : (int32_t)mysql_num_fields( r );
}

SLM_API const char* slm_field_name( int64_t res, int32_t index )
{
    MYSQL_RES* r = to_res( res );
    MYSQL_FIELD* f;

    if ( r == NULL || index < 0 || (unsigned int)index >= mysql_num_fields( r ) ) { return NULL; }
    f = mysql_fetch_field_direct( r, (unsigned int)index );
    return ( f == NULL ) ? NULL : f->name;
}

SLM_API int32_t slm_field_type( int64_t res, int32_t index )
{
    MYSQL_RES* r = to_res( res );
    MYSQL_FIELD* f;

    if ( r == NULL || index < 0 || (unsigned int)index >= mysql_num_fields( r ) ) { return 0; }
    f = mysql_fetch_field_direct( r, (unsigned int)index );
    return ( f == NULL ) ? 0 : (int32_t)f->type;
}

/* ========================================================================
 * 行游标
 * ======================================================================== */

SLM_API int32_t slm_fetch_row( int64_t res )
{
    MYSQL_RES*     r = to_res( res );
    unsigned long* lengths = NULL;
    MYSQL_ROW      row;
    unsigned int   fields;

    if ( r == NULL ) { return 0; }

    row    = mysql_fetch_row( r );
    fields = mysql_num_fields( r );
    if ( row != NULL ) { lengths = mysql_fetch_lengths( r ); }

    slot_bind( r, row, lengths, fields );
    return row != NULL ? 1 : 0;
}

SLM_API const char* slm_row_value( int64_t res, int32_t index )
{
    MYSQL_RES* r = to_res( res );
    SLRowSlot* s;

    if ( r == NULL ) { return NULL; }

    EnterCriticalSection( &s_lock );
    s = slot_find( r, 0 );
    LeaveCriticalSection( &s_lock );

    if ( s == NULL || s->row == NULL ) { return NULL; }
    if ( index < 0 || (unsigned int)index >= s->count ) { return NULL; }
    return s->row[index];
}

SLM_API int64_t slm_row_length( int64_t res, int32_t index )
{
    MYSQL_RES* r = to_res( res );
    SLRowSlot* s;

    if ( r == NULL ) { return 0; }

    EnterCriticalSection( &s_lock );
    s = slot_find( r, 0 );
    LeaveCriticalSection( &s_lock );

    if ( s == NULL || s->row == NULL || s->lengths == NULL ) { return 0; }
    if ( index < 0 || (unsigned int)index >= s->count ) { return 0; }
    return (int64_t)s->lengths[index];
}

SLM_API int32_t slm_row_is_null( int64_t res, int32_t index )
{
    return slm_row_value( res, index ) == NULL ? 1 : 0;
}

/* ========================================================================
 * 转义
 * ======================================================================== */

SLM_API const char* slm_escape( int64_t conn, const char* text )
{
    MYSQL* c = to_conn( conn );
    size_t n;
    char*  buf;

    if ( c == NULL || text == NULL ) { return NULL; }

    n   = strlen( text );
    buf = scratch( n * 2 + 1 );
    if ( buf == NULL ) { return NULL; }

    mysql_real_escape_string( c, buf, text, (unsigned long)n );
    return buf;
}
