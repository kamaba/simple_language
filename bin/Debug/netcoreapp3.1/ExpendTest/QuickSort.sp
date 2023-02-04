import CSharp.System;
ProjectEnter
{
    BubbleSort( int[] arr )
    {       
        for i = 0, i < arr.len-1, i++
        {
            for j = i + 1, j < arr.len, j++ 
            {
                if arr.@i < arr.@j
                {
                    t = arr[j];
                    arr[j] = arr[i];
                    arr[i] = t;
                }
            }
        }
        Console.Write("Bubble整理后的序列");
        for v in arr
        {
            Console.Write( v );
            if v.next == null
            {
                Console.Write( "," );
            }
        }
        Console.WriteLine();
    }
    SelectedSort( int[] arr )
    {        

        for i = 0, i < arr.len-1, i++ 
        {
            minIndex = i;
            for j = 1, j < arr.len, j++
            {
                if( arr[j] < arr[minIndex] )
                {
                    minIndex = i;
                }
            }
            if minIndex != i
            {
                t = arr[minIndex];
                arr[minIndex] = arr[i];
                arr[i] = t;
            }
        }
        Console.Write("Selected整理后的序列");
        for v in arr
        {
            Console.Write( v );
            if v.next == null
            {
                Console.Write( "," );
            }
        }
        Console.WriteLine();
    }
    InsertSort( Array<int> arr )
    {
        for i = 1, i < arr.len, i++
        {
            int current = arr[i];
            int preIndex = i-1;
            while preIndex >= 0 && arr[preIndex] > current 
            {
                arr[preIndex+1] = arr[preIndex];
                preIndex--;
            }
            arr[preIndex+1] = current;
        }   
    }
    MergeSort( int[] arr )
    {
        var merge( int[] left, int[] right )
        {
            int[] result;
            while left.len && right.len
            {
                if( left.@0 <= right.@0 )
                {
                    result.Add( left.Shift() );
                }
                else{
                    result.Add( right.Shift() );
                }
            }
            if( left.len > 0 )
            {
                for i = 0, i < left.len, i++ 
                {
                    result.Add( left.Shift() );
                }
            }
            if right.len > 0
            {
                for i = 0, i < right.len, i++
                {
                    result.Add(right.Shift() );
                }
            }
            return result;
        }
        int mid = arr.len / 2;
        left = List<int>();
        List<int> right = ();
        for i = 0, i < mid, i++
        {
            left.Add(arr[i] );
        }
        for j = mid, j < arr.len, j++
        {
            right.Add( arr[j] );
        }
        left = MergeSort( left )
        right = MergeSort( right )
        return merge( left, right )
    }
    static Main()
    {
        arr = int[]{ 10,32, 15, 31, 2, 44, 19, 26, 30, 8, 22, 1, 6 };
        Console.Write("原来的序列");
        for v in arr
        {
            Console.Write( v );
            if v.next == null
            {
                Console.Write( "," );
            }
        }
        BubbleSort( arr );
    }
}