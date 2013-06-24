using System;
using Newtonsoft.Json;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class RawValueJsonConverter : JsonConverter
    {
        private readonly Type _rawValueType = typeof (RawValue);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == _rawValueType;
        }
    }
}