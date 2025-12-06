



    public interface Core.IIterator
    {
        void reset()
        bool moveNext()
        get object current()
        void release()
    }
    public interface Core.IIterable
    {
        IIterator iterator()
    }