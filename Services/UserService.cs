using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EF.Contexts;
using EF.Models;
using Types.Classes;
using Microsoft.IdentityModel.Tokens;
using OneOf;
using Types.Interfaces;
using Utility = Types.Classes.Utility;
using Codes = System.Net.HttpStatusCode;
using Microsoft.EntityFrameworkCore;

namespace Services;
public class UserService : IDatabaseModelService<User>
{
    private readonly SQLiteContext _db;
    public UserService(SQLiteContext db)
    {
        _db = db;
    }
    public OneOf<User, ErrorInfo> GetUserData(string email)
    {
        User? user = GetUser(email);
        if (user is not null)
            return user;
        return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"User with email: {email} not found!");
    }

    public List<User> GetAllUsers() => _db.Users.ToList();
    public OneOf<User, ErrorInfo> LoginUser(UserShort loginUser)
    {
        if (!CheckEmailExist(loginUser.Email))
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"Пользователь с такой почтой не существует");
        User? user = GetUser(loginUser.Email);
        if (user is null)
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"Внутренняя ошибка сервера!!!");
        if (!PasswordUtility.VerifyPassword(loginUser.Password, user.Password))
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"Неверный пароль");
        user.Token = CreateJwtToken(user);
        return user;
    }
    public OneOf<User, ErrorInfo> RegisterUser(User user)
    {
        if (CheckEmailExist(user.Email))
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"Пользователь с такой почтой уже зарегистрирован");
        if (CheckUsernameExist(user.Username))
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"Пользователь с таким никнеймом уже зарегистрирован");
        var passwordStrengthResult = Utility.CheckPasswordStrength(user.Password);
        if (!string.IsNullOrEmpty(passwordStrengthResult))
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"Cлабый пароль:\n{passwordStrengthResult}");
        user.Password = PasswordUtility.HashPassword(user.Password);
        user.Role = "User";
        var entry = _db.Users.Add(user);
        _db.SaveChanges();
        return entry.Entity;
    }

    public OneOf<string, ErrorInfo> GetUserRole(string username)
    {
        if (!CheckUsernameExist(username))
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"Невозможно получить роль для незарегистрированного пользователя");
        User? user = _db.Users.Where(user => user.Username == username).FirstOrDefault();
        if (user is null)
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"У данного пользователя не установлены права");
        return user.Role;
    }

    public OneOf<User, ErrorInfo> SetUserRole(string username, string role)
    {
        if (!CheckUsernameExist(username))
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"Невозможно установить роль для незарегистрированного пользователя");
        User? user = _db.Users.Where(user => user.Username == username).FirstOrDefault();
        if (user is null)
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"Несуществующий пользователь");
        user.Role = role;
        _db.SaveChanges();
        return user;
    }
    private string CreateJwtToken(User user)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("SecretKeySixteen");
        var identity = new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.Username}")
        });
        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = identity,
            Expires = DateTime.Now.AddDays(1),
            SigningCredentials = credentials
        };
        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        return jwtTokenHandler.WriteToken(token);
    }

    private bool CheckUsernameExist(string username) => _db.Users.Any(user => user.Username == username);
    private bool CheckEmailExist(string email) => _db.Users.Any(user => user.Email == email);
    private User? GetUser(string email) => _db.Users.FirstOrDefault(user => user.Email == email);

    public OneOf<User, ErrorInfo> Add(User user)
    {
        /// Добавление нового пользователя по сути это регистрация, валидность введенных данных проверяется в методе регистрации
        try
        {
            var registerResult = RegisterUser(user).Match(
            user => user,
            error => null
            );

            if (registerResult is null)
                throw new Exception("Возникло исключение при добавлении пользователя! Данные не подходят под сигнатуру");
            return registerResult;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return new ErrorInfo(Codes.NotFound, $"DbUpdateConcurrencyException: {ex.InnerException?.Message}");
        }
        catch (DbUpdateException ex)
        {
            return new ErrorInfo(Codes.NotFound, $"DbUpdateException: {ex.InnerException?.Message}");
        }
        catch (Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"System.Exception: {ex.Message}");
        }
    }

    public OneOf<User, ErrorInfo> Update(User user)
    {
        try
        {
            User? dbUser = _db.Users.FirstOrDefault<User>(dbUser => dbUser.Id == user.Id);
            if (dbUser is null)
                return new ErrorInfo(Codes.NotFound, "Пользователь не найден!");
            dbUser.UpdateSelfDynamically(user);
            _db.SaveChanges();
            return dbUser;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return new ErrorInfo(Codes.NotFound, $"DbUpdateConcurrencyException: {ex.InnerException?.Message}");
        }
        catch (DbUpdateException ex)
        {
            return new ErrorInfo(Codes.NotFound, $"DbUpdateException: {ex.InnerException?.Message}");
        }
        catch (Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"System.Exception: {ex.Message}");
        }
    }

    public OneOf<User, ErrorInfo> Delete(User user)
    {
        try
        {
            /// Валидность введенных данных проверять не нужно, так как удаление производится по айди
            User? dbUser = _db.Users.FirstOrDefault<User>(dbUser => dbUser.Id == user.Id);
            if (dbUser is null)
                return new ErrorInfo(Codes.NotFound, "Пользователь не найден!");
            _db.Users.Remove(dbUser);
            _db.SaveChanges();
            return dbUser;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return new ErrorInfo(Codes.NotFound, $"DbUpdateConcurrencyException: {ex.InnerException?.Message}");
        }
        catch (DbUpdateException ex)
        {
            return new ErrorInfo(Codes.NotFound, $"DbUpdateException: {ex.InnerException?.Message}");
        }
        catch (Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"System.Exception: {ex.Message}");
        }
    }
}