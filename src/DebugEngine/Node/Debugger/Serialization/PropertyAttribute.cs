using System;

namespace DebugEngine.Node.Debugger.Serialization
{
    [Flags]
    internal enum PropertyAttribute
    {
        None = 0,

        ReadOnly = 1,

        DontEnum = 2,

        DontDelete = 4
    }
}