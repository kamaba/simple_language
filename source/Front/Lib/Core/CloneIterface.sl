



    public interface Core.IClone
    {
        clone()
    }

    #!
    public class Level<T> interface Core.IClone
    {
        T _value = null
        public Level<T> clone()
        {
            Level<T> newclone = Level<T>()
            newclone._value = this._value
            ret newclone
        }
    }
    !#