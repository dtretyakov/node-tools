using System.Globalization;
using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class ExceptionMessage : IEventMessage
    {
        public ExceptionMessage(JObject message)
        {
            IsRunning = false;
            IsSuccessful = (bool) message["success"];
            ScriptId = (int) message["body"]["script"]["id"];
            Filename = (string) message["body"]["script"]["name"];
            Line = (int) message["body"]["sourceLine"];
            Column = (int) message["body"]["sourceColumn"];
            IsUnhandled = (bool) message["body"]["uncaught"];
            ExceptionId = (int) message["body"]["exception"]["handle"];
            Description = (string) message["body"]["exception"]["text"];
            string typeName = (string) message["body"]["exception"]["className"]
                              ?? (string) message["body"]["exception"]["type"];
            typeName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(typeName);
            TypeName = string.Format("{0} exception", typeName);
        }

        public int Line { get; private set; }

        public int Column { get; private set; }

        public string Filename { get; private set; }

        public int ScriptId { get; private set; }

        public string Description { get; private set; }

        public int ExceptionId { get; private set; }

        public string TypeName { get; private set; }

        public bool IsUnhandled { get; private set; }

        public bool IsSuccessful { get; private set; }

        public bool IsRunning { get; private set; }
    }
}