using Azure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST10361554_CLDV6212_POE_Part_3_Functions.JSONConverters
{
    public class ETagConverter : JsonConverter<ETag>
    {
        public override ETag ReadJson(JsonReader reader, Type objectType, ETag existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Read the value from the JSON and convert it to a string
            var eTagString = reader.Value?.ToString();

            // Create a new ETag instance from the string
            return string.IsNullOrEmpty(eTagString) ? default : new ETag(eTagString);
        }

        // WriteJson method to serialize the ETag to JSON
        public override void WriteJson(JsonWriter writer, ETag value, JsonSerializer serializer)
        {
            // Write the ETag value as a string to JSON
            writer.WriteValue(value.ToString());
        }
    }
}
