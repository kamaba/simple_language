
Application.CI2
{

}
C22::Application.CI2
{
    Y = 10;
    int getback(){ return 100; }
    interface int C2(){ return Y; }
}
C23 :: C22
{
    M = 100;
    interface int C2(){ return M; }
}

CI3
{
    interface object C3();
}

Applicaction.C3 :: C22 interface Application.CI2, CI3
{
    interface int C2(){ return 100; }
    object C3(){ return Object.New(); }
}
Application.C34 :: C22 interface CI3
{
    
}

#!
函数使用调用本类函数中 直接就是Fun()
如果使用this.Fun() = Fun(); 如果使用父类中的某个函数，则使用base.Fun()
!#
