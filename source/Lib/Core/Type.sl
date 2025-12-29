

    public class Core.MetaClass
    {
        string _namespaceName = "";
        string _className = "";

        get string className(){ ret this._className; }
    }

    public class Core.Type
    {
        int _hashCode = 0
        MetaClass _metaClass = null
        Type[] typelist = null

        override string toString()
        {
            if( this._metaClass == null )
            {
                ret "no_meta_class"
            }

            ret this._metaClass.className;
        }
    }