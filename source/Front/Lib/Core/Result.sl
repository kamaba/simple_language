public class Result extends Object
{
    public Object value = null;
    public int error = 0;
    public String message = "";
}

public class Result<T> extends Object
{
    public T value = null;
    public int error = 0;
    public String message = "";
}