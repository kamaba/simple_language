
public class Math
{
    public const float Pi = 3.1415926f;

    public static T min<T>( T a, T b )
    {
        if a < b
        { 
            ret a
        } 
        else 
        { 
            ret b
        }
    }
    #!
    public float dot( float2 a, float2 b )
    {
        return sqrt( a.@0 * b.@1 + a.@1 * b.@0 )
    }
    public float dot( float3 a, float3 b )
    {
        
    }
    !#
}