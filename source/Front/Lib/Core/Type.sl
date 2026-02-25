public class MetaClass
{
    string _namespaceName = "";
    string _className = "";

    get string className(){ ret this._className; }
}

public class Type extends Object
{
    byte _eType = 0
    MetaClass _metaClass = null
    public Type[] typelist = null
 
    #runtime accessors filled by RuntimeTypeManager.CreateTypeObject    
    get MetaClass metaClass() { ret this._metaClass; }
    get int eType() { ret this._eType.toInt32(); }

    override string toString()
    {
        if( this._metaClass == null )
        {
            ret "no_meta_class"
        }
        string str = this._metaClass.className;
        if this.typelist != null 
        {
            str = str + "<"
            for i = 0, i < this.typelist.length, i++ 
            {
                str = str + this.typelist[i].toString()
            }
            str = str + ">"
        }
        ret str
    }
}