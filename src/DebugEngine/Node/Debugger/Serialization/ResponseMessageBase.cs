using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal abstract class ResponseMessageBase : IResponseMessage
    {
        protected ResponseMessageBase(JObject message)
        {
            MessageId = (int) message["request_seq"];
            IsRunning = (bool) message["running"];
            IsSuccessful = (bool) message["success"];
        }

        public int MessageId { get; private set; }

        public bool IsSuccessful { get; private set; }

        public bool IsRunning { get; private set; }

        public abstract void Execute(object[] arguments);
    }
}