using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage
{
    interface IErrorHandle
    {
        void AddError(int errorid);
    }
}
