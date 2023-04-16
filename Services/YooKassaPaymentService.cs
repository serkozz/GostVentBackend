using OneOf;
using Types.Classes;
using System.Text;
using EF.Models;
using Codes = System.Net.HttpStatusCode;
using YooKassaPaymentInfoNamespace;
using EF.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class YooKassaPaymentService
{
    public string? ShopId { get; set; }
    public string? ShopSecret { get; set; }
    private readonly OrderService _orderService;
    private readonly SQLiteContext _db;

    public YooKassaPaymentService(ConfigurationManager configManager, OrderService orderService, SQLiteContext db)
    {
        ShopId = configManager.GetSection("Payment:YooKassa:ShopId").Value;
        ShopSecret = configManager.GetSection("Payment:YooKassa:ShopSecret").Value;
        _orderService = orderService;
        _db = db;
    }

    /// <summary>
    /// Создает платеж YooKassa используя API
    /// </summary>
    /// <param name="amount"></param>
    public async Task<OneOf<YooKassaPaymentInfo, ErrorInfo>> CreatePaymentUsingAPI(Order order, string email, string returnUrl)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage();
                request.RequestUri = new Uri("https://api.yookassa.ru/v3/payments");
                request.Method = HttpMethod.Post;

                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("User-Agent", "Thunder Client (https://www.thunderclient.com)");
                request.Headers.Add("Idempotence-Key", RandomString(64));
                string authBase64 = Base64Encode($"{ShopId}:{ShopSecret}");
                request.Headers.Add("Authorization", $"Basic {authBase64}");

                var bodyString = $"{{  \"amount\": {{    \"value\": \"{order.Price}\",    \"currency\": \"RUB\"  }},  \"payment_method_data\": {{    \"type\": \"bank_card\"  }},  \"confirmation\": {{    \"type\": \"redirect\",    \"return_url\": \"{returnUrl}\"  }},  \"description\": \"Оплата заказа {order.Name} от пользователя {email}\"}}";
                var content = new StringContent(bodyString, Encoding.UTF8, "application/json");
                request.Content = content;

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return new ErrorInfo(response.StatusCode, response.ReasonPhrase is not null ? response.ReasonPhrase : "YooKassaApiError, watch docs");

                var result = await response.Content.ReadAsStringAsync();
                YooKassaPaymentInfo paymentResponse = YooKassaPaymentInfo.FromJson(result);


                var entityEntry = _db.OrderPayments.Add(new OrderPayment()
                {
                    PaymentId = paymentResponse.Id,
                    Order = order,
                    OrderId = order.Id
                });

                if (entityEntry.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                {
                    _db.SaveChanges();
                    return paymentResponse;
                }

                return new ErrorInfo(Codes.NotFound, "Невозможно создать запись в таблице OrderPayments, без нее невозможно осуществить оплату");
            }
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

    public async Task<OneOf<YooKassaPaymentsCollection, ErrorInfo>> GetAllPayments()
    {
        using (HttpClient client = new HttpClient())
        {
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri("https://api.yookassa.ru/v3/payments/");
            request.Method = HttpMethod.Get;

            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("User-Agent", "Thunder Client (https://www.thunderclient.com)");
            request.Headers.Add("Idempotence-Key", RandomString(64));
            string authBase64 = Base64Encode($"{ShopId}:{ShopSecret}");
            request.Headers.Add("Authorization", $"Basic {authBase64}");

            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            YooKassaPaymentsCollection payments = YooKassaPaymentsCollection.FromJson(result);

            if (payments is null || payments.Items.Length == 0)
                return new ErrorInfo(Codes.NotFound, "YooKassaApiError: Невозможно получить платежи!");

            return payments;
        }
    }

    public async Task<OneOf<YooKassaPaymentInfo, ErrorInfo>> GetPaymentById(Guid paymentId)
    {
        var paymentsOrError = await GetAllPayments();
        if (paymentsOrError.IsT1)
            return paymentsOrError.AsT1;

        YooKassaPaymentsCollection payments = paymentsOrError.AsT0;

        YooKassaPaymentInfo payment = payments.Items.Where(
            payment => payment.Id == paymentId
        ).First();

        if (payment is null)
            return new ErrorInfo(Codes.NotFound, "YooKassaApiError: Невозможно получить платеж по Id!");
        
        return payment;
    }

    public async Task<OneOf<YooKassaPaymentInfo, ErrorInfo>> GetPaymentByOrderData(string orderName, string email)
    {
        var paymentsOrError = await GetAllPayments();
        if (paymentsOrError.IsT1)
            return paymentsOrError.AsT1;

        YooKassaPaymentsCollection payments = paymentsOrError.AsT0;

        var order = _orderService.GetOrdersByEmail(email).Where(
            order => order.Name == orderName
        ).FirstOrDefault();

        if (order is null)
            return new ErrorInfo(Codes.NotFound, "Невозможно получить заказ из таблицы заказов, без него нельзя получить информацию о статусе платежа");

        var orderPayment = _db.OrderPayments.Where(
            orderPayment => orderPayment.OrderId == order.Id
        ).FirstOrDefault();

        if (orderPayment is null)
            return new ErrorInfo(Codes.NotFound, "Невозможно получить id платежа из таблицы платежей, без него нельзя получить информацию о статусе платежа");

        YooKassaPaymentInfo payment = payments.Items.Where(
            payment => payment.Id == orderPayment.PaymentId
        ).First();

        if (payment is null)
            return new ErrorInfo(Codes.NotFound, "YooKassaApiError: Невозможно получить платеж по Id!");

        return payment;
    }

    private string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    private string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
}