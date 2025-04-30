
!IF WINDOW
import Window
!ENDIF

public class Time
{
    static int clock()
    {
!IF WINDOW
        IntPtr handle = Window.Time();

        int clock = handle.clock();

        return clock;
!ENDIF
        return 0;
    }
}