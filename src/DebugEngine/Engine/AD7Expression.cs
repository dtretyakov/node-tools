using System;
using DebugEngine.Node;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace DebugEngine.Engine
{
    // This class represents a succesfully parsed expression to the debugger. 
    // It is returned as a result of a successful call to IDebugExpressionContext2.ParseText
    // It allows the debugger to obtain the values of an expression in the debuggee. 
    internal class AD7Expression : IDebugExpression2
    {
        private readonly string _expression;
        private readonly AD7StackFrame _frame;

        public AD7Expression(AD7StackFrame frame, string expression)
        {
            _frame = frame;
            _expression = expression;
        }

        // This method cancels asynchronous expression evaluation as started by a call to the IDebugExpression2::EvaluateAsync method.
        int IDebugExpression2.Abort()
        {
            throw new NotImplementedException();
        }

        // This method evaluates the expression asynchronously.
        // This method should return immediately after it has started the expression evaluation. 
        // When the expression is successfully evaluated, an IDebugExpressionEvaluationCompleteEvent2 
        // must be sent to the IDebugEventCallback2 event callback
        //
        // This is primarily used for the immediate window which this engine does not currently support.
        int IDebugExpression2.EvaluateAsync(enum_EVALFLAGS dwFlags, IDebugEventCallback2 pExprCallback)
        {
            EvaluateAsync();

            return VSConstants.S_OK;
        }

        private async void EvaluateAsync()
        {
            NodeEvaluationResult result;

            try
            {
                result = await _frame.StackFrame.EvaluateExpressionAsync(_expression);
            }
            catch (Exception)
            {
                return;
            }

            _frame.Engine.Send(
                new AD7ExpressionEvaluationCompleteEvent(this, new AD7Property(result)),
                AD7ExpressionEvaluationCompleteEvent.IID,
                _frame.Engine,
                _frame.Thread);
        }

        // This method evaluates the expression synchronously.
        int IDebugExpression2.EvaluateSync(enum_EVALFLAGS dwFlags, uint dwTimeout, IDebugEventCallback2 pExprCallback, out IDebugProperty2 ppResult)
        {
            NodeEvaluationResult result;
            ppResult = null;

            try
            {
                result = _frame.StackFrame.EvaluateExpressionAsync(_expression).Result;
            }
            catch (Exception)
            {
                return VSConstants.E_FAIL;
            }
            
            if (result == null)
            {
                return VSConstants.E_FAIL;
            }

            ppResult = new AD7Property(result);
            return VSConstants.S_OK;
        }
    }
}