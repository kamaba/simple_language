public class Random extends Object
{
    Int32 _seed = 0

    override _init_()
    {
        this._seed = SystemGeneralRandomSeed()
    }

    _init_( Int32 initialSeed )
    {
        this._seed = initialSeed
    }

    # Advance the linear congruential generator and return the new state.
    public Int32 advance()
    {
        this._seed = (this._seed * 1103515245 + 12345) & 2147483647
        ret this._seed
    }

    # Generate random int in range [0, max)
    public Int32 nextInt( Int32 max )
    {
        if (max <= 0) { ret 0 }
        ret this.advance() % max
    }

    # Generate random int in range [min, max)
    public Int32 nextInt( Int32 min, Int32 max )
    {
        if (min >= max) { ret min }
        ret min + this.nextInt(max - min)
    }

    # Generate random Num in range [0.0, 1.0)
    public Num nextFloat()
    {
        Int32 s = this.advance()
        Num v = SystemConvertFloat64(s)
        ret v / 2147483647.0
    }

    # Generate random Num in range [min, max)
    public Num nextFloat( Num min, Num max )
    {
        ret min + this.nextFloat() * (max - min)
    }

    # Generate random bool
    public bool nextBool()
    {
        ret this.nextInt(2) == 1
    }

    # Static convenience methods
    public static Int32 randomInt( Int32 max )
    {
        Random r = new()
        ret r.nextInt(max)
    }

    public static Num randomFloat()
    {
        Random r = new()
        ret r.nextFloat()
    }

    public static Num randomFloat( Num min, Num max )
    {
        Random r = new()
        ret r.nextFloat(min, max)
    }
}
