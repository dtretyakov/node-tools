using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    /// <summary>
    ///     Lookup command response.
    /// </summary>
    internal class LookupMessage : ResponseMessageBase
    {
        private readonly JObject _message;

        public LookupMessage(JObject message) : base(message)
        {
            _message = message;
        }

        public List<NodeEvaluationResult> Children { get; private set; }

        public override void Execute(object[] arguments)
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentOutOfRangeException("arguments");
            }

            var variable = arguments[0] as NodeEvaluationResult;
            if (variable == null)
            {
                string message = string.Format("Invalid arguments: {0}", arguments);
                throw new ArgumentException(message, "arguments");
            }

            // Retrieve references
            var refs = (JArray) _message["refs"];
            var references = new Dictionary<int, JToken>(refs.Count);
            for (int i = 0; i < refs.Count; i++)
            {
                JToken reference = refs[i];
                var id = (int) reference["handle"];
                references.Add(id, reference);
            }

            // Retrieve properties
            var variableId = variable.Id.ToString(CultureInfo.InvariantCulture);
            var objectData = _message["body"][variableId];
            var properties = new List<NodeEvaluationResult>();

            var props = (JArray)objectData["properties"];
            if (props != null)
            {
                for (int i = 0; i < props.Count; i++)
                {
                    var variableProvider = new LookupVariableProvider(props[i], references, variable);
                    NodeEvaluationResult result = NodeMessageFactory.CreateVariable(variableProvider);
                    properties.Add(result);
                }
            }

            // Try to get prototype
            var prototype = objectData["protoObject"];
            if (prototype != null)
            {
                var variableProvider = new LookupPrototypeProvider(prototype, references, variable);
                NodeEvaluationResult result = NodeMessageFactory.CreateVariable(variableProvider);
                properties.Add(result);
            }

            Children = properties.OrderBy(p => p.Name).ToList();
        }
    }
}