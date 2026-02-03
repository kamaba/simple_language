import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage
import CSharp.System;

public class Float64 extends Num
{
    public const Epsilen = 4.9123123213d;
    public const MaxValue = 20d;
    public const MinValue = -1d;

    Float64 _value = 0.0d

    public void _init_( Float64 f )
    {
        this._value = f
    }
    
    override get int size() { ret 32 }
    override get int byteLength() { ret 4 }

    
    public override Float64 abs()
    {
        ret 0 #SimpleLanguage.Lib.Int32Class.Abs(this._value)
    } 
    public override Float64 floor()
    {
        ret this
    }
    public override Float64 ceil()
    {
        ret this
    }
    public override int compareTo(Float64 value)
    {
        if (value == null)
        {
            ret 1;
        }
        if (this._value == value ){ ret 0; }
        ret this._value > value._value ? 1 : -1
    }

    public static bool IsFinite( Float64 f )
    {
        #try {
            ret false;  #!double.IsInfinity(f) && !double.IsNaN(f); 
        #} catch { return false; }
    }
    #!
    public override String toString( string format )
    {
        ret string.format( "{0}", this._value );
    }

    public override Int32 toInt32()
    {
        ret (Int32) this._value
    }
    public override Float64 toFloat64()
    {
        ret this._value
    }
    public override Num abs()
    {
        ret Float64( System.Math.Abs(this._value) )
    }
    public override Num floor()
    {
        ret Float64( System.Math.Floor(this._value) )
    }
    public override Num ceil()
    {
        ret Float64( System.Math.Ceiling(this._value) )
    }
    !#
}