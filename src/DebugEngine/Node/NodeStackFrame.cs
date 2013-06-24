using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DebugEngine.Node.Debugger;

namespace DebugEngine.Node
{
    internal class NodeStackFrame
    {
        private readonly IDebuggerManager _debugger;
        private readonly int _endLine;
        private readonly string _filename;
        private readonly int _frameId;
        private readonly string _frameName;
        private readonly int _startLine;

        public NodeStackFrame(IDebuggerManager debugger, string frameName, string filename, int startLine, int endLine, int lineNo, int frameId)
        {
            _debugger = debugger;
            _frameName = frameName;
            _filename = filename;
            Line = lineNo;
            _frameId = frameId;
            _startLine = startLine;
            _endLine = endLine;
        }

        /// <summary>
        ///     The line nubmer where the current function/class/module starts
        /// </summary>
        public int StartLine
        {
            get { return _startLine; }
        }

        /// <summary>
        ///     The line number where the current function/class/module ends.
        /// </summary>
        public int EndLine
        {
            get { return _endLine; }
        }

        public int Line { get; set; }

        public string FunctionName
        {
            get { return _frameName; }
        }

        public string FileName
        {
            get { return _filename; }
        }

        /// <summary>
        ///     Gets the ID of the frame.  Frame 0 is the currently executing frame, 1 is the caller of the currently executing frame, etc...
        /// </summary>
        public int Id
        {
            get { return _frameId; }
        }

        public IList<NodeEvaluationResult> Locals { get; set; }

        public IList<NodeEvaluationResult> Parameters { get; set; }

        /// <summary>
        ///     Attempts to parse the given text.  Returns true if the text is a valid expression.  Returns false if the text is not
        ///     a valid expression and assigns the error messages produced to errorMsg.
        /// </summary>
        public bool TryParseText(string text, out string errorMsg)
        {
            errorMsg = null;
            return true;
        }

        /// <summary>
        ///     Executes the given text against this stack frame.
        /// </summary>
        /// <param name="text"></param>
        public Task<NodeEvaluationResult> EvaluateExpressionAsync(string text)
        {
            NodeEvaluationResult variable = Locals.FirstOrDefault(p => p.Name == text);
            if (variable != null)
            {
                return Task.FromResult(variable);
            }

            variable = Parameters.FirstOrDefault(p => p.Name == text);
            if (variable != null)
            {
                return Task.FromResult(variable);
            }

            return _debugger.EvaluateExpressionAsync(text, this);
        }

        /// <summary>
        ///     Sets the line number that this current frame is executing.  Returns true
        ///     if the line was successfully set or false if the line number cannot be changed
        ///     to this line.
        /// </summary>
        public bool SetLineNumber(int lineNo)
        {
            return true;
        }

        public async Task<bool> SetValueAsync(NodeEvaluationResult variable, string value)
        {
            NodeEvaluationResult result = await _debugger.SetVariableValueAsync(variable, value).ConfigureAwait(false);
            if (result == null)
            {
                return false;
            }

            variable.StringValue = result.StringValue;
            variable.TypeName = result.TypeName;
            variable.Type = result.Type;

            return true;
        }
    }
}