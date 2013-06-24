using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class ContinueMessage : ResponseMessageBase
    {
        public ContinueMessage(JObject message) : base(message)
        {
        }

        public override void Execute(object[] arguments)
        {
        }
    }
}