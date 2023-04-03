using EF.Contexts;
using EF.Models;
using OneOf;
using Types.Interfaces;
using Types.Enums;
using Types.Classes;

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

    public async Task<OneOf<Order, ErrorInfo>> CreateOrder(IFormCollection form, string orderName)
    {
        string[] orderNameSplitted = new string[4];
        orderNameSplitted = orderName.Split('_', 4);
        foreach (var file in form.Files)
        {
            var name = file.FileName;
            var client = _storageServiceCollection.TryGetService(typeof(DropboxStorageService));

            if (client is null)
                return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Cant get storage client to work with. Without it order cannot be created");

            await client.UploadFile($"Orders/{orderNameSplitted[0]}/{orderNameSplitted[1] + '_' + orderNameSplitted[2] + '_' + orderNameSplitted[3]}", name, file.OpenReadStream());
        };

        User? orderClient = _userService.GetUserData(orderNameSplitted[0]).Match(
            user => user,
            error => null
        );

        if (orderClient is null)
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Cant get user to create order");

        Enum.TryParse<ProductType>(orderNameSplitted[2], true, out ProductType productType);

        Order order = new Order()
        {
            Client = orderClient,
            Name = orderNameSplitted[1],
            ProductType = productType,
            CreationDate = DateTime.UtcNow,
            LeadDays = 3,
            Price = 0,
            Status = OrderStatus.Created
        };

        _db.Orders.Add(order);
        _db.SaveChanges();

        return order;
    }

    public List<Order> GetOrdersByEmail(string email)
    {
        List<Order> ordersList = _db.Orders.Where(
            order => order.Client.Email == email
        ).ToList();

        return ordersList;
    }

    public Order? Add(Order obj)
    {
        throw new NotImplementedException();
    }

    public Order? Delete(Order obj)
    {
        throw new NotImplementedException();
    }

    public Order? Update(Order obj)
    {
        throw new NotImplementedException();
    }

    public bool ValidateUnique(Order obj)
    {
        throw new NotImplementedException();
    }
}