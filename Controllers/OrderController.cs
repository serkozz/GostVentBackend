using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services;
using Types.Classes;
using Codes = System.Net.HttpStatusCode;

namespace Backend.Controllers;

[ApiController]
[Route("{controller}")]
public class OrderController : ControllerBase
{
    // ROUTE EXAMPLE ----- localhost:5072/order

    private readonly ILogger<OrderController> _logger;
    private readonly OrderService _orderService;
    private readonly YooKassaPaymentService _paymentService;

    public OrderController(ILogger<OrderController> logger, OrderService orderService, YooKassaPaymentService paymentService)
    {
        _logger = logger;
        _orderService = orderService;
        _paymentService = paymentService;
    }

    [HttpGet()]
    [Route("/order/files/{email?}/{orderName?}")]
    [Authorize()]
    public IResult GetOrderFiles([FromQuery()] string email, [FromQuery()] string orderName)
    {
        /// FIXME: (FIXED) Получать ссылки на файлы заказа на основе данных, полученных в JWT токене (email)
        var userEmailClaim = User.FindFirst(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

        if (userEmailClaim.Value != email)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, $"Email: {userEmailClaim.Value} авторизованного пользователя не совпадает с Email: {email} запрашиваемого. Нельзя получить информацию не о своих заказах!"));

        return _orderService.GetOrderFilesAsync(email, orderName).Result.Match(
            orderFilesLinks => Results.Ok(orderFilesLinks),
            error => Results.NotFound(error)
        );
    }

    [HttpDelete()]
    [Route("/order/files")]
    [Authorize()]
    public IResult DeleteOrderFile([FromBody()] DropboxFileInfo orderFileInfo)
    {
        return _orderService.DeleteOrderFileAsync(orderFileInfo).Result.Match(
            dropboxFileInfo => Results.Ok(dropboxFileInfo),
            error => Results.NotFound(error)
        );
    }

    [HttpPost()]
    [Route("/order/files/{orderName?}/{email?}")]
    [Authorize()]
    public IResult AddOrderFile([FromForm()] IFormCollection form, [FromQuery()] string email, [FromQuery()] string orderName)
    {
        var userEmailClaim = User.FindFirst(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

        if (userEmailClaim.Value != email)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, $"Email: {userEmailClaim.Value} авторизованного пользователя не совпадает с Email: {email} запрашиваемого. Нельзя добавлять файлы не в свои заказы!"));

        return _orderService.AddOrderFilesAsync(form, orderName, email).Result.Match(
            addedFilesMetadataList => Results.Ok(addedFilesMetadataList),
            error => Results.NotFound(error)
        );
    }

    [HttpGet()]
    [Route("{email?}")]
    [Authorize()]
    public IResult GetOrdersByEmail([FromQuery()] string email)
    {
        /// FIXME: (FIXED) Пользователь может получить не только свои, но и чужие заказы
        /// напрямую введя чужую почту
        var userEmailClaim = User.FindFirst(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

        if (userEmailClaim.Value != email)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, $"Email: {userEmailClaim.Value} авторизованного пользователя не совпадает с Email: {email} запрашиваемого. Нельзя просматривать не свои заказы!"));

        return Results.Ok(_orderService.GetOrdersByEmail(email));
    }

    [HttpGet()]
    [Route("/orders")]
    [Authorize(Roles = "Admin")]
    public IResult GetOrders()
    {
        return Results.Ok(_orderService.GetAllOrders());
    }


    [HttpPost()]
    [Route("{orderName?}")]
    [Authorize()]
    public IResult CreateOrder([FromForm()] IFormCollection form, [FromQuery()] string orderName)
    {
        /// FIXME: Создавать заказ на основе данных, полученных в JWT токене (email)
        /// FIXME: Нельзя создавать два заказа с одинаковыми именами
        var userInfo = User;
        return _orderService.CreateOrder(form, orderName).Result.Match(
            order => Results.Ok(order),
            error => Results.NotFound(error)
        );
    }

    [HttpDelete()]
    [Route("{email?}/{orderName?}")]
    [Authorize()]
    public IResult DeleteOrder([FromQuery()] string email, [FromQuery()] string orderName)
    {
        /// FIXME: (FIXED) Удалять заказ на основе данных, полученных в JWT токене (email)
        var userEmailClaim = User.FindFirst(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

        if (userEmailClaim.Value != email)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, $"Email: {userEmailClaim.Value} авторизованного пользователя не совпадает с Email: {email} запрашиваемого. Нельзя удалять не свои заказы!"));

        return _orderService.DeleteOrder(orderName, email).Result.Match(
            order => Results.Ok(order),
            error => Results.NotFound(error)
        );
    }

}