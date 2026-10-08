# HlslLibrary —— 内置 HLSL 着色器库（Unity Shader Library 风格）
#
# 组织方式完全照 Unity：
#   Common.hlsl  全局 cbuffer（UnityPerFrame / UnityPerDraw / UnityPerMaterial / UnityLighting）
#                + 顶点结构 Attributes / Varyings + 空间变换函数
#   Lit          不透明 PBR（简化 GGX + 环境光近似）
#   Unlit        无光照
#   DepthOnly    深度预通道（Z-Prepass / ShadowCaster）
#   Skybox       天空盒
#   PostProcess  全屏后处理（Tonemap + 暗角）
#
# 源码以 HLSL 字符串形式保存，运行时交给 HlslShader / ShaderModule 编译

public class HlslLibrary
{
    # ── Common.hlsl ─────────────────────────────────────
    public static string common()
    {
        string s = "#ifndef SL_COMMON_INCLUDED" + "\n"
        s = s + "#define SL_COMMON_INCLUDED" + "\n"
        s = s + "" + "\n"
        s = s + "cbuffer UnityPerFrame : register(b0)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4x4 unity_MatrixVP;" + "\n"
        s = s + "    float4x4 unity_MatrixV;" + "\n"
        s = s + "    float4   _TimeParams;" + "\n"
        s = s + "    float3   _WorldSpaceCameraPos;" + "\n"
        s = s + "    float    _Pad0;" + "\n"
        s = s + "    float4   _ScreenParams;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "cbuffer UnityPerDraw : register(b1)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4x4 unity_ObjectToWorld;" + "\n"
        s = s + "    float4x4 unity_WorldToObject;" + "\n"
        s = s + "    float4   unity_LODFade;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "cbuffer UnityPerMaterial : register(b2)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4 _BaseColor;" + "\n"
        s = s + "    float4 _EmissionColor;" + "\n"
        s = s + "    float4 _SurfaceParams;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "cbuffer UnityLighting : register(b3)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4 _MainLightPosition;" + "\n"
        s = s + "    float4 _MainLightColor;" + "\n"
        s = s + "    float4 _AmbientColor;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "Texture2D    _MainTex        : register(t0);" + "\n"
        s = s + "SamplerState sampler_MainTex : register(s0);" + "\n"
        s = s + "" + "\n"
        s = s + "struct Attributes" + "\n"
        s = s + "{" + "\n"
        s = s + "    float3 positionOS : POSITION;" + "\n"
        s = s + "    float3 normalOS   : NORMAL;" + "\n"
        s = s + "    float4 tangentOS  : TANGENT;" + "\n"
        s = s + "    float2 uv         : TEXCOORD0;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "struct Varyings" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4 positionCS : SV_POSITION;" + "\n"
        s = s + "    float3 positionWS : TEXCOORD0;" + "\n"
        s = s + "    float3 normalWS   : TEXCOORD1;" + "\n"
        s = s + "    float2 uv         : TEXCOORD2;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "float3 TransformObjectToWorld(float3 positionOS)" + "\n"
        s = s + "{" + "\n"
        s = s + "    return mul(unity_ObjectToWorld, float4(positionOS, 1.0)).xyz;" + "\n"
        s = s + "}" + "\n"
        s = s + "" + "\n"
        s = s + "float4 TransformObjectToHClip(float3 positionOS)" + "\n"
        s = s + "{" + "\n"
        s = s + "    return mul(unity_MatrixVP, mul(unity_ObjectToWorld, float4(positionOS, 1.0)));" + "\n"
        s = s + "}" + "\n"
        s = s + "" + "\n"
        s = s + "float3 TransformObjectToWorldNormal(float3 normalOS)" + "\n"
        s = s + "{" + "\n"
        s = s + "    return normalize(mul((float3x3)unity_ObjectToWorld, normalOS));" + "\n"
        s = s + "}" + "\n"
        s = s + "" + "\n"
        s = s + "float3 GetWorldSpaceViewDir(float3 positionWS)" + "\n"
        s = s + "{" + "\n"
        s = s + "    return normalize(_WorldSpaceCameraPos - positionWS);" + "\n"
        s = s + "}" + "\n"
        s = s + "" + "\n"
        s = s + "#endif" + "\n"
        ret s
    }

