
    public interface IIterator
    {
        void reset()
        bool moveNext()
        get object current()
        void release()
    }
    public interface IIterable
    {
        IIterator iterator()
    }
    public interface IIterator<T>
    {
        void reset()
        bool moveNext()
        get T current()
        set void current( T t )
        void release()
    }
    public interface IIterable<T>
    {
        IIterator<T> iterator()
    }