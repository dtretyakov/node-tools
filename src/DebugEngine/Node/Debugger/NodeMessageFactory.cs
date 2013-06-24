using System;
using DebugEngine.Node.Debugger.Serialization;
using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger
{
    internal static class NodeMessageFactory
    {
        public static IResponseMessage CreateResponseMessage(JObject message)
        {
            switch ((string) message["command"])
            {
                case "evaluate":
                    return new EvaluateMessage(message);

                case "continue":
                    return new ContinueMessage(message);

                case "backtrace":
                    return new BacktraceMessage(message);

                case "lookup":
                    return new LookupMessage(message);

                case "setVariableValue":
                    return new SetVariableValueMessage(message);

                case "frame":
                    return new FrameMessage(message);

                case "setbreakpoint":
                    return new SetBreakpointMessage(message);

                case "changebreakpoint":
                    return new ChangeBreakpointMessage(message);

                case "clearbreakpoint":
                    return new ClearBreakpointMessage(message);

                case "setexceptionbreak":
                    return new SetExceptionBreakMessage(message);

                case "suspend":
                    return new SuspendMessage(message);

                case "version":
                    return new VersionMessage(message);
            }

            throw new ArgumentOutOfRangeException("message");
        }

        public static IEventMessage CreateEventMessage(JObject message)
        {
            switch ((string) message["event"])
            {
                case "afterCompile":
                    return new CompileScriptMessage(message);

                case "break":
                    return new BreakpointMessage(message);

                case "exception":
                    return new ExceptionMessage(message);
            }

            throw new ArgumentOutOfRangeException("message");
        }

        public static NodeEvaluationResult CreateVariable(IVariableProvider variable)
        {
            int id = variable.Id;
            IDebuggerManager debugger = variable.Debugger;
            NodeStackFrame stackFrame = variable.StackFrame;
            NodeEvaluationResult parent = variable.Parent;

            string name = variable.Name;
            string fullName = GetFullName(parent, variable.Name, ref name);
            string stringValue;
            string typeName = variable.TypeName;
            NodeExpressionType type = 0;

            if (variable.Attributes.HasFlag(PropertyAttribute.ReadOnly))
            {
                type |= NodeExpressionType.ReadOnly;
            }

            if (variable.Attributes.HasFlag(PropertyAttribute.DontEnum))
            {
                type |= NodeExpressionType.Private;
            }

            switch (typeName)
            {
                case "undefined":
                    stringValue = "undefined";
                    typeName = "Undefined";
                    break;

                case "null":
                    stringValue = "null";
                    typeName = "Null";
                    break;

                case "number":
                    stringValue = variable.Value;
                    typeName = "Number";
                    break;

                case "boolean":
                    stringValue = variable.Value.ToLowerInvariant();
                    typeName = "Boolean";
                    type |= NodeExpressionType.Boolean;
                    break;

                case "regexp":
                    stringValue = variable.Value;
                    typeName = "Regular Expression";
                    type |= NodeExpressionType.Expandable;
                    break;

                case "function":
                    stringValue = string.IsNullOrEmpty(variable.Text) ? "{Function}" : variable.Text;
                    typeName = "Function";
                    type |= NodeExpressionType.Function | NodeExpressionType.Expandable;
                    break;

                case "string":
                    stringValue = variable.Value;
                    typeName = "String";
                    type |= NodeExpressionType.String;
                    break;

                case "object":
                    stringValue = variable.Class == "Object" ? "{...}" : string.Format("{{{0}}}", variable.Class);
                    typeName = "Object";
                    type |= NodeExpressionType.Expandable;
                    break;

                case "error":
                    stringValue = variable.Value;
                    if (!string.IsNullOrEmpty(stringValue) && stringValue.StartsWith("Error: "))
                    {
                        stringValue = variable.Value.Substring(7);
                    }
                    typeName = "Error";
                    type |= NodeExpressionType.Expandable;
                    break;

                default:
                    stringValue = variable.Value;
                    break;
            }

            return new NodeEvaluationResult(debugger, stringValue, null, typeName, name, fullName, type, stackFrame, id);
        }

        private static string GetFullName(NodeEvaluationResult parent, string fullName, ref string name)
        {
            if (parent == null)
            {
                return fullName;
            }

            fullName = string.Format(@"{0}[""{1}""]", parent.FullName, name);

            if (parent.TypeName != "Object")
            {
                return fullName;
            }

            int indexer;
            if (int.TryParse(name, out indexer))
            {
                name = string.Format("[{0}]", indexer);
                fullName = parent.FullName + name;
            }

            return fullName;
        }
    }
}