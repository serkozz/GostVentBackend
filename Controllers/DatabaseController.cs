using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services;
using Types.Enums;

namespace Backend.Controllers;

[ApiController]
[Route("{controller}")]
public class DatabaseController : ControllerBase
{
    // ROUTE EXAMPLE ----- localhost:5072/database

    private readonly ILogger<DatabaseController> _logger;
    private readonly DatabaseService _databaseService;

    public DatabaseController(ILogger<DatabaseController> logger, DatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    [HttpGet()]
    [Route("/database/tables")]
    [Authorize(Roles = "Admin")]
    public IResult GetTables()
    {
        var result = _databaseService.GetTables().Match(
            tables => Results.Ok(tables),
            error => Results.NotFound(error)
        );
        return result;
    }

    [HttpPut("PerformAction")]
    [Route("/database/tables/{action?}/{table?}")]
    [Authorize(Roles = "Admin")]
    public IResult PerformTableAction([FromQuery()] DatabaseAction action, [FromQuery()] string table, [FromBody()] string[] data)
    {
        var result = _databaseService.PerformAction(action, table, data)
        .Match(
            obj => Results.Ok(obj),
            error => Results.NotFound(error)
        );
        return result;
    }
}