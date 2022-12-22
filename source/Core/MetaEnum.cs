using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaEnum : MetaClass
    {
        public MetaEnum(string _name) : base(_name)
        {
        }

        public override bool AddMetaBase(string name, MetaBase mb)
        {
            return base.AddMetaBase(name, mb);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override MetaBase GetChildrenMetaBaseByName(string name)
        {
            return base.GetChildrenMetaBaseByName(name);
        }

        public override bool IsIncludeMetaBase(string name)
        {
            return base.IsIncludeMetaBase(name);
        }

        public override void SetAnchorDeep(int addep)
        {
            base.SetAnchorDeep(addep);
        }

        public override string ToFormatString()
        {
            return base.ToFormatString();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
