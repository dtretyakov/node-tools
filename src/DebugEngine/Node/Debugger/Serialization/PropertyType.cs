namespace DebugEngine.Node.Debugger.Serialization
{
    internal enum PropertyType
    {
        Normal = 0,

        Field = 1,

        ConstantFunction = 2,

        Callbacks = 3,

        Handler = 4,

        Interceptor = 5,

        Transition = 6,

        Nonexistent = 7
    }
}