using System;
using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class SetVariableValueMessage : ResponseMessageBase
    {
        private readonly JObject _message;

        public SetVariableValueMessage(JObject message) : base(message)
        {
            _message = message;
        }

        public NodeEvaluationResult Result { get; private set; }

        public override void Execute(object[] arguments)
        {
            if (arguments.Length != 3)
            {
                throw new ArgumentOutOfRangeException("arguments");
            }

            var debugger = arguments[0] as IDebuggerManager;
            var stackFrame = arguments[1] as NodeStackFrame;
            var name = arguments[2] as string;
            if (debugger == null || stackFrame == null)
            {
                string message = string.Format("Invalid arguments: {0}", arguments);
                throw new ArgumentException(message, "arguments");
            }

            var variableProvider = new NewValueVariableProvider(_message, debugger, stackFrame, name);
            Result = NodeMessageFactory.CreateVariable(variableProvider);
        }
    }
}