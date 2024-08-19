using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());
var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));
app.Use(async (context, next) =>
{
   Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}{DateTime.Now} started"); 
    await next();
     Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}{DateTime.Now} ended"); 
});


var todos = new List<Todo>();
app.MapGet("/todos", (ITaskService service) => service.GetTodos());

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) =>
{
    var task = service.GetTodoById(id);
    return task is null ? TypedResults.NotFound() : TypedResults.Ok(task);

});


app.MapPost("/todos", (Todo task, ITaskService service) =>
{
    // todos.Add(task);
    service.AddTodo(task);
    return TypedResults.Created($"/todos/{task.Id}", task);
}).AddEndpointFilter(async (context, next) =>
{
    var todo = context.Arguments.SingleOrDefault(arg => arg is Todo) as Todo;
    if (todo != null && todo.DueDate < DateTime.Now)
    {
        return TypedResults.BadRequest("DueDate cannot be in the past.");
    }
    return await next(context);
});

app.MapDelete("/todos/{id}", (int id , ITaskService service) =>
{
   
   service.DeleteTodoById(id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);
//  this file .net core is configured to run the web application and listen on port 5000
interface ITaskService
{
    List<Todo> GetTodos();
    Todo? GetTodoById(int id);
  Todo AddTodo(Todo task);
    void DeleteTodoById(int id);
}
class InMemoryTaskService : ITaskService{
    private List<Todo> todos = new List<Todo>();

    public List<Todo> GetTodos() => todos;
    public Todo? GetTodoById(int id) => todos.SingleOrDefault(t => t.Id == id);
    public Todo AddTodo(Todo task)
    {
        todos.Add(task);
        return task;
    }
    public void DeleteTodoById(int id)
    {
        todos.RemoveAll(t => t.Id == id);
    }
}