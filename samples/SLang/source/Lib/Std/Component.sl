
public class Component extends Object
{
    List<Component> _childrensComponents = new()

    public T GetComponent<T:Component>()
    {
        for v in this._childrensComponents
        {
            if v.type == T.type
            {
                ret v as T
            }
        }
        ret null
    }
}