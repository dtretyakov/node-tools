using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class BreakpointMessage : IEventMessage
    {
        public BreakpointMessage(JObject message)
        {
            IsRunning = false;
            IsSuccessful = true;
            ScriptId = (int) message["body"]["script"]["id"];
            Filename = (string) message["body"]["script"]["name"];
            Line = (int) message["body"]["sourceLine"];
            Column = (int) message["body"]["sourceColumn"];
        }

        public BreakpointMessage(int scriptId, string filename, int line, int column)
        {
            ScriptId = scriptId;
            Filename = filename;
            Line = line;
            Column = column;
        }

        public int Line { get; private set; }

        public int Column { get; private set; }

        public string Filename { get; private set; }

        public int ScriptId { get; private set; }

        public bool IsSuccessful { get; private set; }

        public bool IsRunning { get; private set; }
    }
}