    # ── 光照函数（简化版）─────────────────────────────────
    public static string lighting()
    {
        string s = "// 简化的 Cook-Torrance：GGX 法线分布 + Schlick 菲涅尔 + 几何项近似" + "\n"
        s = s + "static const float PI = 3.14159265359;" + "\n"
        s = s + "" + "\n"
        s = s + "float DistributionGGX(float NdotH, float roughness)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float a  = roughness * roughness;" + "\n"
        s = s + "    float a2 = a * a;" + "\n"
        s = s + "    float d  = NdotH * NdotH * (a2 - 1.0) + 1.0;" + "\n"
        s = s + "    return a2 / max(PI * d * d, 0.000001);" + "\n"
        s = s + "}" + "\n"
        s = s + "" + "\n"
        s = s + "float3 FresnelSchlick(float cosTheta, float3 F0)" + "\n"
        s = s + "{" + "\n"
        s = s + "    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);" + "\n"
        s = s + "}" + "\n"
        s = s + "" + "\n"
        s = s + "float GeometrySmith(float NdotV, float NdotL, float roughness)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float k = (roughness + 1.0) * (roughness + 1.0) * 0.125;" + "\n"
        s = s + "    float gv = NdotV / (NdotV * (1.0 - k) + k);" + "\n"
        s = s + "    float gl = NdotL / (NdotL * (1.0 - k) + k);" + "\n"
        s = s + "    return gv * gl;" + "\n"
        s = s + "}" + "\n"
        s = s + "" + "\n"
        s = s + "float3 LightingPBR(float3 N, float3 V, float3 L, float3 radiance," + "\n"
        s = s + "                   float3 albedo, float metallic, float roughness)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float3 H     = normalize(V + L);" + "\n"
        s = s + "    float  NdotV = max(dot(N, V), 0.0001);" + "\n"
        s = s + "    float  NdotL = max(dot(N, L), 0.0);" + "\n"
        s = s + "    float  NdotH = max(dot(N, H), 0.0);" + "\n"
        s = s + "    float  VdotH = max(dot(V, H), 0.0);" + "\n"
        s = s + "" + "\n"
        s = s + "    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);" + "\n"
        s = s + "    float  D  = DistributionGGX(NdotH, roughness);" + "\n"
        s = s + "    float  G  = GeometrySmith(NdotV, NdotL, roughness);" + "\n"
        s = s + "    float3 F  = FresnelSchlick(VdotH, F0);" + "\n"
        s = s + "" + "\n"
        s = s + "    float3 specular = (D * G * F) / max(4.0 * NdotV * NdotL, 0.0001);" + "\n"
        s = s + "    float3 kD = (float3(1.0, 1.0, 1.0) - F) * (1.0 - metallic);" + "\n"
        s = s + "    return (kD * albedo / PI + specular) * radiance * NdotL;" + "\n"
        s = s + "}" + "\n"
        ret s
    }

    # ── Lit：顶点 ────────────────────────────────────────
    public static string litVertexSource()
    {
        string s = "#include Common" + "\n"
        s = s + "" + "\n"
        s = s + "Varyings VSMain(Attributes input)" + "\n"
        s = s + "{" + "\n"
        s = s + "    Varyings output;" + "\n"
        s = s + "    output.positionWS = TransformObjectToWorld(input.positionOS);" + "\n"
        s = s + "    output.normalWS   = TransformObjectToWorldNormal(input.normalOS);" + "\n"
        s = s + "    output.uv         = input.uv;" + "\n"
        s = s + "    output.positionCS = TransformObjectToHClip(input.positionOS);" + "\n"
        s = s + "    return output;" + "\n"
        s = s + "}" + "\n"
        ret s
    }

    # ── Lit：像素 ────────────────────────────────────────
    public static string litPixelSource()
    {
        string s = "#include Common" + "\n"
        s = s + "#include Lighting" + "\n"
        s = s + "" + "\n"
        s = s + "float4 PSMain(Varyings input) : SV_Target" + "\n"
        s = s + "{" + "\n"
        s = s + "    float metallic  = _SurfaceParams.x;" + "\n"
        s = s + "    float roughness = clamp(1.0 - _SurfaceParams.y, 0.045, 1.0);" + "\n"
        s = s + "" + "\n"
        s = s + "    float4 tex = _MainTex.Sample(sampler_MainTex, input.uv);" + "\n"
        s = s + "    float3 albedo = tex.rgb * _BaseColor.rgb;" + "\n"
        s = s + "" + "\n"
        s = s + "    float3 N = normalize(input.normalWS);" + "\n"
        s = s + "    float3 V = GetWorldSpaceViewDir(input.positionWS);" + "\n"
        s = s + "    float3 L = normalize(_MainLightPosition.xyz);" + "\n"
        s = s + "    float3 radiance = _MainLightColor.rgb * _MainLightColor.a;" + "\n"
        s = s + "" + "\n"
        s = s + "    float3 color = LightingPBR(N, V, L, radiance, albedo, metallic, roughness);" + "\n"
        s = s + "    color += albedo * _AmbientColor.rgb * _AmbientColor.a;" + "\n"
        s = s + "    color += _EmissionColor.rgb;" + "\n"
        s = s + "" + "\n"
        s = s + "    return float4(color, _BaseColor.a * tex.a);" + "\n"
        s = s + "}" + "\n"
        ret s
    }

