using Microsoft.AspNetCore.Mvc;
using Model;

namespace A_technology_Task.Controllers;

[Route("api/[controller]")]
[ApiController]
public class logsController : ControllerBase
{
    private static readonly List<BlockedAttemptLog> _blockedAttempts = new();

    [HttpGet("blocked-attempts")]
    public IActionResult GetBlockedLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var logs = _blockedAttempts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            TotalItems = _blockedAttempts.Count,
            Page = page,
            PageSize = pageSize,
            Logs = logs
        });
    }
}
