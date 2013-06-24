using System;

namespace DebugEngine.Node
{
    [Flags]
    internal enum NodeExpressionType
    {
        None = 0,

        Property = 0x1,

        Function = 0x2,

        Boolean = 0x4,

        Private = 0x8,

        Expandable = 0x10,

        ReadOnly = 0x20,

        String = 0x40
    }
}