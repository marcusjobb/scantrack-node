using ScanTrackNode.Graph;
using ScanTrackNode.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Ladda stadsgrafen från CSV
var csvPath = Path.Combine(AppContext.BaseDirectory, "data", "cities.csv");
var graph = GraphLoader.Load(csvPath);
var dijkstra = new DijkstraService(graph);
builder.Services.AddSingleton(dijkstra);

// HTTP-klienter för NodeRegistry och PackageForwarder
builder.Services.AddHttpClient<NodeRegistry>();
builder.Services.AddHttpClient<PackageForwarder>();

builder.Services.AddSingleton<NodeRegistry>();
builder.Services.AddSingleton<PackageForwarder>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// Registrera noden mot Marcus's registry vid uppstart
var registry = app.Services.GetRequiredService<NodeRegistry>();
await registry.RegisterSelfAsync();

app.Run();
