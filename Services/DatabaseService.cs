using EF.Models;
using EF.Contexts;
using OneOf;
using Types.Classes;
using Types.Enums;
using Types.Interfaces;
using Codes = System.Net.HttpStatusCode;

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

        return new ErrorInfo(Codes.NotFound, $"Невозможно получить таблицы");
    }

    public OneOf<IDynamicallySettable, ErrorInfo> PerformAction(DatabaseAction action, string table, string[] data)
    {
        switch (table)
        {
            case "User":
                if (data.Length != typeof(User).GetProperties().Length)
                    return new ErrorInfo(Codes.BadRequest, $"Невозможно обновить таблицу: данные имеют длину {data.Length} полей а {table} таблица имеет {typeof(User).GetProperties().Length}");
                User user = new User(data);

                if (user.Id < 0)
                    return new ErrorInfo(Codes.BadRequest, "Поля объекта содержат неверные данные, проверьте правильность введенных данных");

                switch (action)
                {
                    case DatabaseAction.Post:
                        return _userService.Add(user).Match<OneOf<IDynamicallySettable, ErrorInfo>>(
                            user => user,
                            error => error
                        );

                    case DatabaseAction.Delete:
                        return _userService.Delete(user).Match<OneOf<IDynamicallySettable, ErrorInfo>>(
                            user => user,
                            error => error
                        );

                    case DatabaseAction.Update:
                        return _userService.Update(user).Match<OneOf<IDynamicallySettable, ErrorInfo>>(
                            user => user,
                            error => error
                        );
                }
                break;
            case "Order":
                if (data.Length != typeof(Order).GetProperties().Length)
                    return new ErrorInfo(Codes.BadRequest, $"Невозможно обновить таблицу: данные имеют длину {data.Length} полей а {table} таблица имеет {typeof(Order).GetProperties().Length}");
                Order order = new Order(data);

                if (order.Id < 0)
                    return new ErrorInfo(Codes.BadRequest, "Поля объекта содержат неверные данные, проверьте правильность введенных данных");
                
                switch (action)
                {
                    case DatabaseAction.Post:
                        return _orderService.Add(order).Match<OneOf<IDynamicallySettable, ErrorInfo>>(
                            order => order,
                            error => error
                        );

                    case DatabaseAction.Delete:
                        return _orderService.Delete(order).Match<OneOf<IDynamicallySettable, ErrorInfo>>(
                            order => order,
                            error => error
                        );

                    case DatabaseAction.Update:
                        return _orderService.Update(order).Match<OneOf<IDynamicallySettable, ErrorInfo>>(
                            order => order,
                            error => error
                        );
                }
                break;
        }
        return new ErrorInfo(Codes.NotFound, "Невозможно совершить операцию с базой данных!");
    }
}