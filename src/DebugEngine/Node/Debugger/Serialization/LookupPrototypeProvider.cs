using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class LookupPrototypeProvider : IVariableProvider
    {
        public LookupPrototypeProvider(JToken prototype, Dictionary<int, JToken> references, NodeEvaluationResult variable)
        {
            Id = (int) prototype["ref"];

            JToken reference = references[Id];

            Debugger = variable.Debugger;
            StackFrame = variable.StackFrame;
            Parent = variable;
            Name = "[prototype]";
            TypeName = (string) reference["type"];
            Value = (string) reference["value"];
            Class = (string) reference["className"];
            Text = (string) reference["text"];
            Attributes = PropertyAttribute.DontEnum;
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