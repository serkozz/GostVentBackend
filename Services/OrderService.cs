using EF.Contexts;
using EF.Models;
using OneOf;
using Types.Interfaces;
using Types.Enums;
using Types.Classes;
using Codes = System.Net.HttpStatusCode;
using Microsoft.EntityFrameworkCore;

namespace Services;
public class OrderService : IDatabaseModelService<Order>
{
    private readonly SQLiteContext _db;
    // public Exception? DatabaseException { get; private set; } = null;
    private readonly UserService _userService;
    private readonly StorageServiceCollection _storageServiceCollection;
    public OrderService(SQLiteContext db, UserService userService, StorageServiceCollection storageServiceCollection)
    {
        _db = db;

        _userService = userService;
        _storageServiceCollection = storageServiceCollection;
    }

    public async Task<OneOf<Order, ErrorInfo>> CreateOrder(IFormCollection form, string orderName)
    {
        string[] orderNameSplitted = new string[4];
        orderNameSplitted = orderName.Split('_', 4);
        foreach (var file in form.Files)
        {
            var name = file.FileName;
            var client = _storageServiceCollection.TryGetService(typeof(DropboxStorageService));

            if (client is null)
                return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Невозможно получить клиент хранилища. Без него заказ не может быть создан");

            await client.UploadFile($"Orders/{orderNameSplitted[0]}/{orderNameSplitted[1] + '_' + orderNameSplitted[2] + '_' + orderNameSplitted[3]}", name, file.OpenReadStream());
        };

        User? orderClient = _userService.GetUserData(orderNameSplitted[0]).Match(
            user => user,
            error => null
        );

        if (orderClient is null)
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Невозможно получить клиента, без него создание заказа невозможно");

        Enum.TryParse<ProductType>(orderNameSplitted[2], true, out ProductType productType);

        Order order = new Order()
        {
            Client = orderClient,
            Name = orderNameSplitted[1],
            ProductType = productType,
            CreationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            LeadDays = 3,
            Price = 0,
            Status = OrderStatus.Created
        };

        _db.Orders.Add(order);
        _db.SaveChanges();

        return order;
    }

    public List<Order> GetAllOrders() => _db.Orders.ToList();

    public List<Order> GetOrdersByEmail(string email)
    {
        List<Order> ordersList = _db.Orders.Where(
            order => order.Client.Email == email
        ).ToList();

        return ordersList;
    }

    public OneOf<Order, ErrorInfo> Add(Order order)
    {
        try
        {
            var entry = _db.Orders.Add(order);
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
    }

    public OneOf<Order, ErrorInfo> Delete(Order order)
    {
        try
        {
            /// Валидность введенных данных проверять не нужно, так как удаление производится по айди
            Order? dbOrder = _db.Orders.FirstOrDefault<Order>(dbOrder => dbOrder.Id == order.Id);
            if (dbOrder is null)
                return new ErrorInfo(Codes.NotFound, "Заказ не найден!");
            var entry = _db.Orders.Remove(dbOrder);
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
    }

    public OneOf<Order, ErrorInfo> Update(Order order)
    {
        try
        {
            /// Валидацию переданного пользователя не надо производить, так как EF не позволит его сохранить
            Order? dbOrder = _db.Orders.FirstOrDefault<Order>(dbOrder => dbOrder.Id == order.Id);
            if (dbOrder is null)
                return new ErrorInfo(Codes.NotFound, "Заказ не найден!");
            dbOrder.UpdateSelfDynamically(order);
            _db.SaveChanges();
            return dbOrder;
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