# Unity.Extras.AutoFactory

A Unity extension to automatically generate strongly typed factories at runtime.

For instance, if you have these types:

```csharp
public interface IFoo
{
    int Id { get; }
    string Name { get; }
}

public interface IFooFactory
{
    IFoo Create(int id, string name);
}

public class Foo : IFoo
{
    private readonly IMyDependency _dependency;

    public int Id { get; }
    public string Name { get; }

    public Foo(int id, string name, IMyDependency dependency)
    {
        Id = id;
        Name = name;
        _dependency = dependency;
    }
}
```

Unity.Extras.AutoFactory can automatically generate an implementation of `IFooFactory` that will create instances of `Foo` with the specified parameters and inject the required dependencies:

```csharp
var container = new UnityContainer();

// Activate extension
container.AddNewExtension<AutoFactoryExtension>();

// Register dependencies
container.RegisterType<IMyDependency, MyDependency>();

// Register automatic factory
// Note how the concrete IFoo type is specified
container.RegisterType<IFooFactory>(new AutoFactory<Foo>());

// This will generate the appropriate factory type
var factory = container.Resolve<IFooFactory>();

var foo = factory.Create(42, "test");
```
