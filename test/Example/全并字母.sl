import CSharp.System;

HB
{
    static Fun()
    {
        fp = File.open("test.txt")
        a = fp.read()
        fp.close()

        fp = File.open("test2.txt")
        b = fp.read()
        fp.close()

        fp = File.open("test3.txt", "w" )
        l = List(){a, b }
        l.sort()
        s = ""
        s = s.join(l)

        fp.write(s);
        fp.close()
    }
}