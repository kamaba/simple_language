 
        
        #obj = { firstname = "john"; lastname = "doe"; int age = 50; color1 = { a = 1; r = 0.5; g = 0.1; b = 0.2; }; color2 = Color(){ a = 0.5, r = 1.0 } };    
        
        na = "have";

        tip = "Show: {0}   OK! @na";
        for( int i = 0; i < 10; i++ )
        {
            Console.Write( tip.Format( i.ToString() ) );
            Console.Write( tip, i );
        }
#label( dingo );
        bool flag = true;
        dofor( flag )
        {
            flag = false;
            Console.Write( "Flag: " + flag );
        }       

        {
            x2 = "MM";
            Console.Write( "OK" + x2 );
        }
        arr = [1,2,3,4];
        for( a in arr )
        {
            if( a == 2 )
            {
                continue;
            }
            else if( a == 4 )
                break;
            else if( a == 3 )
                break dingo;
            else
            {
                Console.Write(" mtk： " + a.ToString() );
            }
        }
        x = 2;
        x2 = 3;
        switch()
        {
            case x > 10 && x < 20: break;
            case x2 == 100: m = 100; break;            
            case x2 == 20: x = 10; break;
            case x < 20:{ }; break;
            case x > 40: { m = 200; break; }
            case x > 80: m = 30;
            case x > 100: m = 40;
            default:{ m = 0; } 
        }
        switch( t )
        {
            case int:{ Print("this is internal type!"); }break;
            case float:{}break;
            default:break;
        }
        obj = Class1();
        #switch( obj.Type() )
        #{
            
        #}
        mxx = 30;
        #switch( mxx )
        #{
        #    case 20:{ mxx = 100; }break;
        #    case 100:mxx = 200; break;
        #}
        arr = [1,2,3][22,33,44];
       
        #Count = Add( 1, Add( 1, GetA().a ) ) - X * 2 - Add( 1, (-25+ (32 - (2*2) ) ) ) + GetA().a;

        #Console = GetHandle( "System.Console", 1 );   #变成表达示去看待

        #Console.Write( msg );

        Set( Count, GetA().a + 30 / 1.3 + 100%10 );      











    xs = 3123123.ToString();
    x6 = 1231231231233123.ToLong();
    #Class2 bingo;
    #{
    #}   
    #Y;
    #X;
    #M = null;  #这是一个空对象 相当于object M = null;
    #Count = 20;
    #private f = 12.33;
    #private f2 = 12.35;
    #类的关联，在debug中可以显示 ，有树关系，拓扑关系等。
    #函数的运行时间统计 在函数前增加[Profile()标签]
    #导出类的说明,支持导出到javascript
    #调试类 帧数据记录  帧数据变动表  帧数据运行概念
    #类设计 相关 申请1-400字节之间的class，通过在class中，一个分配序列保存，他们之间默认对象信息.
    #在函数后可加附加执行语句 如  [release]func<x,y>(a,b).[auto]x<tt>(xx);        表示立马释放掉函数中的内存分配
    #如果变量使用 @可以直接读取内存地址，不进行检查  而使用.则以传统方式访问内存 例  a.x.c   而使用  a..x..c &a.&x.&c  a->x->c
    #支持51 x86机器运行 以后
    #! 这段是区域多行注释 !#
    #typeof( Y )    #返回一个float
    #编译解析过程 先读所有要编译文件，然后读取行号与大小，然后进行分类，然后平行N个进度，然后放到多线程中去解析。
    #如果崩溃可以定位，是那一帧的，那个位置 什么原因导致的崩溃 例: 12001 C.Del(); C执行了手动删除 12003 使用C时，发现对象为空对象 隔音时间等都可以显示


        
    