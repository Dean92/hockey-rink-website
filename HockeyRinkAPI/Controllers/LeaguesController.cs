using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;


namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/leagues")]
[Authorize]
public class LeaguesController : ControllerBase
{
    private readonly AppDbContext _db;

    public LeaguesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetLeagues()
    {
        var leagues = _db.Leagues.ToList();
        return Ok(leagues);
    }
}

