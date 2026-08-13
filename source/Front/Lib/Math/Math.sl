public class Math
{
        public static float Pi = 3.141592653589793f
        public static float E = 2.718281828459045f

        # Trigonometric functions
        public static float sin(float value)
        {
            object r = SystemCallExternalFunction("Math.sin", value)
            ret r
        }

        public static float cos(float value)
        {
            object r = SystemCallExternalFunction("Math.cos", value)
            ret r
        }

        public static float tan(float value)
        {
            object r = SystemCallExternalFunction("Math.tan", value)
            ret r
        }

        public static float asin(float value)
        {
            object r = SystemCallExternalFunction("Math.asin", value)
            ret r
        }

        public static float acos(float value)
        {
            object r = SystemCallExternalFunction("Math.acos", value)
            ret r
        }

        public static float atan(float value)
        {
            object r = SystemCallExternalFunction("Math.atan", value)
            ret r
        }

        public static float atan2(float y, float x)
        {
            object r = SystemCallExternalFunction("Math.atan2", y, x)
            ret r
        }

        # Hyperbolic functions
        public static float sinh(float value)
        {
            object r = SystemCallExternalFunction("Math.sinh", value)
            ret r
        }

        public static float cosh(float value)
        {
            object r = SystemCallExternalFunction("Math.cosh", value)
            ret r
        }

        public static float tanh(float value)
        {
            object r = SystemCallExternalFunction("Math.tanh", value)
            ret r
        }

        # Power and logarithm
        public static float pow(float baseValue, float exponent)
        {
            object r = SystemCallExternalFunction("Math.pow", baseValue, exponent)
            ret r
        }

        public static float sqrt(float value)
        {
            object r = SystemCallExternalFunction("Math.sqrt", value)
            ret r
        }

        public static float exp(float value)
        {
            object r = SystemCallExternalFunction("Math.exp", value)
            ret r
        }

        public static float log(float value)
        {
            object r = SystemCallExternalFunction("Math.log", value)
            ret r
        }

        public static float log10(float value)
        {
            object r = SystemCallExternalFunction("Math.log10", value)
            ret r
        }

        # Rounding
        public static float ceil(float value)
        {
            object r = SystemCallExternalFunction("Math.ceil", value)
            ret r
        }

        public static float floor(float value)
        {
            object r = SystemCallExternalFunction("Math.floor", value)
            ret r
        }

        public static float round(float value)
        {
            object r = SystemCallExternalFunction("Math.round", value)
            ret r
        }

        public static float truncate(float value)
        {
            object r = SystemCallExternalFunction("Math.truncate", value)
            ret r
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
