import Std

LoginWindow extends UIWindows
{
    Button _logicBtn = new()
    _init_()
    {
        _logicBtn.text = "Login"
        _logicBtn.rect = Rect(150, 200, 100, 40)
        _logicBtn.onClick = this.Login
        this.addChild(_logicBtn)
    }

    Login()
    {
        print("Login button clicked")
    }
}

UIWindowsLogic
{
    static fun()
    {
        logicw = LoginWindow()
        logicw.title = "Login Window"
        logicw.rect = Rect(100, 100, 400, 300)
        logicw.show()
    }
}