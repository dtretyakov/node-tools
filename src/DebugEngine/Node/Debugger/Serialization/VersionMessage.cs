using System;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class VersionMessage : ResponseMessageBase
    {
        private readonly JObject _message;

        public VersionMessage(JObject message) : base(message)
        {
            _message = message;
        }

        public Version Version { get; private set; }

        public override void Execute(object[] arguments)
        {
            Version version;
            var versionString = (string) _message["body"]["V8Version"];

            if (Version.TryParse(versionString, out version))
            {
                Version = version;
            }
            else
            {
                Debug.Fail("Invalid version response.");
                Version = new Version();
            }
        }
    }
}