    # ── Unlit ───────────────────────────────────────────
    public static string unlitVertexSource()
    {
        string s = "#include Common" + "\n"
        s = s + "" + "\n"
        s = s + "Varyings VSMain(Attributes input)" + "\n"
        s = s + "{" + "\n"
        s = s + "    Varyings output;" + "\n"
        s = s + "    output.positionWS = TransformObjectToWorld(input.positionOS);" + "\n"
        s = s + "    output.normalWS   = TransformObjectToWorldNormal(input.normalOS);" + "\n"
        s = s + "    output.uv         = input.uv;" + "\n"
        s = s + "    output.positionCS = TransformObjectToHClip(input.positionOS);" + "\n"
        s = s + "    return output;" + "\n"
        s = s + "}" + "\n"
        ret s
    }

    public static string unlitPixelSource()
    {
        string s = "#include Common" + "\n"
        s = s + "" + "\n"
        s = s + "float4 PSMain(Varyings input) : SV_Target" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4 tex = _MainTex.Sample(sampler_MainTex, input.uv);" + "\n"
        s = s + "    float3 color = tex.rgb * _BaseColor.rgb + _EmissionColor.rgb;" + "\n"
        s = s + "    return float4(color, tex.a * _BaseColor.a);" + "\n"
        s = s + "}" + "\n"
        ret s
    }

    # ── DepthOnly（Z-Prepass / ShadowCaster 共用）──────────
    public static string depthOnlyVertexSource()
    {
        string s = "#include Common" + "\n"
        s = s + "" + "\n"
        s = s + "cbuffer UnityShadow : register(b4)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4x4 unity_MatrixLP;" + "\n"
        s = s + "    float4   _ShadowParams;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "float4 VSMain(Attributes input) : SV_POSITION" + "\n"
        s = s + "{" + "\n"
        s = s + "    float3 positionWS = TransformObjectToWorld(input.positionOS);" + "\n"
        s = s + "    return mul(unity_MatrixLP, float4(positionWS, 1.0));" + "\n"
        s = s + "}" + "\n"
        ret s
    }

    # 深度只需要写深度，像素着色器可以为空（AlphaTest 时再开）
    public static string depthOnlyPixelSource()
    {
        string s = "#include Common" + "\n"
        s = s + "" + "\n"
        s = s + "void PSMain(Varyings input)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4 tex = _MainTex.Sample(sampler_MainTex, input.uv);" + "\n"
        s = s + "    clip(tex.a * _BaseColor.a - _SurfaceParams.z);" + "\n"
        s = s + "}" + "\n"
        ret s
    }

    # ── Skybox ──────────────────────────────────────────
    public static string skyboxVertexSource()
    {
        string s = "#include Common" + "\n"
        s = s + "" + "\n"
        s = s + "struct SkyboxVaryings" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4 positionCS : SV_POSITION;" + "\n"
        s = s + "    float3 direction  : TEXCOORD0;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "// 全屏三角形：无需顶点缓冲，靠 vertexID 生成" + "\n"
        s = s + "SkyboxVaryings VSMain(uint vertexID : SV_VertexID)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float2 uv = float2((vertexID << 1) & 2, vertexID & 2);" + "\n"
        s = s + "    SkyboxVaryings output;" + "\n"
        s = s + "    output.positionCS = float4(uv * 2.0 - 1.0, 1.0, 1.0);" + "\n"
        s = s + "    float4 clip = mul(unity_MatrixVP, float4(output.positionCS.xy, 1.0, 1.0));" + "\n"
        s = s + "    output.direction  = mul((float3x3)unity_WorldToObject, clip.xyz);" + "\n"
        s = s + "    return output;" + "\n"
        s = s + "}" + "\n"
        ret s
    }

    public static string skyboxPixelSource()
    {
        string s = "#include Common" + "\n"
        s = s + "" + "\n"
        s = s + "TextureCube _SkyboxCubemap : register(t1);" + "\n"
        s = s + "" + "\n"
        s = s + "struct SkyboxVaryings" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4 positionCS : SV_POSITION;" + "\n"
        s = s + "    float3 direction  : TEXCOORD0;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "float4 PSMain(SkyboxVaryings input) : SV_Target" + "\n"
        s = s + "{" + "\n"
        s = s + "    return _SkyboxCubemap.Sample(sampler_MainTex, normalize(input.direction));" + "\n"
        s = s + "}" + "\n"
        ret s
    }

