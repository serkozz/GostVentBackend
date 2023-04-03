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
    private readonly OrderService _orderService;
    public DatabaseService(SQLiteContext db, UserService userService, OrderService orderService)
    {
        _db = db;
        _userService = userService;
        _orderService = orderService;
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

    public OneOf<object, ErrorInfo> PerformAction(DatabaseAction action, string table, string[] data)
    {
        
        switch (table)
        {
            case "User":
                if (data.Length != typeof(User).GetProperties().Length)
                    return new ErrorInfo(System.Net.HttpStatusCode.BadRequest, $"Невозможно обновить таблицу: данные имеют длину {data.Length} полей а {table} таблица имеет {typeof(User).GetProperties().Length}");
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
            case "Order":
                if (data.Length != typeof(Order).GetProperties().Length)
                    return new ErrorInfo(System.Net.HttpStatusCode.BadRequest, $"Невозможно обновить таблицу: данные имеют длину {data.Length} полей а {table} таблица имеет {typeof(Order).GetProperties().Length}");
                var order = new Order(data);
                switch (action)
                {
                    case DatabaseAction.Post:
                        order = _orderService.Add(order);
                        if (order is null)
                            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Невозможно добавить данные, добавляемая сущность имеет неверный формат данных");
                        return order;

                    case DatabaseAction.Delete:
                        order = _orderService.Delete(order);
                        if (order is null)
                            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Невозможно удалить данные, заказ не существует");
                        return order;

                    case DatabaseAction.Update:
                        order = _orderService.Update(order);
                        if (order is null)
                            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Невозможно обновить данные, заказ не существует");
                        return order;
                }
                break;
        }
        return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Unable to perform database action");
    }
}