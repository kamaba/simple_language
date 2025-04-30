

public class File
{
    static int open( string path, string op )
    {
!if Windows

!elif Linux
!elif MAC
!endif
        return -1;
    }
    public static int write( string text )
    {
        return -1;
    }
    public static int close()
    {
        return 0
    }
}