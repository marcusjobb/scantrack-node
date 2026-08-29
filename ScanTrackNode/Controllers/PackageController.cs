using Microsoft.AspNetCore.Mvc;
using ScanTrackNode.Graph;
using ScanTrackNode.Models;
using ScanTrackNode.Services;

namespace ScanTrackNode.Controllers;

[ApiController]
[Route("[controller]")]
public class PaketController : ControllerBase
{
    private readonly DijkstraService _dijkstra;
    private readonly PackageForwarder _forwarder;
    private readonly IConfiguration _config;
    private readonly ILogger<PaketController> _logger;

    // Enkel in-memory lagring — försvinner vid omstart (det är OK för övningen)
    private static readonly List<Package> _received = new();

    public PaketController(
        DijkstraService dijkstra,
        PackageForwarder forwarder,
        IConfiguration config,
        ILogger<PaketController> logger)
    {
        _dijkstra = dijkstra;
        _forwarder = forwarder;
        _config = config;
        _logger = logger;
    }

    // POST /paket — tar emot ett paket från en annan nod
    [HttpPost]
    public async Task<IActionResult> TaEmot([FromBody] Package package)
    {
        var cityName = _config["CITY_NAME"] ?? "Okänd";

        // Lägg till denna stad i historiken
        package.History.Add(cityName);

        _logger.LogInformation(
            "Paket {Id} anlände till {City}. Historik: {History}. Destination: {Dest}",
            package.PackageId, cityName, string.Join(" → ", package.History), package.Destination);

        // Är vi destinationen?
        if (string.Equals(cityName, package.Destination, StringComparison.OrdinalIgnoreCase))
        {
            _received.Add(package);
            _logger.LogInformation("Paket {Id} levererat till {City}!", package.PackageId, cityName);
            return Ok(new { status = "levererat", stad = cityName, paket = package });
        }

        // Hitta nästa hopp med Dijkstra
        var nextHop = _dijkstra.NextHop(cityName, package.Destination, package.History);

        if (nextHop == null)
        {
            _logger.LogWarning(
                "Ingen väg hittades från {City} till {Dest} (historik: {History})",
                cityName, package.Destination, string.Join(", ", package.History));
            return UnprocessableEntity(new { fel = "Ingen väg hittades", historik = package.History });
        }

        _logger.LogInformation("Vidarebefordrar {Id} från {City} till {Next}", package.PackageId, cityName, nextHop);

        var lyckades = await _forwarder.ForwardAsync(package, nextHop);

        if (!lyckades)
            return StatusCode(502, new { fel = $"Kunde inte nå {nextHop}" });

        return Ok(new { status = "vidarebefordrat", nästaHopp = nextHop, paket = package });
    }

    // GET /paket — lista alla paket som levererats till denna nod
    [HttpGet]
    public IActionResult Lista() =>
        Ok(new { stad = _config["CITY_NAME"], mottagna = _received });

    // GET /status — nodens hälsostatus
    [HttpGet("/status")]
    public IActionResult Status() =>
        Ok(new
        {
            stad = _config["CITY_NAME"],
            url = _config["NODE_URL"],
            mottagna = _received.Count,
            uppe_sedan = DateTime.UtcNow
        });

    // GET /route?from=Göteborg&to=Sundsvall — beräkna och visa hela rutten
    [HttpGet("/route")]
    public IActionResult VisaRutt([FromQuery] string from, [FromQuery] string to)
    {
        var route = _dijkstra.FullRoute(from, to, Enumerable.Empty<string>());

        if (route.Count < 2)
            return NotFound(new { fel = $"Ingen rutt hittades från {from} till {to}" });

        return Ok(new { från = from, till = to, rutt = route, antal_stopp = route.Count - 2 });
    }
}
