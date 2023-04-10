using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services;
using Codes = System.Net.HttpStatusCode;
using Types.Classes;

namespace Backend.Controllers;

[ApiController]
[Route("{controller}")]
public class PaymentController : ControllerBase
{
    // ROUTE EXAMPLE ----- localhost:5072/payment

    private readonly ILogger<DatabaseController> _logger;
    private readonly YooKassaPaymentService _paymentService;
    private readonly OrderService _orderService;

    public PaymentController(ILogger<DatabaseController> logger, YooKassaPaymentService paymentService, OrderService orderService)
    {
        _logger = logger;
        _paymentService = paymentService;
        _orderService = orderService;
    }

    [HttpGet()]
    [Route("/payment/status/{orderName?}/{email?}")]
    [Authorize()]
    public async Task<IResult> OrderStatus([FromQuery()] string orderName, [FromQuery()] string email)
    {
        var userEmailClaim = User.FindFirst(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

        if (userEmailClaim.Value != email)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, $"Email: {userEmailClaim.Value} авторизованного пользователя не совпадает с Email: {email} запрашиваемого. Нельзя получить статус не своего заказа!"));

        var paymentOrder = _orderService.GetOrdersByEmail(email).Where(
            order => order.Name == orderName
        ).ToList()[0];

        Console.WriteLine(Request.Method + Request.ContentType);
        var orderPaymentInfo = await _paymentService.GetOrderPaymentInfo(orderName, email);
        var result = orderPaymentInfo.Match(
            paymentInfo => Results.Ok(paymentInfo),
            error => Results.NotFound(error)
        );
        return result;
    }

    [HttpPost()]
    [Route("/payment/pay/{orderName?}/{email?}")]
    [Authorize()]
    public async Task<IResult> PayOrder([FromQuery()] string orderName, [FromQuery()] string email)
    {
        var userEmailClaim = User.FindFirst(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

        if (userEmailClaim.Value != email)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, $"Email: {userEmailClaim.Value} авторизованного пользователя не совпадает с Email: {email} запрашиваемого. Нельзя оплачивать не свои заказы!"));

        var paymentOrder = _orderService.GetOrdersByEmail(email).Where(
            order => order.Name == orderName
        ).ToList()[0];

        var returnUrl = "http://localhost:4200/dashboard";
        var result = await _paymentService.CreatePaymentUsingAPI(paymentOrder, email, returnUrl);

        return result.Match(
            paymentResponse => Results.Ok(paymentResponse),
            error => Results.NotFound(error)
        );
    }
}