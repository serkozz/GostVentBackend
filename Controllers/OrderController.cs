using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services;

namespace Backend.Controllers;

[ApiController]
[Route("{controller}")]
public class OrderController : ControllerBase
{
    // ROUTE EXAMPLE ----- localhost:5072/order

    private readonly ILogger<OrderController> _logger;
    private readonly OrderService _orderService;

    public OrderController(ILogger<OrderController> logger, OrderService orderService)
    {
        _logger = logger;
        _orderService = orderService;
    }

    [HttpGet()]
    [Route("/order")]
    [Authorize(Roles="Admin")]
    public IResult GetOrders()
    {
        return Results.Ok(_orderService.GetAllOrders());
    }

    [HttpGet()]
    [Route("/order/{email?}")]
    [Authorize()]
    public IResult GetOrdersByEmail([FromQuery()] string email)
    {
        var userInfo = User;
        /// FIXME: Пользователь может получить не только свои, но и чужие заказы
        /// напрямую введя чужую почту
        return Results.Ok(_orderService.GetOrdersByEmail(email));
    }

    [HttpPost()]
    [Route("/order/{orderName?}")]
    [Authorize()]
    public IResult CreateOrder([FromForm()] IFormCollection form, [FromQuery()] string orderName)
    {
        /// FIXME: Создавать заказ на основе данных, полученных в JWT токене (email)
        var userInfo = User;
        return _orderService.CreateOrder(form, orderName).Result.Match(
            order => Results.Ok(order),
            error => Results.NotFound(error) 
        );
    }

    [HttpDelete()]
    [Route("/order/{orderName?}")]
    [Authorize()]
    public IResult DeleteOrder([FromQuery()] string orderName)
    {
        /// FIXME: Удалять заказ на основе данных, полученных в JWT токене (email)
        return _orderService.DeleteOrder(orderName).Result.Match(
            order => Results.Ok(order),
            error => Results.NotFound(error) 
        );
    }
}