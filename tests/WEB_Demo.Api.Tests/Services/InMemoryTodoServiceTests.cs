using FluentAssertions;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Services;

public sealed class InMemoryTodoServiceTests
{
    [Fact]
    public void GetAll_ReturnsDefaultTodos()
    {
        var service = new InMemoryTodoService();

        var todos = service.GetAll();

        todos.Should().HaveCount(3);
        todos.Select(todo => todo.Title).Should().Contain([
            "Create .NET Web API",
            "Create Vue 3 client",
            "Connect database"
        ]);
    }

    [Fact]
    public void Create_AddsTodoWithNextId()
    {
        var service = new InMemoryTodoService();

        var created = service.Create("Write tests");

        created.Id.Should().Be(4);
        created.Title.Should().Be("Write tests");
        created.IsDone.Should().BeFalse();
        service.GetAll().Should().Contain(created);
    }

    [Fact]
    public void Toggle_WhenTodoExists_TogglesIsDone()
    {
        var service = new InMemoryTodoService();

        var updated = service.Toggle(3);

        updated.Should().NotBeNull();
        updated!.Id.Should().Be(3);
        updated.IsDone.Should().BeTrue();
    }

    [Fact]
    public void Toggle_WhenTodoDoesNotExist_ReturnsNull()
    {
        var service = new InMemoryTodoService();

        var updated = service.Toggle(999);

        updated.Should().BeNull();
    }

    [Fact]
    public void Delete_ReturnsWhetherTodoWasRemoved()
    {
        var service = new InMemoryTodoService();

        var deleted = service.Delete(1);
        var deletedAgain = service.Delete(1);

        deleted.Should().BeTrue();
        deletedAgain.Should().BeFalse();
        service.GetAll().Should().NotContain(todo => todo.Id == 1);
    }
}
