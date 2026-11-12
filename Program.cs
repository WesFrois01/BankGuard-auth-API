using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Servers = [new ScalarServer("/")];
});

app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();