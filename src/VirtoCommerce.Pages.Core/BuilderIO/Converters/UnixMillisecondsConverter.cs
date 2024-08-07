using System;
using Newtonsoft.Json;

namespace VirtoCommerce.Pages.Core.BuilderIO.Converters
{
    public class UnixMillisecondsConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DateTime dateTime)
            {
                var unixTimeMilliseconds = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
                writer.WriteValue(unixTimeMilliseconds);
            }
            else
            {
                throw new JsonSerializationException("Expected date object value.");
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                var unixTimeMilliseconds = (long)reader.Value;
                return DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds).DateTime;
            }

            throw new JsonSerializationException("Expected integer milliseconds value.");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime);
        }
    }
}
