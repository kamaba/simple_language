
# ============================================================
# 接口测试用例 - 覆盖接口声明、实现、多态、继承等场景
# ============================================================

# --- 1. 单接口基本实现 ---
interface IShape
{
    double area()
    string name()
}

class Circle interface IShape
{
    radius = 0.0d

    _init_( double r )
    {
        this.radius = r
    }

    override double area()
    {
        ret 3.14d * this.radius * this.radius
    }

    override string name()
    {
        ret "Circle"
    }
}

class Rectangle interface IShape
{
    width = 0.0d
    height = 0.0d

    _init_( double w, double h )
    {
        this.width = w
        this.height = h
    }

    override double area()
    {
        ret this.width * this.height
    }

    override string name()
    {
        ret "Rectangle"
    }
}

# --- 2. 多接口实现 ---
interface IComparable
{
    int compareTo( object other )
}

interface IDescribable
{
    string describe()
}

class Product interface IComparable, IDescribable
{
    pid = 0
    pname = ""
    price = 0.0d

    _init_( int id, string n, double p )
    {
        this.pid = id
        this.pname = n
        this.price = p
    }

    override int compareTo( object other )
    {
        Product o = other as Product
        if o == null { ret 1 }
        if this.price > o.price { ret 1 }
        elif this.price < o.price { ret -1 }
        else { ret 0 }
    }

    override string describe()
    {
        ret "Product[id=" + this.pid.toString() + ", name=" + this.pname + ", price=" + this.price.toString() + "]"
    }
}

# --- 3. 接口继承接口 ---
interface IAnimal
{
    string sound()
}

interface IPet extends IAnimal
{
    string petName()
}

class Dog interface IPet
{
    dogName = ""

    _init_( string n )
    {
        this.dogName = n
    }

    override string sound()
    {
        ret "Woof"
    }

    override string petName()
    {
        ret this.dogName
    }
}

class Cat interface IPet
{
    catName = ""

    _init_( string n )
    {
        this.catName = n
    }

    override string sound()
    {
        ret "Meow"
    }

    override string petName()
    {
        ret this.catName
    }
}

# --- 4. 接口引用多态调用 ---
class ShapeFactory
{
    static IShape createCircle( double r )
    {
        ret Circle( r )
    }

    static IShape createRectangle( double w, double h )
    {
        ret Rectangle( w, h )
    }
}

# --- 5. 接口方法带参数和返回值 ---
interface ICalculator
{
    int add( int a, int b )
    int subtract( int a, int b )
    int multiply( int a, int b )
}

class BasicCalculator interface ICalculator
{
    override int add( int a, int b )
    {
        ret a + b
    }

    override int subtract( int a, int b )
    {
        ret a - b
    }

    override int multiply( int a, int b )
    {
        ret a * b
    }
}

# --- 6. 接口与类继承组合 ---
interface IWalkable
{
    void walk()
}

class AnimalBase
{
    legs = 4

    int getLegs()
    {
        ret this.legs
    }
}

class DogAnimal extends AnimalBase interface IWalkable
{
    override void walk()
    {
        global.println( "DogAnimal walking on " + this.legs.toString() + " legs" )
    }
}

