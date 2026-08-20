
public class Component extends Object
{
    List<Component> _childrensComponents = new(4)

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