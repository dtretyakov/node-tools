using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class SuspendMessage : ResponseMessageBase
    {
        public SuspendMessage(JObject message) : base(message)
        {
        }

        public override void Execute(object[] parameters)
        {
        }
    }
}