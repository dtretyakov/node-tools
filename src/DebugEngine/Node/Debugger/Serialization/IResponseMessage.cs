namespace DebugEngine.Node.Debugger.Serialization
{
    internal interface IResponseMessage
    {
        int MessageId { get; }

        bool IsSuccessful { get; }

        bool IsRunning { get; }

        void Execute(object[] arguments);
    }
}