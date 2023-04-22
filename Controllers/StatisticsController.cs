using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services;
using Codes = System.Net.HttpStatusCode;
using Types.Classes;

namespace Backend.Controllers;

[ApiController]
[Route("{controller}")]
public class StatisticsController : ControllerBase
{
    // ROUTE EXAMPLE ----- localhost:5072/payment

    private readonly ILogger<DatabaseController> _logger;
    private readonly StatisticsService _statisticsService;

    public StatisticsController(ILogger<DatabaseController> logger, StatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    [HttpGet()]
    [Route("/statistics")]
    [Authorize()]
    public IResult GetStats()
    {
        // var userEmailClaim = User.FindFirst(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

        // if (userEmailClaim.Value != email)
        //     return Results.NotFound(new ErrorInfo(Codes.NotFound, $"Email: {userEmailClaim.Value} авторизованного пользователя не совпадает с Email: {email} запрашиваемого. Нельзя получить статус не своего заказа!"));
        return Results.Ok();
    }

    [HttpGet()]
    [Route("/statistics/means")]
    // [Authorize()]
    public IResult GetMeans()
    {
        List<StatisticsReport> statisticsReports = new List<StatisticsReport>();

        var res = _statisticsService.MeanOrdersPricePerClient();
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<float>()));

        res =  _statisticsService.MeanOrdersPriceByOrderType();
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<float>()));

        res =  _statisticsService.MeanOrdersFinishTimeByOrderType();
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<float>()));
        
        res =  _statisticsService.OrdersPercentByOrderType();
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<float>()));

        res =  _statisticsService.MeanOrderRatingByType();
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<float>()));

        res =  _statisticsService.MaxOrderPrice();
        statisticsReports.Add(res.IsT0 ? res.AsT0 : new StatisticsReport("ОШИБКА", new List<float>()));

        return Results.Ok(statisticsReports);
    }

    [HttpGet()]
    [Route("/statistics/max")]
    [Authorize()]
    public IResult GetMax()
    {
        var res =  _statisticsService.MeanOrdersPricePerClient();
        return Results.Ok(res.IsT0 ? res.AsT0 : res.AsT1);
    }

    [HttpGet()]
    [Route("/statistics/min")]
    [Authorize()]
    public IResult GetMin()
    {
        var res =  _statisticsService.MeanOrdersPricePerClient();
        return Results.Ok(res.IsT0 ? res.AsT0 : res.AsT1);
    }
}