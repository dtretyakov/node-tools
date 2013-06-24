using DebugEngine.Node;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace DebugEngine.Engine
{
    // This class represents a breakpoint that has been bound to a location in the debuggee. It is a child of the pending breakpoint
    // that creates it. Unless the pending breakpoint only has one bound breakpoint, each bound breakpoint is displayed as a child of the
    // pending breakpoint in the breakpoints window. Otherwise, only one is displayed.
    internal class AD7BoundBreakpoint : IDebugBoundBreakpoint2
    {
        private readonly NodeBreakpoint _breakpoint;
        private readonly AD7BreakpointResolution _breakpointResolution;
        private readonly AD7Engine _engine;
        private readonly AD7PendingBreakpoint _pendingBreakpoint;
        private bool _deleted;
        private bool _enabled;

        public AD7BoundBreakpoint(AD7Engine engine, NodeBreakpoint address, AD7PendingBreakpoint pendingBreakpoint, AD7BreakpointResolution breakpointResolution)
        {
            _engine = engine;
            _breakpoint = address;
            _pendingBreakpoint = pendingBreakpoint;
            _breakpointResolution = breakpointResolution;
            _enabled = true;
            _deleted = false;
        }

        // Called when the breakpoint is being deleted by the user.
        int IDebugBoundBreakpoint2.Delete()
        {
            if (!_deleted)
            {
                _deleted = true;
                _breakpoint.Remove();
                _pendingBreakpoint.OnBoundBreakpointDeleted(this);
                _engine.BreakpointManager.RemoveBoundBreakpoint(_breakpoint);
            }

            return VSConstants.S_OK;
        }

        // Called by the debugger UI when the user is enabling or disabling a breakpoint.
        int IDebugBoundBreakpoint2.Enable(int fEnable)
        {
            bool enabled = fEnable != 0;
            _breakpoint.Enabled = enabled;

            _enabled = enabled;
            return VSConstants.S_OK;
        }

        // Return the breakpoint resolution which describes how the breakpoint bound in the debuggee.
        int IDebugBoundBreakpoint2.GetBreakpointResolution(out IDebugBreakpointResolution2 ppBpResolution)
        {
            ppBpResolution = _breakpointResolution;
            return VSConstants.S_OK;
        }

        // Return the pending breakpoint for this bound breakpoint.
        int IDebugBoundBreakpoint2.GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBreakpoint)
        {
            ppPendingBreakpoint = _pendingBreakpoint;
            return VSConstants.S_OK;
        }

        // 
        int IDebugBoundBreakpoint2.GetState(enum_BP_STATE[] pState)
        {
            pState[0] = 0;

            if (_deleted)
            {
                pState[0] = enum_BP_STATE.BPS_DELETED;
            }
            else if (_enabled)
            {
                pState[0] = enum_BP_STATE.BPS_ENABLED;
            }
            else if (!_enabled)
            {
                pState[0] = enum_BP_STATE.BPS_DISABLED;
            }

            return VSConstants.S_OK;
        }

        // The sample engine does not support hit counts on breakpoints. A real-world debugger will want to keep track 
        // of how many times a particular bound breakpoint has been hit and return it here.
        int IDebugBoundBreakpoint2.GetHitCount(out uint pdwHitCount)
        {
            pdwHitCount = 0;
            return VSConstants.E_NOTIMPL;
        }

        // The sample engine does not support conditions on breakpoints.
        // A real-world debugger will use this to specify when a breakpoint will be hit
        // and when it should be ignored.
        int IDebugBoundBreakpoint2.SetCondition(BP_CONDITION bpCondition)
        {
            _breakpoint.Condition = bpCondition.bstrCondition;
            return VSConstants.S_OK;
        }

        // The sample engine does not support hit counts on breakpoints. A real-world debugger will want to keep track 
        // of how many times a particular bound breakpoint has been hit. The debugger calls SetHitCount when the user 
        // resets a breakpoint's hit count.
        int IDebugBoundBreakpoint2.SetHitCount(uint dwHitCount)
        {
            return VSConstants.E_NOTIMPL;
        }

        // The sample engine does not support pass counts on breakpoints.
        // This is used to specify the breakpoint hit count condition.
        int IDebugBoundBreakpoint2.SetPassCount(BP_PASSCOUNT bpPassCount)
        {
            return VSConstants.E_NOTIMPL;
        }
    }
}