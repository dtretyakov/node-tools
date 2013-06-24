using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class BacktraceVariableProvider : IVariableProvider
    {
        private const string AnonymousVariable = "(anonymous variable)";

        public BacktraceVariableProvider(JToken parameter, IDebuggerManager debugger, NodeStackFrame stackFrame)
        {
            Id = (int) parameter["value"]["ref"];
            Debugger = debugger;
            StackFrame = stackFrame;
            Parent = null;
            Name = (string) parameter["name"] ?? AnonymousVariable;
            TypeName = (string) parameter["value"]["type"];
            Value = (string) parameter["value"]["value"];
            Class = (string) parameter["value"]["className"];
            Text = (string) parameter["value"]["text"];
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