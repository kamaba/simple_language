NumberTest
{
    static Fun()
    {
        i1 = 1;
        i2 = 1i;
        i3 = 11i;
        ui1 = 10ui;
        f0 = 2.0;         #  end:null point:1
        f1 = 2.321f;      #  end:f point:1
        f2 = 2f;          # end:f point:0  报错
        f4 = 2i.ToString();   # end:t point:1
        f5 = 2.0f.ToString();    #
        f6 = 23.223d.ToFloat();
        ul111 = 0xff22.ToString();
        c = 1+1.3;
        d = 2.0 * 3.2132123123123;    
        d1 = 3.0d;
        s1 = 1s;
        s2 = 2us;        
        L1 = 10L;
        UL1 = 12123123123123123123uL;
        h1 = 0x123f;
        h2 = 0xaff;
        h3 = 0x1abfe;    #报错
        o1 = 0o1132;
        o2 = 0o23;      #报错
        bin1 = 0b1111_1111;
        #!
        !#
    }
}