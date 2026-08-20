public class Random extends Object
{
    private int _seed = 0
    _init_()
    {
        this._seed = SystemGeneralRandomSeed()
    }
    _init_(int initialSeed)
    {
        this._seed = initialSeed
    }    
    # Generate random int in range [0, max)
    public int nextInt(int max)
    {
        this._seed = (this._seed * 1103515245 + 12345) & 0x7fffffff
        ret seed % max
    }    
    # Generate random int in range [min, max)
    public int nextInt(int min, int max)
    {
        if min >= max
        {
            ret min
        }
        ret min + nextInt(max - min)
    }    
    # Generate random float in range [0.0, 1.0)
    public float nextFloat()
    {
        seed = (seed * 1103515245 + 12345) & 0x7fffffff
        ret float(seed) / float(0x7fffffff)
    }    
    # Generate random float in range [min, max)
    public float nextFloat(float min, float max)
    {
        ret min + nextFloat() * (max - min)
    }    
    # Generate random bool
    public bool nextBool()
    {
        ret nextInt(2) == 1
    }    
    # Pick random element from array
    public T nextElement<T>(T[] array)
    {
        if array.length == 0
        {
            ret default
        }
        ret array[nextInt(array.length)]
    }    
    # Shuffle array in place
    public void shuffle<T>(T[] array)
    {
        int n = array.length
        for int i = n - 1, i > 0, i--
        {
            int j = nextInt(i + 1)
            T temp = array[i]
            array[i] = array[j]
            array[j] = temp
        }
    }    
    # Static convenience methods
    public static int randomInt(int max)
    {
        Random r = new()
        ret r.nextInt(max)
    }    
    public static float randomFloat()
    {
        Random r = new()
        ret r.nextFloat()
    }    
    public static float randomFloat(float min, float max)
    {
        Random r = new()
        ret r.nextFloat(min, max)
    }
}