# ============================================================
# 入口测试类
# ============================================================
GenClass_Interface
{
    static testBasicInterface()
    {
        global.println( "=== testBasicInterface ===" )

        IShape s1 = Circle( 5.0d )
        IShape s2 = Rectangle( 3.0d, 4.0d )

        global.println( s1.name() + " area=" + s1.area().toString() )
        global.println( s2.name() + " area=" + s2.area().toString() )
    }

    static testMultiInterface()
    {
        global.println( "=== testMultiInterface ===" )

        Product p1 = Product( 1, "Apple", 5.5d )
        Product p2 = Product( 2, "Banana", 3.2d )
        Product p3 = Product( 3, "Cherry", 5.5d )

        IComparable cmp = p1
        global.println( "p1 vs p2: " + cmp.compareTo( p2 ).toString() )
        global.println( "p1 vs p3: " + cmp.compareTo( p3 ).toString() )

        IDescribable desc = p1
        global.println( desc.describe() )
    }

    static testInterfaceInheritance()
    {
        global.println( "=== testInterfaceInheritance ===" )

        IPet dog = Dog( "Buddy" )
        IPet cat = Cat( "Whiskers" )

        global.println( dog.petName() + " says " + dog.sound() )
        global.println( cat.petName() + " says " + cat.sound() )
    }

    static testPolymorphism()
    {
        global.println( "=== testPolymorphism ===" )

        IShape[] shapes = [ Circle( 1.0d ), Rectangle( 2.0d, 3.0d ), Circle( 10.0d ) ]
        for s in shapes
        {
            global.println( s.name() + " area=" + s.area().toString() )
        }
    }

    static testFactoryPattern()
    {
        global.println( "=== testFactoryPattern ===" )

        IShape c = ShapeFactory.createCircle( 7.0d )
        IShape r = ShapeFactory.createRectangle( 6.0d, 8.0d )

        global.println( "factory circle area=" + c.area().toString() )
        global.println( "factory rect area=" + r.area().toString() )
    }

    static testCalculatorInterface()
    {
        global.println( "=== testCalculatorInterface ===" )

        ICalculator calc = BasicCalculator()
        global.println( "add(3,5)=" + calc.add( 3, 5 ).toString() )
        global.println( "sub(10,4)=" + calc.subtract( 10, 4 ).toString() )
        global.println( "mul(6,7)=" + calc.multiply( 6, 7 ).toString() )
    }

    static testInterfaceWithInheritance()
    {
        global.println( "=== testInterfaceWithInheritance ===" )

        DogAnimal da = DogAnimal()
        da.walk()
        global.println( "legs=" + da.getLegs().toString() )
    }

    static testInterfaceNullCheck()
    {
        global.println( "=== testInterfaceNullCheck ===" )

        Product p = Product( 10, "Test", 1.0d )
        IComparable cmp = p
        object obj = p

        global.println( "p is IComparable: true" )

        Product p2 = obj as Product
        if p2 != null
        {
            global.println( "as Product success: " + p2.describe() )
        }
        else
        {
            global.println( "as Product failed" )
        }
    }

    static testInterfaceArrayAndLoop()
    {
        global.println( "=== testInterfaceArrayAndLoop ===" )

        IPet[] pets = [ Dog( "Rex" ), Cat( "Tom" ), Dog( "Max" ) ]
        for pet in pets
        {
            global.println( pet.petName() + " -> " + pet.sound() )
        }
    }

    static fun()
    {
        GenClass_Interface.testBasicInterface()
        GenClass_Interface.testMultiInterface()
        GenClass_Interface.testInterfaceInheritance()
        GenClass_Interface.testPolymorphism()
        GenClass_Interface.testFactoryPattern()
        GenClass_Interface.testCalculatorInterface()
        GenClass_Interface.testInterfaceWithInheritance()
        GenClass_Interface.testInterfaceNullCheck()
        GenClass_Interface.testInterfaceArrayAndLoop()
    }
}

# ============================================================
# 接口规则说明
# ============================================================
# 1. 接口声明: interface IName { 方法签名 } —— 无方法体
# 2. 类实现接口: class X interface IName —— 必须用 override 实现所有接口方法
# 3. 多接口实现: class X interface IA, IB —— 逗号分隔
# 4. 接口继承: interface IB extends IA —— 子接口包含父接口方法
# 5. 接口引用多态: IA a = Impl() —— 运行时按实际类型 CallDynamic 分发
# 6. 接口方法调用: 通过接口引用调用方法，VM 使用 CallDynamic 按名查找
# 7. as/is 接口: 支持 obj as IName 类型转换
# 8. 接口与继承组合: class X extends Base interface IA —— 可同时继承父类和实现接口
# 9. 方法级 interface 修饰符: class内 interface void foo(){} 提供默认实现
# 10. 未实现接口方法会报错: CheckInterface 强制校验
