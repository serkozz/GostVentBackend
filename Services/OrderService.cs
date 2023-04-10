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
            ClientId = orderClient.Id,
            Name = orderNameSplitted[1],
            ProductType = productType,
            CreationDate = DateOnly.FromDateTime(DateTime.Now),
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
            /// Случай, когда ошибка бросается не из-за рассинхрона БД и хранилища, а когда в бд найден дубликат
            /// добавляемого заказа, дубликат удалять не надо, надо просто не добавлять новый! (выходим до удаления файла с хранилища)
            // if (addResult.AsT1.StatusCode == Codes.BadRequest)
            //     return addResult;

            var deleteResult = await client.DeleteAsync($"{orderNameSplitted[0]}/{orderNameSplitted[1] + '_' + orderNameSplitted[2] + '_' + orderNameSplitted[3]}");
            return addResult.AsT1;
        }

        /// (БД:Запись ЕСТЬ, Dropbox:Запись ЕСТЬ)
        return addResult.AsT0;
    }

    public async Task<OneOf<Order, ErrorInfo>> DeleteOrder(string orderName, string email)
    {
        try
        {
        User? orderClient = _db.Users.FirstOrDefault(
            user => user.Email == email
        );

        if (orderClient is null)
            return new ErrorInfo(Codes.NotFound, "Невозможно получить клиента заказа, без него заказ не может быть удален!");

        Order? order = _db.Orders.FirstOrDefault(
            order => order.Name == orderName && order.ClientId == orderClient.Id
        );

        if (order is null)
            return new ErrorInfo(Codes.NotFound, "Невозможно получить удаляемый заказ по его имени!");

        var removedEntityEntry = _db.Orders.Remove(order);
        var client = _storageServiceCollection.TryGetService(typeof(DropboxStorageService)) as DropboxStorageService;

        if (client is null)
            return new ErrorInfo(Codes.NotFound, "Невозможно получить клиент хранилища без него заказ не может быть удален!");

        if (removedEntityEntry.State == EntityState.Deleted)
        {
            string dropboxOrderPath = $"{orderClient.Email}/{order.Name + '_' + order.ProductType.ToString() + '_' + order.CreationDate.ToString()}";
            var deleteResult = await client.DeleteAsync(dropboxOrderPath);
            if (deleteResult.IsT0)
            {
                _db.SaveChanges();
                return removedEntityEntry.Entity;
            }
            return deleteResult.AsT1;
        }

        return new ErrorInfo(Codes.NotFound, "Заказ не был удален, возможно он был удален раннее");
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

    public async Task<OneOf<List<DropboxFileInfo>, ErrorInfo>> GetOrderFilesAsync(string email, string orderName)
    {
        List<DropboxFileInfo> ordersFilesInfo = new List<DropboxFileInfo>();
        var client = _storageServiceCollection.TryGetService(typeof(DropboxStorageService)) as DropboxStorageService;

        if (client is null)
            return new ErrorInfo(Codes.NotFound, "Невозможно получить клиент хранилища без него файлы заказа не могут быть получены!");

        var ordersFilesListOrError = await client.GetFilesAsync(email, orderName);

        if (ordersFilesListOrError.IsT1)
            return ordersFilesListOrError.AsT1;

        List<Dropbox.Api.Files.Metadata> filesMetadata = ordersFilesListOrError.AsT0;

        foreach (Dropbox.Api.Files.Metadata metadata in filesMetadata)
        {
            var linkResultOrError = await client.GetDownloadLinkAsync(metadata.PathLower);
            if (linkResultOrError.IsT1)
                return linkResultOrError.AsT1;
            
            var linkResult = linkResultOrError.AsT0;
            
            DropboxFileInfo dfi = new DropboxFileInfo()
            {
                Name = linkResult.Metadata.Name,
                FileSize = linkResult.Metadata.Size,
                Link = linkResult.Link,
                DropboxPath = linkResult.Metadata.PathDisplay
            };

            ordersFilesInfo.Add(dfi);
        }

        return ordersFilesInfo;
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
    public OneOf<Order, ErrorInfo> Add(Order order, object? additionalArgs = null)
    {
        try
        {
            var exists = _db.Orders.Any(existingOrder => existingOrder.Name == order.Name && existingOrder.ClientId == order.ClientId);

            if (exists)
                return new ErrorInfo(Codes.BadRequest, "Невозможно добавить заказ в БД, у вас уже существует заказ с таким именем, оно должно быть уникальным!!");

            var entry = _db.Orders.Add(order);

            if (additionalArgs is not null)
            {
                OneOf<User, ErrorInfo> userOrError = _userService.GetUserData(entry.Entity.ClientId);

                if (userOrError.IsT1)
                    return userOrError.AsT1;

                User user = userOrError.AsT0;

                var client = _storageServiceCollection.TryGetService(typeof(DropboxStorageService)) as DropboxStorageService;

                if (client is null)
                    return new ErrorInfo(Codes.NotFound, "Невозможно получить клиент хранилища без него заказ не может быть создан!");

                string dropboxOrderPath = $"{user.Email}/{order.Name + '_' + order.ProductType.ToString() + '_' + order.CreationDate.ToString()}";

                var dropboxFolderExists = client.ExistsAsync(dropboxOrderPath).Result;

                if (dropboxFolderExists.IsT1)
                    return dropboxFolderExists.AsT1;

                if (dropboxFolderExists.AsT0 == false)
                {
                    var creationResult = client.CreateFolderAsync(dropboxOrderPath).Result;
                    if (creationResult.IsT1)
                        return creationResult.AsT1;
                }
            }

            if (entry.State == EntityState.Added)
            {
                _db.SaveChanges();
                return entry.Entity;
            }

            return new ErrorInfo(Codes.NotFound, "Невозможно добавить заказ в БД");

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
    public OneOf<Order, ErrorInfo> Delete(Order order, object? additionalArgs = null)
    {
        try
        {
            /// Валидность введенных данных проверять не нужно, так как удаление производится по айди
            Order? dbOrder = _db.Orders.FirstOrDefault<Order>(dbOrder => dbOrder.Id == order.Id);
            if (dbOrder is null)
                return new ErrorInfo(Codes.NotFound, "Заказ не найден!");

            var entry = _db.Orders.Remove(dbOrder);

            if (additionalArgs is not null)
            {
                OneOf<User, ErrorInfo> userOrError = _userService.GetUserData(entry.Entity.ClientId);

                if (userOrError.IsT1)
                    return userOrError.AsT1;

                User user = userOrError.AsT0;

                var client = _storageServiceCollection.TryGetService(typeof(DropboxStorageService)) as DropboxStorageService;

                if (client is null)
                    return new ErrorInfo(Codes.NotFound, "Невозможно получить клиент хранилища без него заказ не может быть создан!");

                string dropboxOrderPath = $"{user.Email}/{order.Name + '_' + order.ProductType.ToString() + '_' + order.CreationDate.ToString()}";

                var dropboxFolderExists = client.ExistsAsync(dropboxOrderPath).Result;

                if (dropboxFolderExists.IsT1)
                    return dropboxFolderExists.AsT1;

                if (dropboxFolderExists.AsT0 == true)
                {
                    var deleteResult = client.DeleteAsync(dropboxOrderPath).Result;
                    if (deleteResult.IsT1)
                        return deleteResult.AsT1;
                }
            }
            if (entry.State == EntityState.Deleted)
            {
                _db.SaveChanges();
                return entry.Entity;
            }

            return new ErrorInfo(Codes.NotFound, "Невозможно удалить заказ из БД");
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
    public OneOf<Order, ErrorInfo> Update(Order order, object? additionalArgs = null)
    {
        try
        {
            /// Валидацию переданного пользователя не надо производить, так как EF не позволит его сохранить
            Order? dbOrder = _db.Orders.FirstOrDefault<Order>(dbOrder => dbOrder.Id == order.Id);
            if (dbOrder is null)
                return new ErrorInfo(Codes.NotFound, "Заказ не найден!");
            OneOf<User, ErrorInfo> oldUserOrError = _userService.GetUserData(dbOrder.ClientId);

            if (oldUserOrError.IsT1)
                return oldUserOrError.AsT1;

            User oldUser = oldUserOrError.AsT0;
            Order oldOrder = (Order)dbOrder.Clone();

            _db.Entry(dbOrder).State = EntityState.Detached;
            dbOrder = order;
            _db.Entry(dbOrder).State = EntityState.Modified;

            var entry = _db.Entry(dbOrder);
            if (additionalArgs is not null)
            {
                OneOf<User, ErrorInfo> newUserOrError = _userService.GetUserData(entry.Entity.ClientId);

                if (newUserOrError.IsT1)
                    return newUserOrError.AsT1;

                User newUser = newUserOrError.AsT0;
                Order newOrder = entry.Entity;

                var client = _storageServiceCollection.TryGetService(typeof(DropboxStorageService)) as DropboxStorageService;

                if (client is null)
                    return new ErrorInfo(Codes.NotFound, "Невозможно получить клиент хранилища без него заказ не может быть создан!");

                string dropboxOldOrderPath = $"{oldUser.Email}/{oldOrder.Name + '_' + oldOrder.ProductType.ToString() + '_' + oldOrder.CreationDate.ToString()}";
                string dropboxNewOrderPath = $"{newUser.Email}/{newOrder.Name + '_' + newOrder.ProductType.ToString() + '_' + newOrder.CreationDate.ToString()}";

                /// Если пути совпадают, то изменены данные, которые не участвуют в формировании имени файла на Dropbox
                /// значит не надо переносить файлы на Dropbox
                if (dropboxOldOrderPath != dropboxNewOrderPath)
                {
                    var dropboxFolderExists = client.ExistsAsync(dropboxOldOrderPath).Result;

                    if (dropboxFolderExists.IsT1)
                        return dropboxFolderExists.AsT1;

                    if (dropboxFolderExists.AsT0 == true)
                    {
                        var moveResult = client.MoveAsync(dropboxOldOrderPath, dropboxNewOrderPath).Result;
                        if (moveResult.IsT1)
                            return moveResult.AsT1;
                    }
                }

            }
            /// FIXME: (FIXED) Он должен фиксировать изменение как модификацию а не как удаление или отсоединение
            /// Если здесь Deleted, то запись удаляется
            /// Если тут Detached, то она не меняется в базе, но меняется ее имя на дропбоксе
            if (entry.State == EntityState.Modified)
            {
                _db.SaveChanges();
                return entry.Entity;
            }

            return new ErrorInfo(Codes.NotFound, "Невозможно обновить заказ в БД");
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

    public async Task<OneOf<DropboxFileInfo, ErrorInfo>> DeleteOrderFileAsync(DropboxFileInfo orderFileInfo)
    {
        var client = _storageServiceCollection.TryGetService(typeof(DropboxStorageService)) as DropboxStorageService;

        if (client is null)
            return new ErrorInfo(Codes.NotFound, "Невозможно получить клиент хранилища без него файлы заказа не могут быть удалены!");

        var deleteResultOrError = await client.DeleteAsync(orderFileInfo);

        if (deleteResultOrError.IsT1)
            return deleteResultOrError.AsT1;

        return orderFileInfo;
    }

    public async Task<OneOf<List<Dropbox.Api.Files.Metadata>, ErrorInfo>> AddOrderFilesAsync(IFormCollection files, string orderName, string email)
    {
        List<Dropbox.Api.Files.Metadata> addedFilesMetadata = new List<Dropbox.Api.Files.Metadata>();
        var client = _storageServiceCollection.TryGetService(typeof(DropboxStorageService)) as DropboxStorageService;

        if (client is null)
            return new ErrorInfo(Codes.NotFound, "Невозможно получить клиент хранилища без него файлы заказа не могут быть удалены!");

        foreach (var file in files.Files)
        {
            var name = file.FileName;
            var uploadResult = await client.UploadFileAsync($"{email}/{orderName}", name, file.OpenReadStream());

            if (uploadResult.IsT1)
                return uploadResult.AsT1;

            addedFilesMetadata.Add((Dropbox.Api.Files.Metadata)uploadResult.AsT0);
        };

        return addedFilesMetadata;
    }
}