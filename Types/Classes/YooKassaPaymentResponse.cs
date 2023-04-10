namespace YooKassaPaymentResponseNamespace
{
    using System;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class YooKassaPaymentResponse
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

        [JsonProperty("confirmation")]
        public Confirmation Confirmation { get; set; }

        [JsonProperty("test")]
        public bool Test { get; set; }

        [JsonProperty("paid")]
        public bool Paid { get; set; }

        [JsonProperty("refundable")]
        public bool Refundable { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
    }

    public partial class Amount
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }
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
        public string Type { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("saved")]
        public bool Saved { get; set; }
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

    public partial class YooKassaPaymentResponse
    {
        public static YooKassaPaymentResponse FromJson(string json) => JsonConvert.DeserializeObject<YooKassaPaymentResponse>(json, YooKassaPaymentResponseNamespace.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this YooKassaPaymentResponse self) => JsonConvert.SerializeObject(self, YooKassaPaymentResponseNamespace.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
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
}