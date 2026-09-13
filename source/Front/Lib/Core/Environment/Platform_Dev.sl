# =========================================================================
# Platform_Dev — device / ai / render / shaderModel definition enums (§13.6).
#
# Part of `namespace Environment { namespace Platform { ... } }`
# (design doc PLATFORM_CAPABILITY_DESIGN.md §13.2).
#
# Usage:
#   if Environment.current.deviceHas( Environment.Platform.device.gpu ) { ... }
# =========================================================================

namespace Environment
{
    namespace Platform
    {
        # Compute device kinds (§13.6). ★ Bit set semantics: the value is a
        # BIT INDEX — query with Environment.current.deviceHas / deviceCount.
        public enum device extends int
        {
            none = 0
            cpu  = 1
            gpu  = 2
            npu  = 3
            dsp  = 4
            fpga = 5
        }

        # AI inference stacks (§13.6). Bit set (P2 detection).
        public enum ai extends int
        {
            none         = 0
            onnxruntime  = 1
            tensorRT     = 2
            openVINO     = 3
            tflite       = 4
            coreML       = 5
            cann         = 6
            rknn         = 7
            cambricon    = 8
            qnn          = 9
            snpe         = 10
            horizonBpu   = 11
            sophon       = 12
            ane          = 13
            torch        = 14
            tensorFlow   = 15
        }

        # Graphics APIs (§13.6). Bit set (P2 detection).
        public enum render extends int
        {
            none      = 0
            d3d11     = 1
            d3d12     = 2
            vulkan    = 3
            metal     = 4
            openGL    = 5
            openGLES  = 6
            webGPU    = 7
            software  = 8
        }

        # Shader model level (§13.6).
        public enum shaderModel extends int
        {
            unknown = 0
            sm_5_0  = 50
            sm_6_0  = 60
            sm_6_5  = 65
            sm_6_6  = 66
            sm_6_7  = 67
        }
    }
}
