namespace Bluepath
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    public class Log
    {
        private Log()
        {
        }

        [Flags]
        public enum MessageType
        {
            // General
            Unspecified             = 0x00,
            Exception               = 0x01,
            Trace                   = 0x02,
            
            // User code execution
            UserCodeExecution       = 0x100,
            UserCodeException       = UserCodeExecution | 0x01,
            UserTaskStateChanged    = UserCodeExecution | 0x02,
        }

        public static void ExceptionMessage(
            Exception exception,
            string message = null,
            MessageType type = MessageType.Exception,
            IDictionary<string, string> keywords = null,
            [CallerMemberName] string memberName = "")
        {

        }

        [Conditional("DEBUG")]
        public static void TraceMessage(
            string message,
            MessageType type = MessageType.Trace,
            IDictionary<string, string> keywords = null
#if TRACE
          , [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
#endif
            )
        {

        }
    }

    public static class LogExtensions
    {
        public static Dictionary<string, string> AsLogKeywords(this Guid guid, string label)
        {
            var d = new Dictionary<string, string>();
            d.Add(label, guid.ToString());
            return d;
        }
    }
}
