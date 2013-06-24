namespace DebugEngine.Node.Debugger.Serialization
{
    internal interface IVariableProvider
    {
        int Id { get; }

        IDebuggerManager Debugger { get; }

        NodeStackFrame StackFrame { get; }

        NodeEvaluationResult Parent { get; }

        string Name { get; }

        string TypeName { get; }

        string Value { get; }

        string Class { get; }

        string Text { get; }

        PropertyAttribute Attributes { get; }

        PropertyType Type { get; }
    }
}