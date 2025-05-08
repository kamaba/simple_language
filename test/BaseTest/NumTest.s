StringTest
{
    Class1{
        a1 = 20;
        static string Printf( string x )
        {
            CSharp.System.Console.Write("---------------------: " + x );
            ret "m";
        }
    }
    static Fun()
    {
        a1 = "aabcc";
        a2 = "aaacc" + "deee";
        a3 = "aa" + 3;
        a4 = "aa" + 20.0f.ToString();
        Class1.Printf( a2 );
        
        int a = 1;
        int b = 2;
        a5 = "${ a+b+(3+10+"aadf").ToString() }";    #{}任何时刻，都表示可以执行自己的内部语句  
        a6 = "print a=$Class1.Printf('ass') ";

        Class1 c1;
        a7 = "print c1.a1=$c1.a1 ";
        a8 = @" /nskemsikeaae/t/r' ";
        a9 = @"asdfasdf{a4} @c1.a1 ";
               
        c1 = 'a';
        c2 = "a";
        string c3 = 'a';   #相当于'a'.cast<String>();
        c4 = 'a' + 'b';   # 相当于  (int)a + (int)b;
        c5 = 'aaaaa';       #报错
        c6 = "aaaaaa";   
        #!
        !#
    }
}