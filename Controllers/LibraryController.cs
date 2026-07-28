using amplyst_spotify_api.Exceptions;
using System.Security.Claims;
using amplyst_spotify_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace amplyst_spotify_api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class LibraryController(ILibraryService service) : ControllerBase
{
    [HttpGet("sync/{syncRunId}")]
    public async Task<IActionResult> GetSyncRequestStatus(Guid syncRunId)
    {
        string? userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var sync = await service.GetSyncDataAsync(syncRunId);
            if (sync == null)
            {
                return NotFound();
            }
            return Ok(sync);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("sync")]
    public async Task<IActionResult> CreateSyncRequest()
    {
        string? userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var sync = await service.CreateSyncDataAsync(userId);
            return Accepted(new { syncRunId = sync.SyncRunId, status = sync.Status.ToString() });
        }
        catch (PendingSyncException ex)
        {
            return Problem(
                type: "https://api.amplyst.example/problems/pending-sync",
                title: "Sync already pending",
                statusCode: StatusCodes.Status409Conflict,
                detail: ex.Message,
                instance: HttpContext.Request.Path);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}