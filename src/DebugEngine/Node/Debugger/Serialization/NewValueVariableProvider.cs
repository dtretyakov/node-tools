using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class NewValueVariableProvider : IVariableProvider
    {
        public NewValueVariableProvider(JObject message, IDebuggerManager debugger, NodeStackFrame stackFrame, string name)
        {
            Id = (int) message["body"]["newValue"]["handle"];
            Debugger = debugger;
            StackFrame = stackFrame;
            Parent = null;
            Name = name;
            TypeName = (string)message["body"]["newValue"]["type"];
            Value = (string)message["body"]["newValue"]["value"];
            Class = (string)message["body"]["newValue"]["className"];
            Text = (string)message["body"]["newValue"]["text"];
            Attributes = PropertyAttribute.None;
            Type = PropertyType.Normal;
        }

        public int Id { get; private set; }

        public IDebuggerManager Debugger { get; private set; }

        public NodeStackFrame StackFrame { get; private set; }

        public NodeEvaluationResult Parent { get; private set; }

        public string Name { get; private set; }

        public string TypeName { get; private set; }

        public string Value { get; private set; }

        public string Class { get; private set; }

        public string Text { get; private set; }

        public PropertyAttribute Attributes { get; private set; }

        public PropertyType Type { get; private set; }
    }
}