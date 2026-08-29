using System.Text;
using System.Text.Json;
using ScanTrackNode.Models;

namespace ScanTrackNode.Services;

public class PackageForwarder
{
    private readonly NodeRegistry _registry;
    private readonly HttpClient _http;
    private readonly ILogger<PackageForwarder> _logger;

    public PackageForwarder(NodeRegistry registry, HttpClient http, ILogger<PackageForwarder> logger)
    {
        _registry = registry;
        _http = http;
        _logger = logger;
    }

    // DIN UPPGIFT: Vidarebefordra paketet till nästa nod i nätverket.
    //
    // Steg för steg:
    //   1. Hämta nodlistan från _registry.GetNodesAsync()
    //   2. Slå upp URL:en för 'nextCity' i listan
    //      - Om staden inte finns: logga ett fel och returnera false
    //   3. Serialisera 'package' till JSON
    //   4. Skicka en POST-request till {url}/paket
    //   5. Logga att du skickade vidare (stad + packageId)
    //   6. Returnera true om HTTP-svaret var lyckat (2xx), annars false
    //
    // Hjälpmedel:
    //   _logger.LogInformation("...", ...)   → loggar ett meddelande
    //   JsonSerializer.Serialize(package)    → konverterar objektet till JSON-sträng
    //   new StringContent(json, Encoding.UTF8, "application/json")  → HTTP-body
    //   await _http.PostAsync(url, content)  → skickar POST-request
    //   response.IsSuccessStatusCode         → true om 200–299
    public async Task<bool> ForwardAsync(Package package, string nextCity)
    {
        // TODO: implementera vidarebefordran
        throw new NotImplementedException("Implementera ForwardAsync — se kommentarerna ovan");
    }
}
