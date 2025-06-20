import CSharp.System;

FB
{
    static Fun( array arr )
    {
        int first = 1
        int second = 1;
        next = 0
        n = arr.length;
        for i = 0, i < n，i++
        {
            if i == 0
            {
                next = first;
            }
            elif i == 1
            {
                next = second
            }
            else{
                next = first + second
                first = second
                second = next
            }

            Debug.Write("output$next “)
        }
    }
}