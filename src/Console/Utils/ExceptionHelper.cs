using System;
using System.Reflection;
using Microsoft.VisualStudio.Shell;

namespace Console.Utils
{
    public static class ExceptionHelper
    {
        private const string LogEntrySource = "NuGet Package Manager";

        public static void WriteToActivityLog(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            exception = Unwrap(exception);

            ActivityLog.LogError(LogEntrySource, exception.Message + exception.StackTrace);
        }

        public static Exception Unwrap(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            if (exception.InnerException == null)
            {
                return exception;
            }

            // Always return the inner exception from a target invocation exception
            if (exception is AggregateException ||
                exception is TargetInvocationException)
            {
                return exception.GetBaseException();
            }

            return exception;
        }
    }
}