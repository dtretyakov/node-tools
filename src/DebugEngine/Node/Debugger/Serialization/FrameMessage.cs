using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DebugEngine.Node.Debugger.Serialization
{
    internal class FrameMessage : ResponseMessageBase
    {
        private readonly JObject _message;

        public FrameMessage(JObject message) : base(message)
        {
            _message = message;
        }

        public Version Version { get; private set; }

        public override void Execute(object[] arguments)
        {
            // Extract scripts
            var references = (JArray)_message["refs"];
            var scripts = new List<NodeScript>(references.Count);
            for (int i = 0; i < references.Count; i++)
            {
                JToken reference = references[i];
                var scriptId = reference["id"].Value<int>();
                var filename = reference["name"].Value<string>();

                scripts.Add(new NodeScript(scriptId, filename));
            }

            ScriptId = (int)_message["body"]["func"]["scriptId"];
            Line = (int) _message["body"]["line"];
            Column = (int) _message["body"]["column"];
            Scripts = scripts;
        }

        public int ScriptId { get; private set; }

        public int Column { get; private set; }

        public int Line { get; private set; }

        public List<NodeScript> Scripts { get; private set; }
    }
}