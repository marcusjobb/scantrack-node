using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScanTrackNode.Services
{
    public class HeartbeatService : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<HeartbeatService> _logger;

        public HeartbeatService(IConfiguration config, ILogger<HeartbeatService> logger)
        {
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(1), ct);
                // skicka POST /nodes till registret

                using var client = new HttpClient();
                var city = _config["CITY_NAME"] ?? "Okänd";
                var registryURL = _config["REGISTRY_URL"] ?? "Okänd";

                var content = new StringContent(
                    $"{{\"city\": \"{city}\"}}",
                    System.Text.Encoding.UTF8, "application/json");

                try
                {
                    _logger.LogInformation("Heartbeat skickas till {RegistryURL} från {City}", registryURL, city);

                    var res = await client.PostAsync($"{registryURL}/nodes", content);

                    if (res.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Heartbeat svarade med statuskod {StatusCode}",
                        res.StatusCode);
                    }
                    else
                    {
                        _logger.LogInformation("Misslyckades med att skicka heartbeat till registret, StatusKod: {StatusCode}",
                        res.StatusCode);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fel vid heartbeat: {Message}", ex.Message);
                }
            }
        }
    }
}