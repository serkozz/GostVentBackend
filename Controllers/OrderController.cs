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
        /// Вообще стоило бы возвращать не только Ок, но и NotFound, но подразумевается,
        /// что данный метод вызывается только зарегистрированными пользователями,
        /// благодаря атрибуту Authorize, значит нельзя вызвать этот метод для несуществующего email
        /// (вообще можно, напрямую прозвонив конечную точку, но в таком случае должен вернуться ок с кодом ошибки)
        /// Пусть будет так
        return Results.Ok(_orderService.GetOrdersByEmail(email));
    }

    [HttpPost()]
    [Route("/order/{orderName?}")]
    [Authorize()]
    public IResult CreateOrder([FromForm()] IFormCollection form, [FromQuery()] string orderName)
    {
        return _orderService.CreateOrder(form, orderName).Result.Match(
            order => Results.Ok(order),
            error => Results.NotFound(error) 
        );
    }
}