using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class BacktraceMessage : ResponseMessageBase
    {
        private const string AnonymousFunction = "(anonymous function)";
        private readonly JObject _message;

        public BacktraceMessage(JObject message) : base(message)
        {
            _message = message;
        }

        public IList<NodeScript> Scripts { get; private set; }

        public IList<NodeStackFrame> StackFrames { get; private set; }

        public override void Execute(object[] arguments)
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentOutOfRangeException("arguments");
            }

            var debugger = arguments[0] as IDebuggerManager;
            if (debugger == null)
            {
                string message = string.Format("Invalid arguments: {0}", arguments);
                throw new ArgumentException(message, "arguments");
            }

            // Extract scripts
            var references = (JArray) _message["refs"];
            Dictionary<int, NodeScript> scripts = GetScripts(references);
            Scripts = scripts.Values.ToList();

            // Extract frames
            var frames = (JArray) _message["body"]["frames"];
            if (frames == null)
            {
                StackFrames = new List<NodeStackFrame>();
                return;
            }

            var stackFrames = new List<NodeStackFrame>(frames.Count);

            for (int i = 0; i < frames.Count; i++)
            {
                JToken frame = frames[i];

                // Create stack frame
                string name = GetFrameName(frame);
                var scriptId = (int) frame["func"]["scriptId"];
                NodeScript script = scripts[scriptId];
                int line = (int) frame["line"] + 1;
                var stackFrameId = (int) frame["index"];

                var stackFrame = new NodeStackFrame(debugger, name, script.Filename, line, line, line, stackFrameId);

                // Locals
                var variables = (JArray) frame["locals"];
                List<NodeEvaluationResult> locals = GetVariables(variables, debugger, stackFrame);

                // Parameters
                variables = (JArray) frame["arguments"];
                List<NodeEvaluationResult> parameters = GetVariables(variables, debugger, stackFrame);

                stackFrame.Locals = locals;
                stackFrame.Parameters = parameters;

                stackFrames.Add(stackFrame);
            }

            StackFrames = stackFrames;
        }

        private static string GetFrameName(JToken frame)
        {
            var framename = (string) frame["func"]["name"];
            if (string.IsNullOrEmpty(framename))
            {
                framename = (string) frame["func"]["inferredName"];
            }
            if (string.IsNullOrEmpty(framename))
            {
                framename = AnonymousFunction;
            }
            return framename;
        }

        private static Dictionary<int, NodeScript> GetScripts(JArray references)
        {
            var scripts = new Dictionary<int, NodeScript>(references.Count);
            for (int i = 0; i < references.Count; i++)
            {
                JToken reference = references[i];
                var scriptId = reference["id"].Value<int>();
                var filename = reference["name"].Value<string>();

                scripts.Add(scriptId, new NodeScript(scriptId, filename));
            }
            return scripts;
        }

        private static List<NodeEvaluationResult> GetVariables(JArray variables, IDebuggerManager debugger, NodeStackFrame stackFrame)
        {
            var results = new List<NodeEvaluationResult>(variables.Count);
            for (int j = 0; j < variables.Count; j++)
            {
                JToken variable = variables[j];
                var variableProvider = new BacktraceVariableProvider(variable, debugger, stackFrame);
                NodeEvaluationResult result = NodeMessageFactory.CreateVariable(variableProvider);
                results.Add(result);
            }
            return results;
        }
    }
}