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

    /// <summary>
    /// Получение пользователя по почте
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public OneOf<User, ErrorInfo> GetUserData(string email)
    {
        User? user = GetUser(email);
        if (user is not null)
            return user;
        return new ErrorInfo(Codes.NotFound, $"Пользователь с почтой: {email} не найден!");
    }

    /// <summary>
    /// Получение пользователя по Id
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public OneOf<User, ErrorInfo> GetUserData(long id)
    {
        User? user = _db.Users.FirstOrDefault(
            user => user.Id == id
        );

        if (user is not null)
            return user;
        return new ErrorInfo(Codes.NotFound, $"Пользователь с id: {id} не найден!");
    }

    /// <summary>
    /// Получение списка всех пользователей
    /// </summary>
    /// <returns></returns>
    public List<User> GetAllUsers() => _db.Users.ToList();

    /// <summary>
    /// Авторизация существующего пользователя
    /// </summary>
    /// <param name="loginUser"></param>
    /// <returns></returns>
    public OneOf<User, ErrorInfo> LoginUser(UserShort loginUser)
    {
        if (!CheckEmailExist(loginUser.Email))
            return new ErrorInfo(Codes.NotFound, $"Пользователь с такой почтой не существует");
        User? user = GetUser(loginUser.Email);
        if (user is null)
            return new ErrorInfo(Codes.NotFound, $"Внутренняя ошибка сервера!!!");
        if (!PasswordUtility.VerifyPassword(loginUser.Password, user.Password))
            return new ErrorInfo(Codes.NotFound, $"Неверный пароль");
        user.Token = CreateJwtToken(user);
        return user;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public OneOf<User, ErrorInfo> RegisterUser(User user)
    {
        try
        {
            if (CheckEmailExist(user.Email))
                return new ErrorInfo(Codes.NotFound, $"Пользователь с такой почтой уже зарегистрирован");
            if (CheckUsernameExist(user.Username))
                return new ErrorInfo(Codes.NotFound, $"Пользователь с таким никнеймом уже зарегистрирован");
            var passwordStrengthResult = Utility.CheckPasswordStrength(user.Password);
            if (!string.IsNullOrEmpty(passwordStrengthResult))
                return new ErrorInfo(Codes.NotFound, $"Cлабый пароль:\n{passwordStrengthResult}");
            user.Password = PasswordUtility.HashPassword(user.Password);
            user.Role = "User";
            var entry = _db.Users.Add(user);
            _db.SaveChanges();
            return entry.Entity;
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

    /// <summary>
    /// Получение роли пользователя
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    public OneOf<string, ErrorInfo> GetUserRole(string username)
    {
        if (!CheckUsernameExist(username))
            return new ErrorInfo(Codes.NotFound, $"Невозможно получить роль для незарегистрированного пользователя");
        User? user = _db.Users.Where(user => user.Username == username).FirstOrDefault();
        if (user is null)
            return new ErrorInfo(Codes.NotFound, $"У данного пользователя не установлены права");
        return user.Role;
    }

    /// <summary>
    /// Установка роли пользователя
    /// </summary>
    /// <param name="username"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    public OneOf<User, ErrorInfo> SetUserRole(string username, string role)
    {
        try
        {
            if (!CheckUsernameExist(username))
                return new ErrorInfo(Codes.NotFound, $"Невозможно установить роль для незарегистрированного пользователя");
            User? user = _db.Users.Where(user => user.Username == username).FirstOrDefault();
            if (user is null)
                return new ErrorInfo(Codes.NotFound, $"Несуществующий пользователь");
            user.Role = role;
            _db.SaveChanges();
            return user;
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

    /// <summary>
    /// Создание JSON Web Token'а, содержащего информацию о пользователе, необходимую для дифференциации прав
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Существует ли такое имя пользователя в БД
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    private bool CheckUsernameExist(string username) => _db.Users.Any(user => user.Username == username);

    /// <summary>
    /// Существует ли такой Email в базе БД
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    private bool CheckEmailExist(string email) => _db.Users.Any(user => user.Email == email);

    /// <summary>
    /// Получение информации о пользователе по почте
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    private User? GetUser(string email) => _db.Users.FirstOrDefault(user => user.Email == email);

    /// <summary>
    /// Добавление пользователя в БД
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public OneOf<User, ErrorInfo> Add(User user, object? additionalArgs = null)
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

    /// <summary>
    /// Обновление пользователя в БД
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public OneOf<User, ErrorInfo> Update(User user, object? additionalArgs = null)
    {
        try
        {
            User? dbUser = _db.Users.FirstOrDefault<User>(dbUser => dbUser.Id == user.Id);
            if (dbUser is null)
                return new ErrorInfo(Codes.NotFound, "Пользователь не найден!");
            dbUser = user;
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

    /// <summary>
    /// Удаление пользователя из БД
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public OneOf<User, ErrorInfo> Delete(User user, object? additionalArgs = null)
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