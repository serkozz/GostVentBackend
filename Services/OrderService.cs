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

    /// <summary>
    /// Создает заказ в базе данных и в облачном хранилище Dropbox, обеспечивая синхронизацию БД с облачным хранилищем
    /// </summary>
    /// <param name="form"></param>
    /// <param name="orderName"></param>
    /// <returns></returns>
    public async Task<OneOf<Order, ErrorInfo>> CreateOrder(IFormCollection form, string orderName)
    {
        /// TODO: Возможно стоит сначала создавать заказ, а потом уже добавлять файлы на дропбокс
        /// ибо удалить запись из бд проще, чем из облачного хранилища в плане времени
        string[] orderNameSplitted = new string[4];
        orderNameSplitted = orderName.Split('_', 4);
        var client = _storageServiceCollection.TryGetService(typeof(DropboxStorageService)) as DropboxStorageService;

        if (client is null)
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Невозможно получить клиент хранилища. Без него заказ не может быть создан");

        /// Создаем папку заказа на Dropbox и помещаем туда файлы заказа
        foreach (var file in form.Files)
        {
            var name = file.FileName;
            var uploadResult = await client.UploadFileAsync($"{orderNameSplitted[0]}/{orderNameSplitted[1] + '_' + orderNameSplitted[2] + '_' + orderNameSplitted[3]}", name, file.OpenReadStream());

            /// Если поместить файлы невозможно, то возвращаем ошибку, (БД:Записи НЕТ, Dropbox:Записи НЕТ)
            if (uploadResult.IsT1)
                return uploadResult.AsT1;
        };

        User? orderClient = _userService.GetUserData(orderNameSplitted[0]).Match(
            user => user,
            error => null
        );

        /// Получаем клиента заказа по почте, если он не существует, то удаляем созданную папку заказа в Dropbox
        /// (БД:Записи НЕТ, Dropbox:Записи НЕТ)
        if (orderClient is null)
        {
            var deleteResult = await client.DeleteAsync($"{orderNameSplitted[0]}/{orderNameSplitted[1] + '_' + orderNameSplitted[2] + '_' + orderNameSplitted[3]}");
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Невозможно получить клиента, без него создание заказа невозможно");
        }

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

        var addResult = Add(order).Match<OneOf<Order, ErrorInfo>>(
            order => order,
            error => error
        );

        /// Если в результате добавления заказа в БД возникла ошибка, то удаляем папку на Dropbox
        /// (БД:Записи НЕТ, Dropbox:Записи НЕТ)
        if (addResult.IsT1)
        {
            var deleteResult = await client.DeleteAsync($"{orderNameSplitted[0]}/{orderNameSplitted[1] + '_' + orderNameSplitted[2] + '_' + orderNameSplitted[3]}");
            return addResult.AsT1;
        }

        /// (БД:Запись ЕСТЬ, Dropbox:Запись ЕСТЬ)
        return order;
    }

    public async Task<OneOf<Order, ErrorInfo>> DeleteOrder(string orderName)
    {
        Order? order = _db.Orders.FirstOrDefault(
            order => order.Name == orderName
        );

        if (order is null)
            return new ErrorInfo(Codes.NotFound, "Невозможно получить удаляемый заказ по его имени!");

        var removedEntityEntry = _db.Orders.Remove(order);
        User? orderClient = _db.Users.FirstOrDefault(
            user => user.Id == order.ClientId
        );
        var client = _storageServiceCollection.TryGetService(typeof(DropboxStorageService)) as DropboxStorageService;

        if (client is null)
            return new ErrorInfo(Codes.NotFound, "Невозможно получить клиент хранилища без него заказ не может быть удален!");
        
        if (orderClient is null)
            return new ErrorInfo(Codes.NotFound, "Невозможно получить клиента заказа, без него заказ не может быть удален!");

        if (removedEntityEntry.State == EntityState.Deleted)
        {
            string dropboxOrderPath = $"{orderClient.Email}/{order.Name + '_' + order.ProductType.ToString() + '_' + order.CreationDate.ToString()}";
            var deleteResult = await client.DeleteAsync(dropboxOrderPath);
            return removedEntityEntry.Entity;
        }

        return new ErrorInfo(Codes.NotFound, "Заказ не был удален, возможно он был удален раннее");
    }

    /// <summary>
    /// Получение всех заказов всех клиентов
    /// </summary>
    /// <returns></returns>
    public List<Order> GetAllOrders() => _db.Orders.ToList();

    /// <summary>
    /// Получение всех заказов одного клиента по его почте
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public List<Order> GetOrdersByEmail(string email)
    {
        List<Order> ordersList = _db.Orders.Where(
            order => order.Client.Email == email
        ).ToList();

        return ordersList;
    }

    /// <summary>
    /// Добавление заказа в базу данных
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Удаление заказа из базы данных
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Обновление данных в базе данных
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
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