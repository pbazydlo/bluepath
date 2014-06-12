namespace Bluepath
{
    using Bluepath.DLINQ;
    using Bluepath.Reporting.OpenXes;
    using Bluepath.Services;
    using Bluepath.Storage.Redis;
    using Bluepath.Storage.Structures.Collections;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Linq;

    public class Log
    {
        private const string RedisHost = "localhost";
        private const string RedisXesLogKey = "BluepathReportingOpenXes2";
        private static bool pauseLogging = false;

        private Log()
        {
        }

        [Flags]
        public enum MessageType
        {
            // General
            Unspecified             = 0,
            Exception               = 1,
            Fatal                   = 1 << 1,
            Trace                   = 1 << 2,
            Info                    = 1 << 3,

            ServiceStarted          = 1 << 6,
            ServiceStopped          = 1 << 7,

            // User code execution
            UserCodeExecution       = 1 << 8,
            UserCodeException       = UserCodeExecution << 1,
            UserTaskStateChanged    = UserCodeExecution << 2,
        }

        public static void ExceptionMessage(
            Exception exception,
            string message = null,
            MessageType type = MessageType.Exception,
            IDictionary<string, string> keywords = null,
            bool logLocallyOnly = false,
            [CallerMemberName] string memberName = "")
        {
            // TODO: Implement logging
            if ((type & MessageType.Exception) != MessageType.Exception)
            {
                type |= MessageType.Exception;
            }

            var formattedMessage = string.Format(
                    "[LOG][{1}] {0} ({3}) {2}[caller: {4}]",
                    message,
                    type,
                    keywords.ToLogString(),
                    exception.Message,
                    memberName);
            // TODO aggregate exceptions

            if ((type & MessageType.UserCodeException) == MessageType.UserCodeException)
            {
                formattedMessage = string.Format(
                    "[LOG][{1}] {0} ({3}) {2}[caller: {4}]\n[details: {5}]\n---------------------------------------------------------------------------",
                    message,
                    type,
                    keywords.ToLogString(),
                    exception.Message,
                    memberName,
                    exception.ToString());
            }

            Debug.WriteLine(formattedMessage);
            Console.WriteLine(formattedMessage);

            if (!logLocallyOnly)
            {
                Log.WriteToStorageList(message);
            }
        }

        [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1001:CommasMustBeSpacedCorrectly", Justification = "Reviewed. Suppression is OK here."), SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines", Justification = "Reviewed. Suppression is OK here."), SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1111:ClosingParenthesisMustBeOnLineOfLastParameter", Justification = "Reviewed. Suppression is OK here."), SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1113:CommaMustBeOnSameLineAsPreviousParameter", Justification = "Reviewed. Suppression is OK here."), SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:ClosingParenthesisMustBeSpacedCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        [Conditional("DEBUG")]
        public static void TraceMessage(
            string message,
            MessageType type = MessageType.Trace,
            IDictionary<string, string> keywords = null,
            bool logLocallyOnly = false
#if TRACE
          , [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
#endif
            )
        {
            // TODO: Implement logging
            var traceInfo = default(string);
#if TRACE
            traceInfo = string.Format("[caller: {2} in {0}, line {1}]", Path.GetFileName(sourceFilePath), sourceLineNumber, memberName);
#endif
            var formattedMessage = string.Format("[LOG][{1}] {0} {2}{3}", message, type, keywords.ToLogString(), traceInfo);
            Debug.WriteLine(formattedMessage);
            Console.WriteLine(formattedMessage);

            if (!logLocallyOnly)
            {
                Log.WriteToStorageList(message);
            }
        }

        private static void WriteToStorageList(string activity) 
        {
            var resource = BluepathListener.NodeGuid.ToString().Substring(6);
            var storage = new RedisStorage(RedisHost);
            var list = new DistributedList<EventType>(storage, RedisXesLogKey);
            list.Add(new EventType(activity, resource, DateTime.Now, EventType.Transition.Start));
            list.Add(new EventType(activity, resource, DateTime.Now, EventType.Transition.Complete));
        }

        public static void SaveXes(string fileName, string caseName = null, bool clearListAfterSave = false)
        {
            var storage = new RedisStorage(RedisHost);
            var list = new DistributedList<EventType>(storage, RedisXesLogKey);

            var @case = new TraceType(caseName ?? Guid.NewGuid().ToString(), list);
            LogType log = null;
            if (!File.Exists(fileName))
            {
                log = LogType.Create(new[] { @case });
            }
            else
            {
                using(StreamReader reader = new StreamReader(fileName))
                {
                    log = LogType.Deserialize(reader.BaseStream);
                }

                var tracesList = log.trace.ToList();
                tracesList.Add(@case);
                log.trace = tracesList.ToArray();
            }
            
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileName))
            {
                log.Serialize(file.BaseStream);
            }

            if(clearListAfterSave)
            {
                list.Clear();
            }
        }

        public static void ClearXes()
        {
            var storage = new RedisStorage(RedisHost);
            var list = new DistributedList<EventType>(storage, RedisXesLogKey);
            list.Clear();
        }
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:StaticElementsMustAppearBeforeInstanceElements", Justification = "Reviewed. Suppression is OK here."), SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
    public static class LogExtensions
    {
        public static Dictionary<string, string> EidAsLogKeywords(this Guid guid)
        {
            return guid.AsLogKeywords("eid");
        }

        public static Dictionary<string, string> AsLogKeywords(this Guid guid, string label)
        {
            var d = new Dictionary<string, string>();
            d.Add(label, guid.ToString());
            return d;
        }

        public static string ToLogString(this IDictionary<string, string> keywords)
        {
            if (keywords == null || keywords.Count < 1)
            {
                return null;
            }

            var sb = new StringBuilder();
            sb.Append("[");

            foreach (var keyword in keywords)
            {
                if (sb.Length > 1)
                {
                    sb.Append("][");
                }

                sb.Append(keyword.Key);
                sb.Append(":");
                sb.Append(keyword.Value);
            }

            sb.Append("]");

            return sb.ToString();
        }
    }
}
