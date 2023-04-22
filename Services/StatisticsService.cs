using EF.Contexts;
using OneOf;
using Types.Classes;
using Types.Enums;
using Codes = System.Net.HttpStatusCode;

namespace Services;
public class StatisticsService
{
    private readonly SQLiteContext _db;
    private readonly UserService _userService;
    private readonly OrderService _orderService;
    private readonly YooKassaPaymentService _paymentService;

    public StatisticsService(SQLiteContext db, UserService userService, OrderService orderService, YooKassaPaymentService paymentService)
    {
        _db = db;
        _userService = userService;
        _orderService = orderService;
        _paymentService = paymentService;
    }

    public OneOf<StatisticsReport, ErrorInfo> MeanOrdersPricePerClient()
    {
        try
        {
            float priceCumSum = 0;
            int ordersCount = 0;
            List<float> data = new List<float>();
            foreach (var order in _db.Orders.Where(order => order.Status == OrderStatus.Finished && order.Price != 0))
            {
                priceCumSum += order.Price;
                ordersCount++;
            }
            float meanPrice = priceCumSum / ordersCount;
            data.Add(meanPrice);
            return new StatisticsReport("Средняя сумма заказа", data);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<StatisticsReport, ErrorInfo> MeanOrdersPriceByOrderType()
    {
        try
        {
            Dictionary<ProductType, Tuple<float, int>> productType_ordersCumPrice_ordersCount__Dictionary = new Dictionary<ProductType, Tuple<float, int>>();
            var productTypes = Enum.GetValues(typeof(ProductType));
            List<float> data = new List<float>();

            foreach (var value in productTypes)
            {
                productType_ordersCumPrice_ordersCount__Dictionary.Add((ProductType)value, new Tuple<float, int>(0f, 0));
            }

            foreach (var order in _db.Orders.Where(order => order.Status == OrderStatus.Finished && order.Price != 0))
            {
                float cumPrice = productType_ordersCumPrice_ordersCount__Dictionary[order.ProductType].Item1;
                int orderCount = productType_ordersCumPrice_ordersCount__Dictionary[order.ProductType].Item2;

                cumPrice += order.Price;
                orderCount++;

                productType_ordersCumPrice_ordersCount__Dictionary[order.ProductType] = new Tuple<float, int>(cumPrice, orderCount);
            }

            foreach (var ordersCumPrice_ordersCount in productType_ordersCumPrice_ordersCount__Dictionary.Values)
            {
                if (ordersCumPrice_ordersCount.Item2 == 0)
                    continue;

                float meanPrice = ordersCumPrice_ordersCount.Item1 / ordersCumPrice_ordersCount.Item2;
                data.Add(meanPrice);
            }

            return new StatisticsReport("Средняя сумма заказа по категориям (Spiro,Воздуховод,Глушитель,Дефлектор)", data);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<StatisticsReport, ErrorInfo> MeanOrdersFinishTimeByOrderType()
    {
        try
        {
            Dictionary<ProductType, Tuple<float, int>> productType_LeadDays_ordersCount__Dictionary = new Dictionary<ProductType, Tuple<float, int>>();
            var productTypes = Enum.GetValues(typeof(ProductType));
            List<float> data = new List<float>();

            foreach (var value in productTypes)
            {
                productType_LeadDays_ordersCount__Dictionary.Add((ProductType)value, new Tuple<float, int>(0f, 0));
            }

            foreach (var order in _db.Orders.Where(order => order.Status == OrderStatus.Finished && order.Price != 0))
            {
                float leadDays = productType_LeadDays_ordersCount__Dictionary[order.ProductType].Item1;
                int orderCount = productType_LeadDays_ordersCount__Dictionary[order.ProductType].Item2;

                leadDays += order.LeadDays;
                orderCount++;

                productType_LeadDays_ordersCount__Dictionary[order.ProductType] = new Tuple<float, int>(leadDays, orderCount);
            }

            foreach (var LeadDays_ordersCount in productType_LeadDays_ordersCount__Dictionary.Values)
            {
                if (LeadDays_ordersCount.Item2 == 0)
                    continue;

                float meanDays = LeadDays_ordersCount.Item1 / LeadDays_ordersCount.Item2;
                data.Add(meanDays);
            }

            return new StatisticsReport("Среднее время выполнения заказа по категориям (Spiro,Воздуховод,Глушитель,Дефлектор)", data);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<StatisticsReport, ErrorInfo> OrdersPercentByOrderType()
    {
        try
        {
            Dictionary<ProductType, int> productType_ordersCount__Dictionary = new Dictionary<ProductType, int>();
            var productTypes = Enum.GetValues(typeof(ProductType));
            List<float> data = new List<float>();

            foreach (var value in productTypes)
            {
                productType_ordersCount__Dictionary.Add((ProductType)value, 0);
            }

            int ordersCount = 0;

            foreach (var order in _db.Orders.Where(order => order.Status == OrderStatus.Finished && order.Price != 0))
            {
                productType_ordersCount__Dictionary[order.ProductType]++;
                ordersCount++;
            }

            foreach (var count in productType_ordersCount__Dictionary.Values)
            {
                float percent = (float)count / (float)ordersCount;
                data.Add(percent);
            }

            return new StatisticsReport("Процент заказов среди всех по категориям (Spiro,Воздуховод,Глушитель,Дефлектор)", data);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<StatisticsReport, ErrorInfo> MeanOrderRatingByType()
    {
        try
        {
            Dictionary<ProductType, Tuple<float, int>> productType_cumRating_ordersCount__Dictionary = new Dictionary<ProductType, Tuple<float, int>>();
            var productTypes = Enum.GetValues(typeof(ProductType));
            List<float> data = new List<float>();

            foreach (var value in productTypes)
            {
                productType_cumRating_ordersCount__Dictionary.Add((ProductType)value, new Tuple<float, int>(0f, 0));
            }

            foreach (var orderRating in _db.OrderRating)
            {
                _db.Entry(orderRating).Reference(orderRating => orderRating.Order).Load();

                float cumRating = productType_cumRating_ordersCount__Dictionary[orderRating.Order.ProductType].Item1;
                int orderCount = productType_cumRating_ordersCount__Dictionary[orderRating.Order.ProductType].Item2;

                cumRating += orderRating.Rating;
                orderCount++;

                productType_cumRating_ordersCount__Dictionary[orderRating.Order.ProductType] = new Tuple<float, int>(cumRating, orderCount);
            }

            foreach (var cumRating_ordersCount in productType_cumRating_ordersCount__Dictionary.Values)
            {
                if (cumRating_ordersCount.Item2 == 0)
                    continue;

                float meanRating = cumRating_ordersCount.Item1 / cumRating_ordersCount.Item2;
                data.Add(meanRating);
            }

            return new StatisticsReport("Средняя оценка заказа по категориям (Spiro,Воздуховод,Глушитель,Дефлектор)", data);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<StatisticsReport, ErrorInfo> MaxOrderPrice()
    {
        try
        {
            List<float> data = new List<float>();
            var maxPrice = _db.Orders.Max(order => order.Price);
            data.Add(maxPrice);
            return new StatisticsReport("Максимальная цена заказа", data);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }
}