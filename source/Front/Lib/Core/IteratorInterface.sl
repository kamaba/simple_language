
public interface IIterator
{
    void reset()
    bool moveNext()
    get object current()
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
}
public interface IIterable<T>
{
    IIterator<T> iterator()
}