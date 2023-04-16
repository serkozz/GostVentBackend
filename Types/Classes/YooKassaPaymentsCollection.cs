namespace YooKassaPaymentInfoNamespace
{
    using System;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class YooKassaPaymentsCollection
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("next_cursor")]
        public Guid NextCursor { get; set; }

        [JsonProperty("items")]
        public YooKassaPaymentInfo[] Items { get; set; }
    }

    public partial class YooKassaPaymentInfo
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("amount")]
        public Amount Amount { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("recipient")]
        public Recipient Recipient { get; set; }

        [JsonProperty("payment_method")]
        public PaymentMethod PaymentMethod { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("expires_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? ExpiresAt { get; set; }

        [JsonProperty("test")]
        public bool Test { get; set; }

        [JsonProperty("paid")]
        public bool Paid { get; set; }

        [JsonProperty("refundable")]
        public bool Refundable { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("authorization_details", NullValueHandling = NullValueHandling.Ignore)]
        public AuthorizationDetails AuthorizationDetails { get; set; }

        [JsonProperty("confirmation", NullValueHandling = NullValueHandling.Ignore)]
        public Confirmation Confirmation { get; set; }

        [JsonProperty("income_amount", NullValueHandling = NullValueHandling.Ignore)]
        public Amount IncomeAmount { get; set; }

        [JsonProperty("captured_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? CapturedAt { get; set; }

        [JsonProperty("refunded_amount", NullValueHandling = NullValueHandling.Ignore)]
        public Amount RefundedAmount { get; set; }

        [JsonProperty("cancellation_details", NullValueHandling = NullValueHandling.Ignore)]
        public CancellationDetails CancellationDetails { get; set; }
    }

    public partial class Amount
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("currency")]
        public Currency Currency { get; set; }
    }

    public partial class AuthorizationDetails
    {
        [JsonProperty("rrn")]
        public string Rrn { get; set; }

        [JsonProperty("auth_code")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long AuthCode { get; set; }

        [JsonProperty("three_d_secure")]
        public ThreeDSecure ThreeDSecure { get; set; }
    }

    public partial class ThreeDSecure
    {
        [JsonProperty("applied")]
        public bool Applied { get; set; }

        [JsonProperty("method_completed")]
        public bool MethodCompleted { get; set; }

        [JsonProperty("challenge_completed")]
        public bool ChallengeCompleted { get; set; }
    }

    public partial class CancellationDetails
    {
        [JsonProperty("party")]
        public string Party { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public partial class Confirmation
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("return_url")]
        public Uri ReturnUrl { get; set; }

        [JsonProperty("confirmation_url")]
        public Uri ConfirmationUrl { get; set; }
    }

    public partial class Metadata
    {
    }

    public partial class PaymentMethod
    {
        [JsonProperty("type")]
        public TypeEnum Type { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("saved")]
        public bool Saved { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("card", NullValueHandling = NullValueHandling.Ignore)]
        public Card Card { get; set; }
    }

    public partial class Card
    {
        [JsonProperty("first6")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long First6 { get; set; }

        [JsonProperty("last4")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Last4 { get; set; }

        [JsonProperty("expiry_year")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long ExpiryYear { get; set; }

        [JsonProperty("expiry_month")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long ExpiryMonth { get; set; }

        [JsonProperty("card_type")]
        public string CardType { get; set; }

        [JsonProperty("issuer_country")]
        public string IssuerCountry { get; set; }
    }

    public partial class Recipient
    {
        [JsonProperty("account_id")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long AccountId { get; set; }

        [JsonProperty("gateway_id")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long GatewayId { get; set; }
    }

    public enum Currency { Rub };

    public enum TypeEnum { BankCard };

    public partial class YooKassaPaymentsCollection
    {
        public static YooKassaPaymentsCollection FromJson(string json) => JsonConvert.DeserializeObject<YooKassaPaymentsCollection>(json, YooKassaPaymentInfoNamespace.Converter.Settings);
    }

    public partial class YooKassaPaymentInfo
    {
        public static YooKassaPaymentInfo FromJson(string json) => JsonConvert.DeserializeObject<YooKassaPaymentInfo>(json, YooKassaPaymentInfoNamespace.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this YooKassaPaymentsCollection self) => JsonConvert.SerializeObject(self, YooKassaPaymentInfoNamespace.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                CurrencyConverter.Singleton,
                TypeEnumConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class CurrencyConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Currency) || t == typeof(Currency?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            if (value == "RUB")
            {
                return Currency.Rub;
            }
            throw new Exception("Cannot unmarshal type Currency");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Currency)untypedValue;
            if (value == Currency.Rub)
            {
                serializer.Serialize(writer, "RUB");
                return;
            }
            throw new Exception("Cannot marshal type Currency");
        }

        public static readonly CurrencyConverter Singleton = new CurrencyConverter();
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }

    internal class TypeEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(TypeEnum) || t == typeof(TypeEnum?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            if (value == "bank_card")
            {
                return TypeEnum.BankCard;
            }
            throw new Exception("Cannot unmarshal type TypeEnum");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (TypeEnum)untypedValue;
            if (value == TypeEnum.BankCard)
            {
                serializer.Serialize(writer, "bank_card");
                return;
            }
            throw new Exception("Cannot marshal type TypeEnum");
        }

        public static readonly TypeEnumConverter Singleton = new TypeEnumConverter();
    }
}
