using Azure.FX.Applications;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
builder.Services.AddCloudMachine();

app.MapGet("/", () => "Mochi Server");

app.Run();

app.MapGet("api/todos", (CloudMachineRequest request) => {
    return Results.Ok(
        new { 
            todos = new object [] {
                new { task="hire electrician", assignedTo="Ela" }
            }
        }
    );
});