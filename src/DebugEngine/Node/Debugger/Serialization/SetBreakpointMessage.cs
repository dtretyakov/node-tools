using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class SetBreakpointMessage : ResponseMessageBase
    {
        private readonly JObject _message;

        public SetBreakpointMessage(JObject message) : base(message)
        {
            _message = message;
        }

        public int Id { get; private set; }

        public int Line { get; private set; }

        public int Column { get; private set; }

        public override void Execute(object[] parameters)
        {
            Id = (int) _message["body"]["breakpoint"];
            var actual = (JArray) _message["body"]["actual_locations"];
            JToken breakpoint = actual.Count == 1 ? actual[0] : _message["body"];
            Line = (int) breakpoint["line"];
            Column = (int) breakpoint["column"];
        }
    }
}