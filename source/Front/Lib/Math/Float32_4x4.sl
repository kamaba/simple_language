

public class Float32_4x4
{
    public float[] _mat4x4 = new float[16];
    public _init_()
    {
        for (int i = 0; i < 16; i++)
        {
            this._mat4x4[i] = 0.0f;
        }
    }

    
    Float32 _getItem_( int index )
    {        
        ret this._mat4x4[index];
    }
    void _setItem_( int index, Float32 value )
    {        
        this._mat4x4[index] = value;
    }
}