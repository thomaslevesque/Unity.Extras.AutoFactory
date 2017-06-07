using FluentAssertions;
using Microsoft.Practices.Unity;
using Xunit;

namespace Unity.Extras.AutoFactory.Tests
{
    public class AutoFactoryExtensionTests
    {
        [Fact]
        public void Generated_factory_creates_instance_of_specified_type_with_specified_arguments()
        {
            // Arrange
            var container = new UnityContainer();
            container.AddNewExtension<AutoFactoryExtension>();
            container.RegisterType<IFooFactory>(new AutoFactory<Foo1>());

            // Act
            var factory = container.Resolve<IFooFactory>();
            var foo = factory.Create(42, "hello");

            // Assert
            foo.Should().BeOfType<Foo1>();
            foo.Id.Should().Be(42);
            foo.Name.Should().Be("hello");
        }

        [Fact]
        public void Generated_factory_injects_dependencies()
        {
            // Arrange
            var container = new UnityContainer();
            container.AddNewExtension<AutoFactoryExtension>();
            container.RegisterType<IDummyDependency, DummyDependency>();
            container.RegisterType<IFooFactory>(new AutoFactory<Foo2>());

            // Act
            var factory = container.Resolve<IFooFactory>();
            var foo = factory.Create(42, "hello");

            // Assert
            foo.Should().BeOfType<Foo2>();
            foo.Id.Should().Be(42);
            foo.Name.Should().Be("hello");
            ((Foo2)foo).Dummy.Should().BeOfType<DummyDependency>();
        }

        // ReSharper disable MemberCanBePrivate.Global
        public interface IFoo
        {
            int Id { get; }
            string Name { get; }
        }

        public interface IFooFactory
        {
            IFoo Create(int id, string name);
        }

        public interface IDummyDependency
        {
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        public class DummyDependency : IDummyDependency
        {
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        public class Foo1 : IFoo
        {
            public Foo1(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public int Id { get; }
            public string Name { get; }
        }

        public class Foo2 : IFoo
        {
            public Foo2(int id, string name, IDummyDependency dummy)
            {
                Id = id;
                Name = name;
                Dummy = dummy;
            }

            public int Id { get; }
            public string Name { get; }
            public IDummyDependency Dummy { get; }
        }

        // ReSharper restore MemberCanBePrivate.Global
    }
}
