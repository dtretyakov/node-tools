using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class ChangeBreakpointMessage : ResponseMessageBase
    {
        public ChangeBreakpointMessage(JObject message) : base(message)
        {
        }

        public override void Execute(object[] parameters)
        {
        }
    }
}