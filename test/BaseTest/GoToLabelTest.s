GotoLabel
{
    static Fun()
    {
        a = 20;
        i = 10;
        label ap1;
        if i < 10 {
            i++;
            goto ap1;
        }
        i = 20;
        goto ap2;
    }
}