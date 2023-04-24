using EF.Contexts;
using EF.Models;
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

    public OneOf<StatisticsReport, ErrorInfo> SaveStats(StatisticsReport report)
    {
        try
        {
            if (report.Data.Count == 0)
                return new ErrorInfo(Codes.NotFound, "Невозможно сохранить статистику без содержащихся в ней данных");

            var existingRepOrNull = _db.StatisticsReport.FirstOrDefault(statRep => statRep.CreationDate == report.CreationDate && statRep.Name == report.Name);

            if (existingRepOrNull is not null)
            {
                existingRepOrNull.CopyFrom(report);
                _db.StatisticsReport.Update(existingRepOrNull);
                int affectedRows = _db.SaveChanges();

                if (affectedRows == 0)
                    return new ErrorInfo(Codes.NotFound, "Невозможно обновить существующую статистику");

                return existingRepOrNull;
            }

            var entry = _db.StatisticsReport.Add(report);

            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Added)
            {
                int rowsAffected = _db.SaveChanges();
                return entry.Entity;
            }
            return new ErrorInfo(Codes.NotFound, "Возникла непредвиденная ошибка при сохранении статистики в БД");
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<StatisticsReport, ErrorInfo> MeanOrdersPricePerClient(DateOnly? reportCreationDate = null)
    {
        try
        {
            float priceCumSum = 0;
            int ordersCount = 0;
            List<StatisticsData> data = new List<StatisticsData>();
            foreach (var order in _db.Orders.Where(order => order.Status == OrderStatus.Finished && order.Price != 0))
            {
                priceCumSum += order.Price;
                ordersCount++;
            }
            float meanPrice = priceCumSum / ordersCount;
            data.Add(new StatisticsData("Средняя сумма заказа", meanPrice));
            return new StatisticsReport("Средняя сумма заказа", data, reportCreationDate);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<StatisticsReport, ErrorInfo> MeanOrdersPriceByOrderType(DateOnly? reportCreationDate = null)
    {
        try
        {
            Dictionary<ProductType, Tuple<float, int>> productType_ordersCumPrice_ordersCount__Dictionary = new Dictionary<ProductType, Tuple<float, int>>();
            var productTypes = Enum.GetValues(typeof(ProductType));
            List<StatisticsData> data = new List<StatisticsData>();

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

            foreach (var kvp in productType_ordersCumPrice_ordersCount__Dictionary)
            {
                if (kvp.Value.Item2 == 0)
                    continue;

                float meanPrice = kvp.Value.Item1 / kvp.Value.Item2;
                data.Add(new StatisticsData($"{kvp.Key.ToString()}", meanPrice));
            }

            return new StatisticsReport("Средняя сумма заказа по категориям (Spiro,Воздуховод,Глушитель,Дефлектор)", data, reportCreationDate);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<StatisticsReport, ErrorInfo> MeanOrdersFinishTimeByOrderType(DateOnly? reportCreationDate = null)
    {
        try
        {
            Dictionary<ProductType, Tuple<float, int>> productType_LeadDays_ordersCount__Dictionary = new Dictionary<ProductType, Tuple<float, int>>();
            var productTypes = Enum.GetValues(typeof(ProductType));
            List<StatisticsData> data = new List<StatisticsData>();

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

            foreach (var kvp in productType_LeadDays_ordersCount__Dictionary)
            {
                if (kvp.Value.Item2 == 0)
                    continue;

                float meanDays = kvp.Value.Item1 / kvp.Value.Item2;
                data.Add(new StatisticsData($"{kvp.Key.ToString()}", meanDays));
            }

            return new StatisticsReport("Среднее время выполнения заказа по категориям (Spiro,Воздуховод,Глушитель,Дефлектор)", data, reportCreationDate);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<StatisticsReport, ErrorInfo> OrdersPercentByOrderType(DateOnly? reportCreationDate = null)
    {
        try
        {
            Dictionary<ProductType, int> productType_ordersCount__Dictionary = new Dictionary<ProductType, int>();
            var productTypes = Enum.GetValues(typeof(ProductType));
            List<StatisticsData> data = new List<StatisticsData>();

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

            foreach (var kvp in productType_ordersCount__Dictionary)
            {
                float percent = (float)kvp.Value / (float)ordersCount;
                data.Add(new StatisticsData($"{kvp.Key.ToString()}", percent));
            }

            return new StatisticsReport("Процент заказов среди всех по категориям (Spiro,Воздуховод,Глушитель,Дефлектор)", data, reportCreationDate);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<StatisticsReport, ErrorInfo> MeanOrderRatingByType(DateOnly? reportCreationDate = null)
    {
        try
        {
            Dictionary<ProductType, Tuple<float, int>> productType_cumRating_ordersCount__Dictionary = new Dictionary<ProductType, Tuple<float, int>>();
            var productTypes = Enum.GetValues(typeof(ProductType));
            List<StatisticsData> data = new List<StatisticsData>();

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

            foreach (var kvp in productType_cumRating_ordersCount__Dictionary)
            {
                if (kvp.Value.Item2 == 0)
                    continue;

                float meanRating = kvp.Value.Item1 / kvp.Value.Item2;
                data.Add(new StatisticsData($"{kvp.Key.ToString()}", meanRating));
            }

            return new StatisticsReport("Средняя оценка заказа по категориям (Spiro,Воздуховод,Глушитель,Дефлектор)", data, reportCreationDate);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<StatisticsReport, ErrorInfo> MaxOrderPrice(DateOnly? reportCreationDate = null)
    {
        try
        {
            List<StatisticsData> data = new List<StatisticsData>();
            var maxPrice = _db.Orders.Max(order => order.Price);
            data.Add(new StatisticsData("Максимальная цена заказа", maxPrice));
            return new StatisticsReport("Максимальная цена заказа", data, reportCreationDate);
        }
        catch (System.Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Возникла ошибка: {ex.Message}");
        }
    }

    public OneOf<List<StatisticsReport>, ErrorInfo> GetStatistics(DateOnly? date = null)
    {
        if (date is null)
            date = DateOnly.FromDateTime(DateTime.Now);

        var statisticsReports = _db.StatisticsReport.Where(rep => rep.CreationDate == date).ToList();

        if (statisticsReports.Count == 0)
            return new ErrorInfo(Codes.NotFound, $"Статистика за указанный день {date.ToString()} отсутствует");

        foreach (var report in statisticsReports)
        {
            _db.Entry(report).Collection(rep => rep.Data).Load();
        }

        return statisticsReports;
    }
}