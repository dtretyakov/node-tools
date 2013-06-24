using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DebugEngine.Node.Debugger
{
    internal interface IDebuggerManager
    {
        /// <summary>
        ///     Gets a value indicating whether process is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        ///     Gets a value indicating whether process stopped for exception.
        /// </summary>
        bool IsException { get; }

        event EventHandler<ThreadEventArgs> StepComplete;
        event EventHandler<ThreadEventArgs> AsyncBreakComplete;
        event EventHandler<ModuleLoadedEventArgs> ModuleLoaded;
        event EventHandler<ExceptionRaisedEventArgs> ExceptionRaised;
        event EventHandler<BreakpointHitEventArgs> BreakpointHit;
        event EventHandler<BreakpointEventArgs> BreakpointBindSucceeded;
        event EventHandler<BreakpointEventArgs> BreakpointBindFailed;

        Task InitializeAsync();

        /// <summary>
        ///     Adds a breakpoint.
        /// </summary>
        /// <param name="breakpoint">Break point.</param>
        Task AddBreakpointAsync(NodeBreakpoint breakpoint);

        /// <summary>
        ///     Changes a break point.
        /// </summary>
        /// <param name="breakpoint">Break point.</param>
        Task ChangeBreakpointAsync(NodeBreakpoint breakpoint);

        /// <summary>
        ///     Removes a break point.
        /// </summary>
        /// <param name="breakpoint">Break point.</param>
        Task RemoveBreakpointAsync(NodeBreakpoint breakpoint);

        /// <summary>
        ///     Configures exception handling policy.
        /// </summary>
        /// <param name="throwOnAllExceptions">Defines whether throw on all exceptions.</param>
        Task SetExceptionHandlingAsync(bool throwOnAllExceptions);

        /// <summary>
        ///     Gets a collection of descentants for a variable.
        /// </summary>
        /// <param name="variable">Variable.</param>
        /// <param name="stackFrame">Stack frame.</param>
        /// <returns>Collection of descendants.</returns>
        Task<IList<NodeEvaluationResult>> GetChildrenAsync(NodeEvaluationResult variable, NodeStackFrame stackFrame);

        /// <summary>
        ///     Gets a collection of stack frames with variables.
        /// </summary>
        /// <returns>Collection of stack frames.</returns>
        Task<IList<NodeStackFrame>> GetStackFramesAsync();

        /// <summary>
        ///     Evaluates an expression.
        /// </summary>
        /// <param name="text">Expression text.</param>
        /// <param name="stackFrame">Stack frame.</param>
        /// <returns>Result.</returns>
        Task<NodeEvaluationResult> EvaluateExpressionAsync(string text, NodeStackFrame stackFrame);

        /// <summary>
        ///     Evaluates a variable.
        /// </summary>
        /// <param name="variable">Variable.</param>
        /// <returns>Result.</returns>
        Task<NodeEvaluationResult> EvaluateVariableAsync(NodeEvaluationResult variable);

        /// <summary>
        ///     Sets a new variable value.
        /// </summary>
        /// <param name="variable">Variable.</param>
        /// <param name="value">New value.</param>
        /// <returns>Result.</returns>
        Task<NodeEvaluationResult> SetVariableValueAsync(NodeEvaluationResult variable, string value);

        /// <summary>
        ///     Continue program execution.
        /// </summary>
        Task Continue();

        /// <summary>
        ///     Steps execution into statement.
        /// </summary>
        Task StepInto();

        /// <summary>
        ///     Steps execution over statement.
        /// </summary>
        Task StepOver();

        /// <summary>
        ///     Steps execution out statement.
        /// </summary>
        Task StepOut();

        /// <summary>
        ///     Breaks execution.
        /// </summary>
        Task Break();

        /// <summary>
        ///     Terminates debugging session.
        /// </summary>
        Task Terminate();
    }
}