using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ProjectSystem.Project
{
    internal sealed class NodeBuildableProjectConfig : BuildableProjectConfig
    {
        private readonly EventSinkCollection _callbacks = new EventSinkCollection();

        public NodeBuildableProjectConfig(ProjectConfig config) : base(config)
        {
        }

        public override int StartBuild(IVsOutputWindowPane pane, uint options)
        {
            NotifySubscribers();

            return VSConstants.S_OK;
        }

        private void NotifySubscribers()
        {
            int shouldContinue = 1;
            foreach (IVsBuildStatusCallback cb in _callbacks)
            {
                try
                {
                    ErrorHandler.ThrowOnFailure(cb.BuildBegin(ref shouldContinue));
                    ErrorHandler.ThrowOnFailure(cb.BuildEnd(1));
                }
                catch (Exception e)
                {
                    // If those who ask for status have bugs in their code it should not prevent the build/notification from happening
                    Debug.Fail(String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.BuildEventError, CultureInfo.CurrentUICulture), e.Message));
                }
            }
        }

        public override int AdviseBuildStatusCallback(IVsBuildStatusCallback callback, out uint cookie)
        {
            cookie = _callbacks.Add(callback);
            return VSConstants.S_OK;
        }

        public override int UnadviseBuildStatusCallback(uint cookie)
        {
            _callbacks.RemoveAt(cookie);
            return VSConstants.S_OK;
        }

        public override int StartClean(IVsOutputWindowPane pane, uint options)
        {
            NotifySubscribers();

            return VSConstants.S_OK;
        }

        public override int QueryStartBuild(uint options, int[] supported, int[] ready)
        {
            return VSConstants.S_OK;
        }

        public override int QueryStartClean(uint options, int[] supported, int[] ready)
        {
            return VSConstants.S_OK;
        }

        public override int QueryStartUpToDateCheck(uint options, int[] supported, int[] ready)
        {
            return VSConstants.S_OK;
        }

        public override int QueryStatus(out int done)
        {
            done = 1;
            return VSConstants.S_OK;
        }

        public override int Stop(int fsync)
        {
            return VSConstants.S_OK;
        }
    }
}