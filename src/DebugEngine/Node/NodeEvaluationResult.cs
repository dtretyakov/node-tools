using System.Collections.Generic;
using System.Text.RegularExpressions;
using DebugEngine.Node.Debugger;

namespace DebugEngine.Node
{
    /// <summary>
    ///     Represents the result of an evaluation of an expression against a given stack frame.
    /// </summary>
    internal class NodeEvaluationResult
    {
        private readonly Regex _stringLengthExpression = new Regex(@"\.\.\. \(length: ([0-9]+)\)$", RegexOptions.Compiled);
        private IList<NodeEvaluationResult> _children;
        private int? _stringLength;
        private string _stringValue;

        /// <summary>
        ///     Creates a evaluation result for an expression which successfully returned a value.
        /// </summary>
        public NodeEvaluationResult(IDebuggerManager debugger, string stringValue, string hexValue,
                                    string typeName, string expression, string fullName,
                                    NodeExpressionType type, NodeStackFrame stackFrame, int id)
        {
            Debugger = debugger;
            Name = expression;
            FullName = fullName;
            StackFrame = stackFrame;
            _stringValue = stringValue;
            HexValue = hexValue;
            TypeName = typeName;
            Id = id;
            Type = type;
        }

        public NodeExpressionType Type { get; set; }

        public IDebuggerManager Debugger { get; private set; }

        /// <summary>
        ///     Gets the string representation of this evaluation or null if an exception was thrown.
        /// </summary>
        public string StringValue
        {
            get { return _stringValue; }
            set
            {
                if (_stringValue == value)
                {
                    return;
                }

                _stringValue = value;
                _stringLength = null;
            }
        }

        public int StringLength
        {
            get { return (int) (_stringLength ?? (_stringLength = GetStringLength(StringValue))); }
        }

        /// <summary>
        ///     Gets the string representation of this evaluation in hexadecimal or null if the hex value was not computable.
        /// </summary>
        public string HexValue { get; private set; }

        /// <summary>
        ///     Gets the type name of the result of this evaluation or null if an exception was thrown.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        ///     Gets the expression which was evaluated to return this object.
        /// </summary>
        public string Name { get; private set; }

        public string FullName { get; private set; }

        /// <summary>
        ///     Returns the stack frame in which this expression was evaluated.
        /// </summary>
        public NodeStackFrame StackFrame { get; private set; }

        public int Id { get; private set; }

        /// <summary>
        ///     Gets the list of children which this object contains.  The children can be either
        ///     members (x.foo, x.bar) or they can be indexes (x[0], x[1], etc...).  Calling this
        ///     causes the children to be determined by communicating with the debuggee.  These
        ///     objects can then later be evaluated.  The names returned here are in the form of
        ///     "foo" or "0" so they need additional work to append onto this expression.
        ///     Returns null if the object is not expandable.
        /// </summary>
        public IList<NodeEvaluationResult> Children
        {
            get { return _children ?? (_children = Debugger.GetChildrenAsync(this, StackFrame).Result); }
            set { _children = value; }
        }

        private int GetStringLength(string stringValue)
        {
            Match match = _stringLengthExpression.Match(stringValue);
            if (!match.Success)
            {
                return stringValue.Length;
            }

            return int.Parse(match.Groups[1].Value);
        }
    }
}