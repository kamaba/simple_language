
public class MetaClass
{
    string _namespaceName = "";
    string _className = "";

    get string className(){ ret this._className; }
}

public class Type extends Object
{
    int _hashCode = 0
    byte _eType = 0
    MetaClass _metaClass = null
    public Type[] typelist = null

    // runtime accessors filled by RuntimeTypeManager.CreateTypeObject
    get MetaClass metaClass() { ret this._metaClass; }
    get int hashCode() { ret this._hashCode; }
    get int eType() { ret this._eType.toInt32(); }

    override string toString()
    {
        if( this._metaClass == null )
        {
            ret "no_meta_class"
        }
        ret this._metaClass.className;
    }
}