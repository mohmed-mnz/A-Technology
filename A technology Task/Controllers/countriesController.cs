using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace A_technology_Task.Controllers;

[Route("api/[controller]")]
[ApiController]
public class countriesController : ControllerBase
{
   
    private static readonly ConcurrentDictionary<string, bool> _blockedCountries = new();
    private static readonly List<BlockedAttemptLog> _blockedAttempts = new();
    private static readonly ConcurrentDictionary<string, DateTime> _tempBlockedCountries = new();



    [HttpPost("block-country")]
    public IActionResult BlockCountry(BlockedAttemptLog blockedAttempt)
    {
        if (_blockedCountries.ContainsKey(blockedAttempt.CountryCode))
            return StatusCode(403, "Country is already blocked");

        if (blockedAttempt.Blocked)
        {
            _blockedAttempts.Add(blockedAttempt);
            _blockedCountries.TryAdd(blockedAttempt.CountryCode, true);
            return Ok($"Country {blockedAttempt.CountryCode} blocked successfully.");
        }

        return BadRequest("Invalid request.");
    }

    [HttpDelete("block/{countryCode}")]
    public IActionResult UnblockCountry(string countryCode)
    {
        if (!_blockedCountries.TryRemove(countryCode, out _))
            return NotFound("Country not found in blocked list.");

        return Ok($"Country {countryCode} unblocked.");
    }

    [HttpGet("get-all-blocked-countries")]
    public IActionResult GetBlockedCountries([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? code = null)
    {
        var filteredCountries = new List<string>();


        if (page < 1 || pageSize < 1)
            return BadRequest("Invalid page or page size.");


        if (code == null)
        {
            filteredCountries = _blockedCountries.Keys
           .Skip((page - 1) * pageSize)
           .Take(pageSize)
           .ToList();

        }
        else
        {
            filteredCountries = _blockedCountries.Keys
           .Where(c => string.IsNullOrEmpty(code) || c.Contains(code, StringComparison.OrdinalIgnoreCase))
           .Skip((page - 1) * pageSize)
           .Take(pageSize)
           .ToList();
        }

        return Ok(new
        {
            TotalItems = _blockedCountries.Count,
            Page = page,
            PageSize = pageSize,
            Countries = filteredCountries
        });
    }

    [HttpPost("temporal-block")]
    public IActionResult TemporarilyBlockCountry([FromBody] TempBlockRequest request)
    {
        if (request.DurationMinutes < 1 || request.DurationMinutes > 1440)
            return BadRequest("Invalid duration. Must be between 1 and 1440 minutes.");

        if (_tempBlockedCountries.ContainsKey(request.CountryCode))
            return Conflict("Country is already temporarily blocked.");

        _tempBlockedCountries.TryAdd(request.CountryCode, DateTime.UtcNow.AddMinutes(request.DurationMinutes));

        return Ok($"Country {request.CountryCode} temporarily blocked.");
    }

}
