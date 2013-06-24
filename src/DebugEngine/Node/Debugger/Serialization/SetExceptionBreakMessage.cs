using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class SetExceptionBreakMessage : ResponseMessageBase
    {
        public SetExceptionBreakMessage(JObject message) : base(message)
        {
        }

        public override void Execute(object[] parameters)
        {
        }
    }
}