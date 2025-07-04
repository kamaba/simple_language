using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaModule : MetaBase
    {
        public MetaModule( string _name )
        {
            m_Name = _name;
            m_MetaNode = new MetaNode(this);
        }
    }
}
