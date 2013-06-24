using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace DebugEngine.Node.Debugger.Serialization
{
    [JsonConverter(typeof (RawValueJsonConverter))]
    internal class RawValue
    {
        private readonly Regex _jsonTypes =
            new Regex(@"^(null|true|false)$|^("".*"")$|^('.*')$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex _undefinedType = new Regex(@"^(undefined)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _numberType = new Regex(@"^[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly string _value;

        public RawValue(string value)
        {
            _value = value;
        }

        public override string ToString()
        {
            if (_jsonTypes.IsMatch(_value))
            {
                return string.Format("{{ \"value\": {0} }}", _value);
            }

            if (_numberType.IsMatch(_value))
            {
                return string.Format("{{ \"type\": \"number\", \"stringDescription\": \"{0}\" }}", _value);
            }
            
            if (_undefinedType.IsMatch(_value))
            {
                return "{ \"type\": \"undefined\" }";
            }

            throw new InvalidOperationException("Only primitive JavaScript types are supported.");
        }
    }
}