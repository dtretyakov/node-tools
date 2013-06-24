using System.Collections.Generic;
using DebugEngine.Engine;
using DebugEngine.Node;
using Microsoft.VisualStudio.Debugger.Interop;

namespace DebugEngine.Management
{
    // This class manages breakpoints for the engine. 
    internal class BreakpointManager
    {
        private readonly Dictionary<NodeBreakpoint, AD7BoundBreakpoint> _breakpointMap = new Dictionary<NodeBreakpoint, AD7BoundBreakpoint>();
        private readonly AD7Engine _mEngine;
        private readonly List<AD7PendingBreakpoint> _mPendingBreakpoints;

        public BreakpointManager(AD7Engine engine)
        {
            _mEngine = engine;
            _mPendingBreakpoints = new List<AD7PendingBreakpoint>();
        }

        // A helper method used to construct a new pending breakpoint.
        public void CreatePendingBreakpoint(IDebugBreakpointRequest2 pBpRequest, out IDebugPendingBreakpoint2 ppPendingBp)
        {
            var pendingBreakpoint = new AD7PendingBreakpoint(pBpRequest, _mEngine, this);
            ppPendingBp = pendingBreakpoint;
            _mPendingBreakpoints.Add(pendingBreakpoint);
        }

        // Called from the engine's detach method to remove the debugger's breakpoint instructions.
        public void ClearBoundBreakpoints()
        {
            foreach (AD7PendingBreakpoint pendingBreakpoint in _mPendingBreakpoints)
            {
                pendingBreakpoint.ClearBoundBreakpoints();
            }
        }

        public void AddBoundBreakpoint(NodeBreakpoint breakpoint, AD7BoundBreakpoint boundBreakpoint)
        {
            _breakpointMap[breakpoint] = boundBreakpoint;
        }

        public void RemoveBoundBreakpoint(NodeBreakpoint breakpoint)
        {
            _breakpointMap.Remove(breakpoint);
        }

        public AD7BoundBreakpoint GetBreakpoint(NodeBreakpoint breakpoint)
        {
            return _breakpointMap[breakpoint];
        }
    }
}