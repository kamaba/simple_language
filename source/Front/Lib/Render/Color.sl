public class Color
{
        float r = 0.0f;
        float g = 0.0f;
        float b = 0.0f;
        float a = 0.0f;

        _init_( float _r, float _g, float _b, float _a )
        {
            this.r = _r;
            this.g = _g;
            this.b = _b;
            this.a = _a;
        }

        addColor( const Color a )
        {
            this.r += a.r;
            this.g += a.g;
            this.b += a.b;
            this.a += a.a`;
        }
    }
