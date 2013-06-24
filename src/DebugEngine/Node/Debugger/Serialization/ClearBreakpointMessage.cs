using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class ClearBreakpointMessage : ResponseMessageBase
    {
        public ClearBreakpointMessage(JObject message) : base(message)
        {
        }

        public override void Execute(object[] parameters)
        {
        }
    }
}