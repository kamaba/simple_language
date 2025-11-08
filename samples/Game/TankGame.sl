import Std


Buttle extends Behaviour
{
    private _init_(){}

    _init_( int __attack, int __speed )
    {
        this._attack = __attack
        this._speed = __speed
    }
    _attack = 10
    _speed = 10
}

Tank extends Behaviour
{
    level = 0
    List<Buttle> _buttleList = new()
    void loadButtle()
    {
        int speed = 10
        int buttleCount = switch(this.level)
        {
            case 1..3{
                speed = 3
                tr 10
            }
            case 3..6{
                speed = 6
                tr 20
            }
            case 7..10{                
                speed = 9
                tr 30
            }
            default{
                tr 0
            }
        }
        Buttle btl = new(100,speed)
        _buttleList.allFull(btl)
    }
}

public class Map extends Behaviour
{
    enum EBlock
    {
        None
        Block,
        Mask
    }
    Color
    {
        _init_( float _r, float _g, float _b )
        {
            this.r = _r
            this.g = _g
            this.b = _b            
        }
        r = 0.0f
        g = 0.0f
        b = 0.0f
    }
    Cell
    {
        key = ""
        x = 0
        y = 0
        EBlock eBlock = EBlock.None
        Color color = new(){ r = 1.0f, g = 1.0f, b = 1.0f }
    }
    public List<Cell> _cellList = new()

    string _data = ""

    _init_( string _d )
    {
        this._data = _d;
    }
    parse()
    {
        j = Json(this_data)
        for c1 in j
        {
            var k = c1.key
            var val in c1.value
            {
                
                Color _color = new(val."c".0,val."c".1,val."c".2)
                Cell cell = new(){ key = k, x = val."x".toInt32(), y = val."y".toInt32(), eBlock = val."b".cast<EBlock>(), color = _color }
                this._cellList.add(cell)
            }
        }

    }
}

enum ELayout
{
    Center = 1
    Left = 2
    Right = 3
    Up = 4
    Down = 5
}

UIStart extends UIBehaviour
{
    _init_()
    {

    }

    loadUI()
    {
        Button okBtn = new("Start" )
        okBtn.setLayout( ELayout.Center )
        okBtn.onClick.AddListener( okCallBack )
        this.addChild( okBtn )

        
        Button resetBtn = new("Reset" )
        resetBtn.setLayout( ELayout.Center )
        resetBtn.onClick.AddListener( resetCallBack )
        this.addChild( resetBtn )

    }
    okCallBack()
    {
        Log.Info("okCallBack");

        GameLogic.instance.loadLevel(1)
    }
    resetCallBack()
    {
        Log.Info("resetCallBack");
    }
    showUI()
    {
        setActive( true )
    }
}
UIMain extends UIBehaviour{

    update()
    {
        if Input.key( EKey.Up ) 
        || Input.key( EKey.Down )
        || Input.key( EKey.Left )
        || Input.key( EKey.Right )
        {

        }
    }
}

@instance()
GameLogic
{
    UIStart _uiStart = new();
    Map _map = null;
    Tank _tank = null
    init()
    {
        showStartUI()
    }
    showStartUI()
    {
        this._uiStart.loadUI()
        this._uiStart.ShowUI()
    }
    loadLevel( short level )
    {
        File f = new('level/level_{level}.level')

        string all = f.readAllText()

        this._map = Map(all)
        this._map.parse()

    }
    loadTank()
    {
        this._tank = new("tank")
        this._tank.level = 12
        this._hp = 100
        this.loadButtle();
    }
    static mainLoigc()
    {
        instance.init();
    }
}