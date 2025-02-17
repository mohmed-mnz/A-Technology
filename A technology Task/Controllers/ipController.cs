using Microsoft.AspNetCore.Mvc;
using Model;
using System.Collections.Concurrent;
using System.Net;
using Newtonsoft.Json.Linq;

namespace A_technology_Task.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ipController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    private static readonly ConcurrentDictionary<string, bool> _blockedCountries = new();
    private static readonly List<BlockedAttemptLog> _blockedAttempts = new();
    private static readonly ConcurrentDictionary<string, DateTime> _tempBlockedCountries = new();
    private static readonly ConcurrentDictionary<string, string> _ipCache = new();

    public ipController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = configuration["IPGeolocation:ApiKey"] ?? throw new ArgumentNullException("API Key is missing in configuration!");
    }

  

    [HttpGet("look-up")]
    public async Task<IActionResult> LookupIP([FromQuery] string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!IPAddress.TryParse(ipAddress, out _))
            return BadRequest("Invalid IP address.");

        if (_ipCache.TryGetValue(ipAddress, out var cachedResponse))
            return Ok(cachedResponse);

        var response = await _httpClient.GetAsync($"https://api.ipgeolocation.io/ipgeo?apiKey={_apiKey}&ip={ipAddress}");

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            return StatusCode(429, "Rate limit exceeded. Please try again later.");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        _ipCache[ipAddress] = result;

        return Ok(result);
    }

    [HttpGet("check-block")]
    public async Task<IActionResult> CheckIPBlock()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ipAddress))
            return BadRequest("Could not retrieve IP address.");

        string result;
        if (!_ipCache.TryGetValue(ipAddress, out result!))
        {
            var response = await _httpClient.GetAsync($"https://api.ipgeolocation.io/ipgeo?apiKey={_apiKey}&ip={ipAddress}");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return StatusCode(429, "Rate limit exceeded. Please try again later.");

            response.EnsureSuccessStatusCode();
            result = await response.Content.ReadAsStringAsync();
            _ipCache[ipAddress] = result;
        }

        var countryCode = JObject.Parse(result)["country_code"]?.ToString();
        if (!string.IsNullOrEmpty(countryCode) && (_blockedCountries.ContainsKey(countryCode) || IsTemporarilyBlocked(countryCode)))
        {
            _blockedAttempts.Add(new BlockedAttemptLog
            {
                IpAddress = ipAddress,
                CountryCode = countryCode,
                Timestamp = DateTime.UtcNow,
                UserAgent = Request.Headers["User-Agent"].ToString(),
                Blocked = true
            });
            return Forbid("Your country is blocked.");
        }

        return Ok("IP is not blocked.");
    }

   

    private bool IsTemporarilyBlocked(string countryCode)
    {
        if (_tempBlockedCountries.TryGetValue(countryCode, out var expiration))
        {
            if (DateTime.UtcNow > expiration)
            {
                _tempBlockedCountries.TryRemove(countryCode, out _);
                return false;
            }
            return true;
        }
        return false;
    }
}
