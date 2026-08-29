using System.Text;
using System.Text.Json;

namespace ScanTrackNode.Services;

// Kommunicerar med Marcus's centrala registreringssserver.
// Registrerar sig själv vid start och hämtar nodlistan vid vidarebefordran.
public class NodeRegistry
{
    private readonly HttpClient _http;
    private readonly string _registryUrl;
    private readonly string _cityName;
    private readonly string _nodeUrl;
    private readonly ILogger<NodeRegistry> _logger;

    public NodeRegistry(HttpClient http, IConfiguration config, ILogger<NodeRegistry> logger)
    {
        _http = http;
        _registryUrl = config["REGISTRY_URL"]
            ?? throw new InvalidOperationException("Miljövariabel REGISTRY_URL saknas");
        _cityName = config["CITY_NAME"]
            ?? throw new InvalidOperationException("Miljövariabel CITY_NAME saknas");
        _nodeUrl = config["NODE_URL"]
            ?? throw new InvalidOperationException("Miljövariabel NODE_URL saknas");
        _logger = logger;
    }

    // Anropas vid uppstart — registrerar noden i det centrala registret.
    public async Task RegisterSelfAsync()
    {
        var body = JsonSerializer.Serialize(new { city = _cityName, url = _nodeUrl });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync($"{_registryUrl}/nodes", content);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Nod registrerad: {City} → {Url}", _cityName, _nodeUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kunde inte registrera noden mot {Registry}", _registryUrl);
        }
    }

    // Hämtar alla kända noder från registret.
    // Returnerar en dictionary: stad → URL
    public async Task<Dictionary<string, string>> GetNodesAsync()
    {
        try
        {
            var json = await _http.GetStringAsync($"{_registryUrl}/nodes");
            var nodes = JsonSerializer.Deserialize<List<NodeEntry>>(json) ?? new();
            return nodes.ToDictionary(
                n => n.City,
                n => n.Url,
                StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kunde inte hämta nodlista från {Registry}", _registryUrl);
            return new Dictionary<string, string>();
        }
    }

    private record NodeEntry(string City, string Url);
}
