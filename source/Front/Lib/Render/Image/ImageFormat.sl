# =========================================================================
# Image/ImageFormat.sl —— 图像格式 / 像素格式 / 色彩空间 / 错误码
#
# 约定（与 DB/Sqlite.sl 保持一致）：
#   - 所有类型必须是 namespace 下的顶级类型，**不能嵌套在类内**
#     （嵌套类型会让导出端把父类全名写进 namespaceList，加载端随即创建
#       同名命名空间节点遮蔽类节点，导致调用链解析失败）
#   - 编解码本体由图像后端通过 SystemCallExternalFunction("Image.xxx", ...)
#     实现；本目录负责：数据模型 + 能用纯 SL 算出来的部分
#     （magic 嗅探、PNG/BMP 头解析、像素运算、缩放、色彩转换…）
# =========================================================================

namespace Image
{
    # ---------------------------------------------------------------
    # 容器 / 文件格式
    # ---------------------------------------------------------------
    public enum EImageFormat
    {
        Unknown = 0
        Bmp
        Png
        Jpeg
        Gif
        Webp
        Svg
        Tiff
        Ico
        Tga
        Ppm
        Avif
        Heic
    }

    # ---------------------------------------------------------------
    # 像素排布（ImageBuffer 的内存布局）
    # ---------------------------------------------------------------
    public enum EPixelFormat
    {
        Unknown = 0
        Gray8         # 1 通道 8 位
        Gray16        # 1 通道 16 位
        Rgb8          # 3 通道 8 位
        Rgba8         # 4 通道 8 位（默认）
        Bgr8          # 3 通道 8 位，蓝在前
        Bgra8         # 4 通道 8 位，蓝在前
        Rgb16         # 3 通道 16 位
        Rgba16        # 4 通道 16 位
        Rgb32F        # 3 通道 Float32（HDR 中间结果）
        Rgba32F       # 4 通道 Float32
    }

    # ---------------------------------------------------------------
    # 色彩空间
    # ---------------------------------------------------------------
    public enum EColorSpace
    {
        Unknown = 0
        SRGB
        Linear
        DisplayP3
        Rec709
        AdobeRgb
    }

    # ---------------------------------------------------------------
    # 缩放插值
    # ---------------------------------------------------------------
    public enum EInterpolation
    {
        Nearest = 0
        Bilinear
        Bicubic
        Lanczos
    }

    # ---------------------------------------------------------------
    # EXIF Orientation（1..8），JPEG 常见
    # ---------------------------------------------------------------
    public enum EOrientation
    {
        Normal = 1
        MirrorHorizontal = 2
        Rotate180 = 3
        MirrorVertical = 4
        MirrorHorizontalRotate270 = 5
        Rotate90 = 6
        MirrorHorizontalRotate90 = 7
        Rotate270 = 8
    }

    # ---------------------------------------------------------------
    # 错误码
    # ---------------------------------------------------------------
    public enum ImageErrorCode extends Error
    {
        OK = {code = 0}
        UnsupportedFormat = {code = 1}
        DecodeFailed = {code = 2}
        EncodeFailed = {code = 3}
        InvalidData = {code = 4}
        OutOfBounds = {code = 5}
        CodecMissing = {code = 6}
        IoError = {code = 7}
    }
}
