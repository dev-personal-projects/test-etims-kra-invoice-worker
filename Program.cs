using Microsoft.Extensions.Options;
using test_etims_kra_invoice_worker;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Configure KRA eTIMS settings
builder.Services.Configure<KraEtimsConfig>(
    builder.Configuration.GetSection("KraEtims"));

// Register HttpClient factory
builder.Services.AddHttpClient("KraEtims", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IOptions<KraEtimsConfig>>().Value;
    if (!string.IsNullOrEmpty(config.BaseUrl))
    {
        client.BaseAddress = new Uri(config.BaseUrl);
    }
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register KRA eTIMS service
builder.Services.AddScoped<KraEtimsService>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("KraEtims");
    var config = serviceProvider.GetRequiredService<IOptions<KraEtimsConfig>>().Value;
    var logger = serviceProvider.GetService<ILogger<KraEtimsService>>();
    return new KraEtimsService(httpClient, config, logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map all API endpoints
app.MapInvoiceEndpoints();

app.Run();
