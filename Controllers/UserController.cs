using Microsoft.AspNetCore.Mvc;
using EF.Models;
using Microsoft.AspNetCore.Authorization;
using Services;
using Types.Classes;
using Codes = System.Net.HttpStatusCode;
using OneOf;
using Types.Enums;

namespace Backend.Controllers;

[ApiController]
[Route("{controller}")]
public class UserController : ControllerBase
{
    // ROUTE EXAMPLE ----- localhost:5072/user/serezha-kozlov.2002@mail.ru

    private readonly ILogger<UserController> _logger;
    private readonly UserService _userService;
    private readonly MailService _mailService;

    public UserController(ILogger<UserController> logger, UserService userService, MailService mailService)
    {
        _logger = logger;
        _mailService = mailService;
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
    [HttpGet()]
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
    [HttpPost()]
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
    [HttpPost()]
    [Route("/register")]
    public IResult RegisterUser([FromBody()] User user)
    {
        var result = _userService.RegisterUser(user).Match(
            user => Results.Ok(user),
            error => Results.NotFound(error)
        );
        return result;
    }

    /// <summary>
    /// Производит смену пароля пользователя
    /// </summary>
    [HttpPost()]
    [Route("/user/password/change")]
    [Authorize()]
    public IResult ChangePassword([FromBody()] PasswordChangeInfo pci)
    {
        var userEmailClaim = User.FindFirst(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

        if (userEmailClaim.Value != pci.Email)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, $"Email: {userEmailClaim.Value} авторизованного пользователя не совпадает с Email: {pci.Email} запрашиваемого"));

        var result = _userService.ChangePassword(pci.Email, pci.OldPassword, pci.NewPassword, pci.NewPasswordRepeated, isRestoring: false).Match(
            password => Results.Ok(password),
            error => Results.NotFound(error)
        );
        return result;
    }

    [HttpGet()]
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

    [HttpGet()]
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

    [HttpDelete()]
    [Route("/user/delete/{email?}")]
    [Authorize()]
    public async Task<IResult> AccountDeletionRequest([FromQuery()] string email)
    {
        var userEmailClaim = User.FindFirst(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

        if (userEmailClaim.Value != email)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, $"Email: {userEmailClaim.Value} авторизованного пользователя не совпадает с Email: {email} запрашиваемого"));

        User? requestedUser = _userService.GetUserData(email).Match(
            user => user,
            error => null
        );

        string fullUrl = Microsoft.AspNetCore.Http.Extensions.UriHelper.GetEncodedUrl(Request);

        if (requestedUser is null)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, "Запрашиваемый пользователь не найден"));

        var deletionTokenOrError = _userService.CreateToken(requestedUser, TokenType.DeleteAccount);

        if (deletionTokenOrError.IsT1)
            return Results.NotFound(deletionTokenOrError.AsT1);

        OneOf<Uri, ErrorInfo> uriOrError = await _mailService.SendLink(requestedUser.Email, fullUrl, deletionTokenOrError.AsT0);

        if (uriOrError.IsT1)
            return Results.NotFound(uriOrError.AsT1);

        return Results.Ok(uriOrError.AsT0);
    }

    [HttpGet()]
    [Route("/user/delete/confirm/{email?}/{data?}")]
    public IResult ConfirmAccountDeletion([FromQuery()] string email, [FromQuery()] string data)
    {
        User? requestedUser = _userService.GetUserData(email).Match(
            user => user,
            error => null
        );

        if (requestedUser is null)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, "Запрашиваемый пользователь не найден"));

        var confirmationOrError = _userService.ConfirmToken(requestedUser, data, TokenType.DeleteAccount);

        if (confirmationOrError.IsT1)
            return Results.NotFound(confirmationOrError.AsT1);

        if (confirmationOrError.AsT0 is true)
            _userService.Delete(requestedUser).Match(
                user => Results.Ok(user),
                error => Results.NotFound(error)
            );
        
        return Results.NotFound(new ErrorInfo(Codes.NotFound, "Возникла неизвестная ошибка при удалении аккаунта пользователя"));

        /// FIXME: Выйти из аккаунта на фронте
    }

    /// <summary>
    /// Производит смену пароля пользователя
    /// </summary>
    [HttpPost()]
    [Route("/user/password/restore/{email?}")]
    public async Task<IResult> RestorePasswordRequest([FromQuery()] string email)
    {
        User? requestedUser = _userService.GetUserData(email).Match(
            user => user,
            error => null
        );

        if (requestedUser is null)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, "Запрашиваемый пользователь не найден"));

        var restoreTokenOrError = _userService.CreateToken(requestedUser, TokenType.RestorePassword);

        if (restoreTokenOrError.IsT1)
            return Results.NotFound(restoreTokenOrError.AsT1);

        OneOf<string, ErrorInfo> contentOrError = await _mailService.SendMessageAsync(requestedUser.Email, "Восстановление аккаунта ГостВент", restoreTokenOrError.AsT0.Value);

        if (contentOrError.IsT1)
            return Results.NotFound(contentOrError.AsT1);

        return Results.Ok("Токен отправлен");
    }

    [HttpPost()]
    [Route("/user/password/restore/confirm/{confirmationCode?}")]
    public IResult ConfirmRestorePassword([FromQuery()] string confirmationCode, [FromBody()] PasswordChangeInfo pci)
    {
        User? requestedUser = _userService.GetUserData(pci.Email).Match(
            user => user,
            error => null
        );

        if (requestedUser is null)
            return Results.NotFound(new ErrorInfo(Codes.NotFound, "Запрашиваемый пользователь не найден"));

        var validatedOrError = _userService.ConfirmToken(requestedUser, confirmationCode, TokenType.RestorePassword);

        if (validatedOrError.IsT1)
            return Results.NotFound(validatedOrError.AsT1);

        var result = _userService.ChangePassword(pci.Email, pci.OldPassword, pci.NewPassword, pci.NewPasswordRepeated, isRestoring: true).Match(
            password => Results.Ok(password),
            error => Results.NotFound(error)
        );
        return result;
    }
}
