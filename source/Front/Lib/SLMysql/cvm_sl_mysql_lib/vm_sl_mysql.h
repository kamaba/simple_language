#ifndef VM_SL_MYSQL_H
#define VM_SL_MYSQL_H

/* ==========================================================================
 * vm_sl_mysql.h —— SLMysql 模块原生层导出声明（sl_mysql_lib.dll）
 * --------------------------------------------------------------------------
 * 本库是 MySQL Connector/C（libmysqlclient，见本目录 mysqlclient/）的一层
 * **扁平 C ABI 包装**：把 MYSQL / MYSQL_RES 这类不透明结构体收敛为 SL 侧可
 * 承载的整数句柄（Int64），把所有数据都收敛为 SL 侧可直接搬运的
 * 标量（bool / Int32 / Int64 / string），从而让 SLMysql.sl 能通过
 *
 *     @DllStaticImport( "sl_mysql_lib", "slm_query" )
 *     static bool query( Int64 conn, string sql ) { ret false }
 *
 * 走 opcode 118 静态绑定直调，无需任何 Adaptor / 委托转发。
 *
 * 导出命名约定：统一前缀 slm_（SimpleLanguage MySQL）。
 * 调用约定：x64 cdecl（Windows x64 唯一），与 cvm sl_ffi_call 的
 *          INT 类（含指针 / utf8）/ DBL 类分类调度对齐。
 *
 * 参数个数上限：6（SL_FFI_MAX_ARGS），所有导出函数均满足。
 *
 * 返回值 / 字符串内存约定：
 *   - const char* 返回值一律指向 libmysqlclient 内部缓冲（随结果集或连接
 *     生命周期有效），或本库持有的线程局部缓冲区，**调用方不得 free**；
 *     SL 侧（VM）在返回瞬间就会把内容拷进自己的字符串，所以只需保证
 *     "返回那一刻有效" 即可。NULL 表示无值（VM 侧映射为 null）。
 *   - 返回 NULL 的场合：句柄无效 / 索引越界 / 该列为 SQL NULL。
 * ========================================================================== */

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/* 在所有 Connector 头之后导出的问题不存在，这里只放前向声明 + 宏 */
#if defined(_WIN32) && defined(_MSC_VER)
#  define SLM_API __declspec(dllexport)
#else
#  define SLM_API
#endif

/* ------------------------------------------------------------------ */
/* 生命周期 / 连接                                                     */
/* ------------------------------------------------------------------ */

/* 创建一个连接对象；返回句柄，0 = 分配失败。 */
SLM_API int64_t slm_init(void);

/* 关闭并释放连接；句柄随后不得再使用（重复调用安全）。 */
SLM_API void    slm_close(int64_t conn);

/* 连接服务器。port <= 0 时取默认 3306；host/user/password/database
   传空串等价于传 NULL（走服务器端默认 / 免密）。成功返回非 0。 */
SLM_API int32_t slm_connect(int64_t conn, const char* host, const char* user,
                            const char* password, const char* database, int32_t port);

/* 切换当前数据库。 */
SLM_API int32_t slm_select_db(int64_t conn, const char* database);

/* 设置连接字符集（如 "utf8mb4"）并同步发往服务端。 */
SLM_API int32_t slm_set_charset(int64_t conn, const char* charset);

/* 探活：断开时会自动重连。 */
SLM_API int32_t slm_ping(int64_t conn);

/* ------------------------------------------------------------------ */
/* 事务                                                               */
/* ------------------------------------------------------------------ */

SLM_API int32_t slm_autocommit(int64_t conn, int32_t on);
SLM_API int32_t slm_commit(int64_t conn);
SLM_API int32_t slm_rollback(int64_t conn);

/* ------------------------------------------------------------------ */
/* 诊断                                                               */
/* ------------------------------------------------------------------ */

/* 最近一次调用的错误码，0 = 无错。 */
SLM_API int32_t      slm_errno(int64_t conn);
/* 最近一次调用的错误文本，空串表示无错。 */
SLM_API const char*  slm_error(int64_t conn);
/* 客户端库版本号（如 60111 -> 6.1.11）。 */
SLM_API int64_t      slm_client_version(void);
/* 客户端库版本文本。 */
SLM_API const char*  slm_client_info(void);

/* ------------------------------------------------------------------ */
/* 执行                                                               */
/* ------------------------------------------------------------------ */

/* 执行一条 SQL（走 mysql_real_query，二进制安全，不受内嵌 '\0' 影响）。 */
SLM_API int32_t slm_query(int64_t conn, const char* sql);

/* 取走整个结果集到客户端缓冲（-blocking），返回结果集句柄，0 = 无结果集。 */
SLM_API int64_t slm_store_result(int64_t conn);

/* 逐行流式取结果（结果集会占用服务端资源，必须取完或 free）。 */
SLM_API int64_t slm_use_result(int64_t conn);

/* 上一条语句影响的行数；-1 = 出错，数据时也可能返回 >0。 */
SLM_API int64_t slm_affected_rows(int64_t conn);
/* 上一条 INSERT 产生的 AUTO_INCREMENT 值，0 = 无。 */
SLM_API int64_t slm_insert_id(int64_t conn);
/* 上一条语句结果集的列数（DML 为 0）。 */
SLM_API int32_t slm_field_count(int64_t conn);

/* ------------------------------------------------------------------ */
/* 结果集                                                             */
/* ------------------------------------------------------------------ */

/* 释放结果集；句柄随后失效（重复调用安全）。 */
SLM_API void        slm_free_result(int64_t res);
/* 行数（use_result 模式下返回未读数，随读取递增）。 */
SLM_API int64_t     slm_num_rows(int64_t res);
/* 列数。 */
SLM_API int32_t     slm_num_fields(int64_t res);
/* 列名；索引越界返回 NULL。 */
SLM_API const char* slm_field_name(int64_t res, int32_t index);
/* 列类型（enum_field_types，见 mysql_com.h）。 */
SLM_API int32_t     slm_field_type(int64_t res, int32_t index);

/* ------------------------------------------------------------------ */
/* 行游标                                                             */
/* ------------------------------------------------------------------ */

/* 推进到下一行。返回非 0 表示本行有效（可随后取列值），0 = 已到末尾。
   内部由本库记住当前行，因此同一结果集可以并发混用在多个线程上。 */
SLM_API int32_t     slm_fetch_row(int64_t res);

/* 取当前行第 index 列的文本值；SQL NULL / 越界 / 无当前行返回 NULL。
   指针由结果集持有，slm_fetch_row 或 slm_free_result 后失效。 */
SLM_API const char* slm_row_value(int64_t res, int32_t index);

/* 当前行第 index 列的字节长度；SQL NULL / 越界返回 0。 */
SLM_API int64_t     slm_row_length(int64_t res, int32_t index);

/* 当前行第 index 列是否为 SQL NULL。 */
SLM_API int32_t     slm_row_is_null(int64_t res, int32_t index);

/* ------------------------------------------------------------------ */
/* 转义                                                               */
/* ------------------------------------------------------------------ */

/* 按连接字符集转义（不含两侧引号），结果在本库线程局部缓冲中，
   下一次同线程的 slm_escape / slm_error 之前有效。 */
SLM_API const char* slm_escape(int64_t conn, const char* text);

#ifdef __cplusplus
}
#endif

#endif /* VM_SL_MYSQL_H */
