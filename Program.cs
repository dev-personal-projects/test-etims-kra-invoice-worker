var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Health check endpoint
app.MapGet("/", () => Results.Ok(new { 
    service = "KRA eTIMS Invoice Worker", 
    status = "running",
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("Health");

app.Run();
