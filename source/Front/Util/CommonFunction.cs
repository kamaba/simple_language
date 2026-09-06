using SimpleLanguage.Core;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace SimpleLanguage
{
    public class CommonFunction
    {
        public static int StringToIntHash(string str)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);
            byte[] md5 = MD5.HashData(data);
            //取前4个字节拼成int
            return BitConverter.ToInt32(md5, 0);
        }
    }
}
