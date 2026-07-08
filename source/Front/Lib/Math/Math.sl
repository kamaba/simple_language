namespace Std.Math
{
    public class Math
    {
        public const float Pi = 3.141592653589793f
        public const float E = 2.718281828459045f

        # Trigonometric functions
        public static float sin(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Sin", bo, value)
            ret bo.toFloat32()
        }

        public static float cos(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Cos", bo, value)
            ret bo.toFloat32()
        }

        public static float tan(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Tan", bo, value)
            ret bo.toFloat32()
        }

        public static float asin(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Asin", bo, value)
            ret bo.toFloat32()
        }

        public static float acos(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Acos", bo, value)
            ret bo.toFloat32()
        }

        public static float atan(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Atan", bo, value)
            ret bo.toFloat32()
        }

        public static float atan2(float y, float x)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Atan2", bo, y, x)
            ret bo.toFloat32()
        }

        # Hyperbolic functions
        public static float sinh(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Sinh", bo, value)
            ret bo.toFloat32()
        }

        public static float cosh(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Cosh", bo, value)
            ret bo.toFloat32()
        }

        public static float tanh(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Tanh", bo, value)
            ret bo.toFloat32()
        }

        # Power and logarithm
        public static float pow(float baseValue, float exponent)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Pow", bo, baseValue, exponent)
            ret bo.toFloat32()
        }

        public static float sqrt(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Sqrt", bo, value)
            ret bo.toFloat32()
        }

        public static float exp(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Exp", bo, value)
            ret bo.toFloat32()
        }

        public static float log(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Log", bo, value)
            ret bo.toFloat32()
        }

        public static float log10(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Log10", bo, value)
            ret bo.toFloat32()
        }

        # Rounding
        public static float ceil(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Ceiling", bo, value)
            ret bo.toFloat32()
        }

        public static float floor(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Floor", bo, value)
            ret bo.toFloat32()
        }

        public static float round(float value)
        {
            BridgeObject bo = new("float")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Round", bo, value)
            ret bo.toFloat32()
        }

        public static int truncate(float value)
        {
            BridgeObject bo = new("int")
            NativeBridge.Call(BridgeObject.CLR, "System", "Math", "Truncate", bo, value)
            ret bo.toInt32()
        }

        # Absolute value
        public static int abs(int value)
        {
            if value < 0
            {
                ret -value
            }
            ret value
        }

        public static float abs(float value)
        {
            if value < 0.0f
            {
                ret -value
            }
            ret value
        }

        # Min / Max
        public static int min(int a, int b)
        {
            if a < b
            {
                ret a
            }
            ret b
        }

        public static float min(float a, float b)
        {
            if a < b
            {
                ret a
            }
            ret b
        }

        public static int max(int a, int b)
        {
            if a > b
            {
                ret a
            }
            ret b
        }

        public static float max(float a, float b)
        {
            if a > b
            {
                ret a
            }
            ret b
        }

        # Clamp
        public static int clamp(int value, int minValue, int maxValue)
        {
            if value < minValue
            {
                ret minValue
            }
            if value > maxValue
            {
                ret maxValue
            }
            ret value
        }

        public static float clamp(float value, float minValue, float maxValue)
        {
            if value < minValue
            {
                ret minValue
            }
            if value > maxValue
            {
                ret maxValue
            }
            ret value
        }

        # Sign
        public static int sign(int value)
        {
            if value > 0
            {
                ret 1
            }
            if value < 0
            {
                ret -1
            }
            ret 0
        }

        public static int sign(float value)
        {
            if value > 0.0f
            {
                ret 1
            }
            if value < 0.0f
            {
                ret -1
            }
            ret 0
        }

        # Distance
        public static float distance(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1
            float dy = y2 - y1
            ret sqrt(dx * dx + dy * dy)
        }

        public static float distance3D(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            float dx = x2 - x1
            float dy = y2 - y1
            float dz = z2 - z1
            ret sqrt(dx * dx + dy * dy + dz * dz)
        }

        # Lerp
        public static float lerp(float a, float b, float t)
        {
            ret a + (b - a) * t
        }

        # Degrees / Radians conversion
        public static float degrees(float radians)
        {
            ret radians * 180.0f / Pi
        }

        public static float radians(float degrees)
        {
            ret degrees * Pi / 180.0f
        }
    }
}
