using Microsoft.AspNetCore.Mvc;
using EF.Models;
using Microsoft.AspNetCore.Authorization;
using Services;

namespace Backend.Controllers;

[ApiController]
[Route("{controller}")]
public class UserController : ControllerBase
{
    // ROUTE EXAMPLE ----- localhost:5072/user/serezha-kozlov.2002@mail.ru

    private readonly ILogger<UserController> _logger;
    private readonly UserService _userService;

    public UserController(ILogger<UserController> logger, UserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    [HttpGet()]
    [Route("/users")]
    [Authorize(Roles = "Admin")]
    public IResult GetUsers()
    {
        return Results.Ok(_userService.GetAllUsers());
    }

    /// <summary>
    /// Возвращает информацию о пользователе по email если пользователь существует
    /// </summary>
    /// <param name="email">Почта пользователя</param>
    /// <returns>Информация о пользователе с указанным email или информация об ошибке</returns>
    [HttpGet(Name = "GetUserInfoByEmail")]
    [Route("/user/{email?}")]
    [Authorize(Roles = "Admin")]
    public IResult GetUser(string email)
    {
        var result = _userService.GetUserData(email).Match(
            user => Results.Ok(user),
            error => Results.NotFound(error)
        );
        return result;
    }

    /// <summary>
    /// Производит авторизацию пользователя и возвращает его
    /// </summary>
    /// <param name="user">Информация о пользователе</param>
    /// <returns>Информация о авторизованном пользователе или информация об ошибке</returns>
    [HttpPost(Name = "PostUserDataToLogin")]
    [Route("/login")]
    public IResult LoginUser([FromBody()] UserShort userShort)
    {
        var result = _userService.LoginUser(userShort).Match(
            user => Results.Ok(user),
            error => Results.NotFound(error)
        );
        return result;
    }

    /// <summary>
    /// Производит регистрацию пользователя и возвращает его
    /// </summary>
    /// <param name="user">Информация о пользователе</param>
    /// <returns>Информация о зарегистрированном пользователе или информация об ошибке</returns>
    [HttpPost(Name = "PostUserDataToRegister")]
    [Route("/register")]
    public IResult RegisterUser([FromBody()] User user)
    {
        var result = _userService.RegisterUser(user).Match(
            user => Results.Ok(user),
            error => Results.NotFound(error)
        );
        return result;
    }


    [HttpGet(Name = "GetUserRole")]
    [Route("/role/get/{username?}")]
    [Authorize(Roles = "Admin")]
    public IResult GetUserRole([FromQuery()] string username)
    {
        var result = _userService.GetUserRole(username).Match(
            role => Results.Ok(role),
            error => Results.NotFound(error)
        );
        return result;
    }

    [HttpGet(Name = "SetUserRole")]
    [Route("/role/set/{username?}/{role?}")]
    [Authorize(Roles = "Admin")]
    public IResult SetUserRole([FromQuery()] string username, [FromQuery()] string role)
    {
        var result = _userService.SetUserRole(username, role).Match(
            user => Results.Ok(user),
            error => Results.NotFound(error)
        );
        return result;
    }
}
