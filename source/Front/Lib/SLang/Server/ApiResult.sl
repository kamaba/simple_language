import Std;
import Core;

namespace SLang
{
    # ============================================================================
    # SLang/Server/ApiResult.sl — 统一 JSON 返回体（P1）
    # 设计契约：SERVER_DESIGN.md §4.3 / §6.7
    #
    # 输出形态：{"code":0,"message":"ok","data":{...}}
    # 注意：类字段名 payload（data 是关键字），输出 JSON 键名固定 "data"——
    # 组包一律走 toJson() 手工组树（键序 code→message→data 稳定，不依赖
    # fromData 对本类字段名的序列化）；嵌套 payload 经 BaseJson.fromData 递归。
    # ============================================================================

    public class ApiResult
    {
        # 0 = 成功；非 0 业务错误码（用户自定义段 ≥ 1000）
        public Int32 code = 0
        # 描述文本
        public string message = "ok"
        # 任意负载（Serializable 对象 / null；data 是关键字故字段名用 payload）
        public object payload = null

        # 无参构造显式落默认值（跨模块 string 字段默认值消费侧不可靠，
        # [2026-10-17] 复盘 ②；本类多经 ok/fail 工厂构造，此处兜底）
        _init_()
        {
            this.code = 0
            this.message = "ok"
            this.payload = null
        }

        # 成功封装（payload 可为 null）
        public static ApiResult ok( object payload )
        {
            ApiResult r = ApiResult()
            r.code = 0
            r.message = "ok"
            r.payload = payload
            ret r
        }

        # 失败封装（code 业务码；message 描述）
        public static ApiResult fail( Int32 code, string message )
        {
            ApiResult r = ApiResult()
            r.code = code
            r.message = message
            r.payload = null
            ret r
        }

        # 序列化（键序 code → message → data；payload 经 fromData 递归）
        public string toJson()
        {
            string msg = this.message
            if msg == null
            {
                msg = ""
            }
            # message 为框架/用户拼接文本，非 JSON 转义源（v1 约定：不含引号/反斜杠）
            string payloadJson = "null"
            if this.payload != null
            {
                BaseJson j = BaseJson.fromData( this.payload )
                string s = j.toJson()
                if SystemStringLength( s ) > 0
                {
                    payloadJson = s
                }
            }
            ret "{\"code\":" + this.code.toString() + ",\"message\":\"" + msg + "\",\"data\":" + payloadJson + "}"
        }
    }
}
