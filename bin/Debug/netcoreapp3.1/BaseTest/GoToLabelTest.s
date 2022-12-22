GotoLabel
{
    static Fun()
    {
        a = 20;
        label ap1;
        i = 10;
        if i < 10{
            i++;
            goto ap1;
        }
        i = 20;
        goto ap2;
    }
}