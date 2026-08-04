using System.Security.Claims;
using amplyst_spotify_api.Exceptions;
using amplyst_spotify_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace amplyst_spotify_api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class ImportController(IImportService service, ILogger<ImportController> logger) : ControllerBase
{
    [HttpGet("jobs/{jobId}")]
    public async Task<IActionResult> GetImportJobStatus(Guid jobId)
    {
        using var jobScope = logger.BeginScope(new Dictionary<string, object> { ["JobId"] = jobId });

        string? userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var response = await service.GetImportJobByIdAsync(jobId);
            if (response == null)
            {
                return NotFound();
            }
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost()]
    public async Task<IActionResult> CreateImportJob()
    {
        string? userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var job = await service.CreateImportJobAsync(userId);
            using var jobScope = logger.BeginScope(new Dictionary<string, object> { ["JobId"] = job.Id });
            logger.LogInformation("Created import job {JobId} for user {UserId}.", job.Id, userId);
            return Accepted(new { id = job.Id, status = job.Status.ToString() });
        }
        catch (ImportAlreadyInProgressException ex)
        {
            return Problem(
                type: "https://api.amplyst.example/problems/import-in-progress",
                title: "There is an import already in progress",
                statusCode: StatusCodes.Status409Conflict,
                detail: ex.Message,
                instance: HttpContext.Request.Path
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}