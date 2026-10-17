import Std;
import Core;

FIParent
{
    int pa = 42
    bool pb = true
}

FIChild extends FIParent
{
    int pc = 7
    override _init_()
    {
    }
}

FIChildNoCtor extends FIParent
{
    int pd = 9
}

FICrossChild extends MemoryStream
{
    int px = 5
    override _init_()
    {
    }
}

SFReproTest
{
    static fun()
    {
        Console.println( "=== field-init probe ===" )
        var p = FIParent()
        Console.println( "P parent direct: pa=" + p.pa + " pb=" + p.pb )
        var c = FIChild()
        Console.println( "C child override-ctor: pa=" + c.pa + " pc=" + c.pc )
        var d = FIChildNoCtor()
        Console.println( "D child no-ctor: pa=" + d.pa + " pd=" + d.pd )
        var ms = MemoryStream()
        Console.println( "M core-module child: canRead=" + ms.canRead + " canWrite=" + ms.canWrite + " canSeek=" + ms.canSeek )
        var x = FICrossChild()
        Console.println( "X cross-module child: canRead=" + x.canRead + " canWrite=" + x.canWrite + " canSeek=" + x.canSeek + " px=" + x.px )
        Console.println( "=== probe end ===" )

        Console.println( "=== F repro ===" )
        StdOutStream so = StdOutStream.shared()
        Console.println( "shared() ok canWrite=" + so.canWrite + " canRead=" + so.canRead )
        var b1 = ByteBuffer.fromString( "[F1] stdout write" )
        Console.println( "fromString ok readable=" + b1.readableBytes )
        so.write( b1 )
        Console.println( "F1 write ok" )
        var b2 = ByteBuffer.fromString( "\n" )
        so.write( b2 )
        Console.println( "newline write ok" )
        bool f2 = false
        label labF2
        {
            try so.read( ByteBuffer( 16 ) )
        }
        catch
        {
            f2 = true
        }
        Console.println( "F2 read reject = " + f2 )
        bool f3 = so.canWrite && so.canRead == false && so.canSeek == false
        Console.println( "F3 capability = " + f3 )
        Console.println( "=== F repro end ===" )
    }
}
