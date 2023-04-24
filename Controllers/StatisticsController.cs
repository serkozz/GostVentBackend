using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services;
using Codes = System.Net.HttpStatusCode;
using Types.Classes;
using EF.Models;
using EF.Contexts;
using OneOf;

namespace Backend.Controllers;

[ApiController]
[Route("{controller}")]
public class StatisticsController : ControllerBase
{
    // ROUTE EXAMPLE ----- localhost:5072/statistics

    private readonly ILogger<DatabaseController> _logger;
    private readonly StatisticsService _statisticsService;
    private readonly SQLiteContext _db;

    public StatisticsController(ILogger<DatabaseController> logger, StatisticsService statisticsService, SQLiteContext db)
    {
        _db = db;
        _statisticsService = statisticsService;
        _logger = logger;
    }

    [HttpGet()]
    [Route("/statistics/{fromDate?}/{toDate?}")]
    // [Authorize()]
    public IResult GetStatistics([FromQuery()] string fromDate, [FromQuery()] string toDate)
    {
        DateOnly from;
        DateOnly to = new DateOnly();

        var date = to.ToDateTime(new TimeOnly(0));

        if (!DateOnly.TryParse(fromDate, out from))
            from = DateOnly.FromDateTime(DateTime.Now);
        if (!DateOnly.TryParse(toDate, out to))
            to = DateOnly.FromDateTime(DateTime.Now);

        OneOf<List<StatisticsReport>, ErrorInfo> statsFromOrError = _statisticsService.GetStatistics(from);
        OneOf<List<StatisticsReport>, ErrorInfo> statsToOrError = new ErrorInfo(Codes.NotFound, "Начальная и конечная дата интервала равны");

        if (from != to)
            statsToOrError = _statisticsService.GetStatistics(to);

        if (statsFromOrError.IsT0 && statsToOrError.IsT0)
        {
            List<StatisticsReport> listDif = new List<StatisticsReport>();
            int id = 0;
            foreach (var statistics in statsFromOrError.AsT0)
            {
                listDif.Add(statistics.Difference(statsToOrError.AsT0[id]).AsT0);
                id++;
            }

            return Results.Ok(listDif);
        }

        if (statsFromOrError.IsT1)
            return Results.NotFound(statsFromOrError.AsT1);

        return Results.Ok(statsFromOrError.AsT0);
    }

    [HttpGet()]
    [Route("/statistics/calculate/{date?}")]
    // [Authorize()]
    public IResult RecalculateMeans([FromQuery()] string? date)
    {
        List<StatisticsReport> statisticsReports = new List<StatisticsReport>();

        DateOnly repDate;
        if (!DateOnly.TryParse(date, out repDate))
            repDate = DateOnly.FromDateTime(DateTime.Now);

        var res = _statisticsService.MeanOrdersPricePerClient(repDate);
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<StatisticsData>()));

        res = _statisticsService.MeanOrdersPriceByOrderType(repDate);
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<StatisticsData>()));

        res = _statisticsService.MeanOrdersFinishTimeByOrderType(repDate);
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<StatisticsData>()));

        res = _statisticsService.OrdersPercentByOrderType(repDate);
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<StatisticsData>()));

        res = _statisticsService.MeanOrderRatingByType(repDate);
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<StatisticsData>()));

        res = _statisticsService.MaxOrderPrice(repDate);
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<StatisticsData>()));

        var errorsList = new List<ErrorInfo>();

        foreach (var report in statisticsReports)
        {
            var reportOrError = _statisticsService.SaveStats(report);

            if (reportOrError.IsT1)
                errorsList.Add(reportOrError.AsT1);
        }

        if (errorsList.Count != 0)
            return Results.NotFound(errorsList);

        return Results.Ok(statisticsReports);
    }

    [HttpGet()]
    [Route("/statistics/max")]
    [Authorize()]
    public IResult GetMax()
    {
        var res = _statisticsService.MeanOrdersPricePerClient();
        return Results.Ok(res.IsT0 ? res.AsT0 : res.AsT1);
    }

    [HttpGet()]
    [Route("/statistics/min")]
    [Authorize()]
    public IResult GetMin()
    {
        var res = _statisticsService.MeanOrdersPricePerClient();
        return Results.Ok(res.IsT0 ? res.AsT0 : res.AsT1);
    }
}