public class Math
{
    public static float Pi = 3.141592653589793f
    public static float E = 2.718281828459045f

    # Trigonometric functions
    public static float sin(float value)
    {
        ret SystemMathSin(value)
    }

    public static float cos(float value)
    {
        ret SystemMathCos(value)
    }

    public static float tan(float value)
    {
        ret SystemMathTan( value)
    }

    public static float asin(float value)
    {
        ret SystemMathASin( value)
    }

    public static float acos(float value)
    {
        ret SystemMathACos( value)
    }

    public static float atan(float value)
    {
        ret SystemMathATan( value )
    }

    public static float atan2(float y, float x)
    {
        ret SystemMathATan2( y, x )
    }

    # Hyperbolic functions
    public static float sinh(float value)
    {
        ret SystemMathASinh( value )
    }

    public static float cosh(float value)
    {
        ret SystemMathCosh( value )
    }

    public static float tanh(float value)
    {
        ret SystemMathTanh( value )
    }

    # Power and logarithm
    public static float pow(float baseValue, float exponent)
    {
        ret SystemMathPow( baseValue, exponent )
    }

    public static float sqrt(float value)
    {
        ret SystemMathSqrt( value )
    }

    public static float exp(float value)
    {
        ret SystemMathExp( value )
    }

    public static float log(float value)
    {
        ret SystemMathLog( value )
    }

    public static float log10(float value)
    {
        ret SystemMathLog10( value )
    }

    # Rounding
    public static float ceil(float value)
    {
        ret SystemMathCeil( value )
    }

    public static float floor(float value)
    {
        ret SystemMathFloor( value )
    }

    public static float round(float value)
    {
        ret SystemMathRound( value )
    }

    public static float truncate(float value)
    {
        ret SystemMathTruncate( value )
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
