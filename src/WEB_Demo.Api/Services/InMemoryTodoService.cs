using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed class InMemoryTodoService : ITodoService
{
    private readonly List<TodoItem> _todos =
    [
        new(1, "Create .NET Web API", true),
        new(2, "Create Vue 3 client", true),
        new(3, "Connect database", false)
    ];

    public IReadOnlyList<TodoItem> GetAll()
    {
        return _todos;
    }

    public TodoItem Create(string title)
    {
        var nextId = _todos.Count == 0 ? 1 : _todos.Max(todo => todo.Id) + 1;
        var todo = new TodoItem(nextId, title.Trim(), false);
        _todos.Add(todo);

        return todo;
    }

    public TodoItem? Toggle(int id)
    {
        var index = _todos.FindIndex(todo => todo.Id == id);
        if (index < 0)
        {
            return null;
        }

        var updated = _todos[index] with { IsDone = !_todos[index].IsDone };
        _todos[index] = updated;

        return updated;
    }

    public bool Delete(int id)
    {
        return _todos.RemoveAll(todo => todo.Id == id) > 0;
    }
}
