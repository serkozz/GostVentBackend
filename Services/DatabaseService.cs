using EF.Models;
using EF.Contexts;
using OneOf;
using Types.Classes;
using Services;
using Types.Enums;

namespace Services;
public class DatabaseService
{
    private readonly SQLiteContext _db;
    private readonly UserService _userService;
    public DatabaseService(SQLiteContext db, UserService userService)
    {
        _db = db;
        _userService = userService;
    }

    public OneOf<string[], ErrorInfo> GetTables()
    {
        var tableNames = _db.Model.GetEntityTypes()
            .Select(t => t.DisplayName())
            .Distinct()
            .ToList();
        if (tableNames.Count != 0)
            return tableNames.ToArray();

        return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"Can't get tables");
    }

    public OneOf<Test[], ErrorInfo> GetTestsTable()
    {
        var tests = _db.Tests.ToArray();
        if (tests.Length != 0)
            return tests;

        return new ErrorInfo(System.Net.HttpStatusCode.NotFound, $"Can't get tests");
    }

    public OneOf<object, ErrorInfo> PerformAction(DatabaseAction action, string table, string[] data)
    {
        
        switch (table)
        {
            case "User":
                if (data.Length != typeof(User).GetProperties().Length)
                    return new ErrorInfo(System.Net.HttpStatusCode.BadRequest, $"Unable to update table: Data has {data.Length} items but {table} table has {typeof(User).GetProperties().Length}");
                var user = new User(data);
                switch (action)
                {
                    case DatabaseAction.Post:
                        user = _userService.Add(user);
                        if (user is null)
                            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Невозможно добавить данные, добавляемая сущность имеет неверный формат данных");
                        return user;

                    case DatabaseAction.Delete:
                        user = _userService.Delete(user);
                        if (user is null)
                            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Невозможно удалить данные, пользователь не существует");
                        return user;

                    case DatabaseAction.Update:
                        user = _userService.Update(user);
                        if (user is null)
                            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Невозможно обновить данные, пользователь не существует");
                        return user;
                }
                break;
            case "Test":
                if (data.Length != typeof(Test).GetProperties().Length)
                    return new ErrorInfo(System.Net.HttpStatusCode.BadRequest, $"Unable to perform database action: Data has {data.Length} items but {table} table has {typeof(Test).GetProperties().Length}");
                return new Test(data);
        }
        return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Unable to perform database action");
    }
}