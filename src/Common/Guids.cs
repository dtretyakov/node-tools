// Guids.cs
// MUST match guids.h

using System;

namespace Common
{
    public static class Guids
    {
        public const string NodeToolsPackageString = "4ae051ed-7874-4aa0-a046-e5fb3c00c6a0";
        public const string NodeToolsCmdSetString = "3eba873a-98c1-4636-a40a-508a3fe65961";
        public const string ToolWindowPersistanceString = "af517204-2703-4ff1-b39a-f4113c718989";
        public const string GeneralPropertyPageString = "1196B0CF-07AF-41C1-B63C-BBA04E6C4714";
        public const string DebugEngineString = "{EC1375B7-E2CE-43E8-BF75-DC638DE1F1F9}";

        public static readonly Guid NodeToolsCmdSet = new Guid(NodeToolsCmdSetString);
        public static readonly Guid GeneralPropertyPage = new Guid(GeneralPropertyPageString);
        public static readonly Guid DebugEngine = new Guid(DebugEngineString);
    };
}