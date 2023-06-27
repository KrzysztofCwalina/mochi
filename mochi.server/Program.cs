var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Mochi Server");
app.Run();

app.MapGet("api/todos", () => {
    return Results.Ok(
        new { 
            todos = new object [] {
                new { task="hire electrician", assignedTo="Ela" }
            }
        }
    );
});