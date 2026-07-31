using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Endpoints;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/todos")
            .WithTags("Todos");

        group.MapGet("/", (ITodoService todoService) =>
            Results.Ok(ApiResponse<IReadOnlyList<TodoItem>>.Ok(todoService.GetAll())));

        group.MapPost("/", (CreateTodoRequest request, ITodoService todoService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(
                    ApiResponse<TodoItem>.Fail("Title is required.", ApiErrorCodes.ValidationError));
            }

            var todo = todoService.Create(request.Title);
            return Results.Created($"/api/todos/{todo.Id}", ApiResponse<TodoItem>.Ok(todo, "Created."));
        });

        group.MapPatch("/{id:int}/toggle", (int id, ITodoService todoService) =>
        {
            var updated = todoService.Toggle(id);
            return updated is null
                ? Results.NotFound(
                    ApiResponse<TodoItem>.Fail("Todo item was not found.", ApiErrorCodes.NotFound))
                : Results.Ok(ApiResponse<TodoItem>.Ok(updated));
        });

        group.MapDelete("/{id:int}", (int id, ITodoService todoService) =>
        {
            var deleted = todoService.Delete(id);
            return deleted
                ? Results.Ok(ApiResponse<object>.Ok(null, "Deleted."))
                : Results.NotFound(
                    ApiResponse<object>.Fail("Todo item was not found.", ApiErrorCodes.NotFound));
        });

        return endpoints;
    }
}
