namespace DebugEngine.Node.Debugger.Serialization
{
    internal interface IEventMessage
    {
        bool IsSuccessful { get; }

        bool IsRunning { get; }
    }
}