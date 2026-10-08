using AuthApi.Data;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]

public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var dbOk = await _db.Database.CanConnectAsync();
        return Ok(new
        {
            api = "running",
            database = dbOk ? "Connected" : "Unreachable"
        });
    }
}
