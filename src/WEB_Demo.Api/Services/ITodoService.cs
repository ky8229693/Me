using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public interface ITodoService
{
    IReadOnlyList<TodoItem> GetAll();

    TodoItem Create(string title);

    TodoItem? Toggle(int id);

    bool Delete(int id);
}
