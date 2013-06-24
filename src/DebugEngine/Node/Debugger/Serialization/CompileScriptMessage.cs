using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class CompileScriptMessage : IEventMessage
    {
        public CompileScriptMessage(JObject message)
        {
            IsRunning = (bool) message["running"];
            IsSuccessful = (bool) message["success"];
            ScriptId = (int) message["body"]["script"]["id"];
            Filename = (string) message["body"]["script"]["name"];
        }

        public string Filename { get; private set; }

        public int ScriptId { get; private set; }

        public bool IsSuccessful { get; private set; }

        public bool IsRunning { get; private set; }
    }
}