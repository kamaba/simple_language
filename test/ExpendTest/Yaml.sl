function_test_class
{
    fun1()
    {
        m = 20;
        fn( a )
        {
            b = if a == 30{ tr 100 + m; }
            if( b > 90 )
            {    ret a+100; }
            fn2( s1, s2 )
            {
                if( s1 > 100 )
                {ret 20;}    
                else
                {
                    ret 100;
                }
            }

            ret fn2(a, 10 ) - 100;
        }


        fn.fn2( 10, 20 );
    }
}