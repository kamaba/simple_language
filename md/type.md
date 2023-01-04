# 语言的基本类型
语言所有都是对象概念，所以即使是像csharp中的值类型，所以

—————————————————————————————————————————————————————————

## 项目的创建
1. 



创建后的工程文件:
```csharp
file:test_project.sp

ProjectEnter
{
    static Main()
    {    
        r = Rectangle(10.0f, 20.0f );
        r.Display();

    }
    static Test()
    {
        r = Rectangle();
        r.DefaultLW();
        r.Display();
    }
}