    # ── 全屏通道（后处理 / FinalBlit）──────────────────────
    public static string fullscreenVertexSource()
    {
        string s = "struct FullscreenVaryings" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4 positionCS : SV_POSITION;" + "\n"
        s = s + "    float2 uv         : TEXCOORD0;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "FullscreenVaryings VSMain(uint vertexID : SV_VertexID)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float2 uv = float2((vertexID << 1) & 2, vertexID & 2);" + "\n"
        s = s + "    FullscreenVaryings output;" + "\n"
        s = s + "    output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);" + "\n"
        s = s + "    output.uv         = float2(uv.x, 1.0 - uv.y);" + "\n"
        s = s + "    return output;" + "\n"
        s = s + "}" + "\n"
        ret s
    }

    public static string postProcessPixelSource()
    {
        string s = "cbuffer PostProcessParams : register(b0)" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4 _PostParams0;" + "\n"
        s = s + "    float4 _PostParams1;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "Texture2D    _BlitTexture : register(t0);" + "\n"
        s = s + "SamplerState sampler_LinearClamp : register(s0);" + "\n"
        s = s + "" + "\n"
        s = s + "struct FullscreenVaryings" + "\n"
        s = s + "{" + "\n"
        s = s + "    float4 positionCS : SV_POSITION;" + "\n"
        s = s + "    float2 uv         : TEXCOORD0;" + "\n"
        s = s + "};" + "\n"
        s = s + "" + "\n"
        s = s + "// ACES 近似 tonemap + 暗角" + "\n"
        s = s + "float3 ACESFilm(float3 x)" + "\n"
        s = s + "{" + "\n"
        s = s + "    return clamp((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14), 0.0, 1.0);" + "\n"
        s = s + "}" + "\n"
        s = s + "" + "\n"
        s = s + "float4 PSMain(FullscreenVaryings input) : SV_Target" + "\n"
        s = s + "{" + "\n"
        s = s + "    float exposure = _PostParams0.x;" + "\n"
        s = s + "    float vignette = _PostParams0.y;" + "\n"
        s = s + "" + "\n"
        s = s + "    float3 color = _BlitTexture.Sample(sampler_LinearClamp, input.uv).rgb;" + "\n"
        s = s + "    color = ACESFilm(color * exposure);" + "\n"
        s = s + "" + "\n"
        s = s + "    float2 d = input.uv - 0.5;" + "\n"
        s = s + "    float v = 1.0 - dot(d, d) * vignette;" + "\n"
        s = s + "    color *= saturate(v);" + "\n"
        s = s + "" + "\n"
        s = s + "    return float4(color, 1.0);" + "\n"
        s = s + "}" + "\n"
        ret s
    }

    # ── 组装成 HlslShader ───────────────────────────────
    public static HlslShader litShader()
    {
        HlslShader sh = HlslShader( "Hidden/SL/Lit" )
        sh.vertexSource = HlslLibrary.common() + "\n" + HlslLibrary.lighting() + "\n" + HlslLibrary.litVertexSource()
        sh.pixelSource = HlslLibrary.common() + "\n" + HlslLibrary.lighting() + "\n" + HlslLibrary.litPixelSource()
        sh.vertexEntry = "VSMain"
        sh.pixelEntry = "PSMain"
        sh.addParameter( "unity_MatrixVP", EShaderParamType.Matrix4x4, 0 )
        sh.addParameter( "unity_ObjectToWorld", EShaderParamType.Matrix4x4, 1 )
        sh.addParameter( "_BaseColor", EShaderParamType.Float4, 2 )
        sh.addParameter( "_MainTex", EShaderParamType.Texture2D, 0 )
        ret sh
    }

    public static HlslShader unlitShader()
    {
        HlslShader sh = HlslShader( "Hidden/SL/Unlit" )
        sh.vertexSource = HlslLibrary.common() + "\n" + HlslLibrary.unlitVertexSource()
        sh.pixelSource = HlslLibrary.common() + "\n" + HlslLibrary.unlitPixelSource()
        ret sh
    }

    public static HlslShader depthOnlyShader()
    {
        HlslShader sh = HlslShader( "Hidden/SL/DepthOnly" )
        sh.vertexSource = HlslLibrary.common() + "\n" + HlslLibrary.depthOnlyVertexSource()
        sh.pixelSource = HlslLibrary.common() + "\n" + HlslLibrary.depthOnlyPixelSource()
        ret sh
    }

    public static HlslShader skyboxShader()
    {
        HlslShader sh = HlslShader( "Hidden/SL/Skybox" )
        sh.vertexSource = HlslLibrary.common() + "\n" + HlslLibrary.skyboxVertexSource()
        sh.pixelSource = HlslLibrary.common() + "\n" + HlslLibrary.skyboxPixelSource()
        ret sh
    }

    public static HlslShader postProcessShader()
    {
        HlslShader sh = HlslShader( "Hidden/SL/PostProcess" )
        sh.vertexSource = HlslLibrary.fullscreenVertexSource()
        sh.pixelSource = HlslLibrary.postProcessPixelSource()
        ret sh
    }
}
