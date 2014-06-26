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
        private const string RedisXesLogKey = "BluepathReportingOpenXes2";
        private static bool pauseLogging = false;
        public static string RedisHost = "localhost";
        public static bool WriteInfoToConsole = true;

        private Log()
        {
        }

        [Flags]
        public enum MessageType
        {
            // General
            Unspecified = 0,
            Exception = 1,
            Fatal = 1 << 1,
            Trace = 1 << 2,
            Info = 1 << 3,

            ServiceStarted = 1 << 6,
            ServiceStopped = 1 << 7,

            // User code execution
            UserCodeExecution = 1 << 8,
            UserCodeException = UserCodeExecution << 1,
            UserTaskStateChanged = UserCodeExecution << 2,
        }

        public enum Activity
        {
            Info,
            Custom,
            Service_is_ready,
            Callback_URI_set,
            Calling_initialize_on_remote_executor,
            Calling_remote_TryJoin,
            Distributed_thread_is_calling_Join_on_executor,
            Local_executor_caught_exception_in_user_code,
            Local_executor_created,
            Local_executor_initialized,
            Local_executor_started_running_user_code,
            Local_executor_finished_running_user_code,
            Local_executor_joins_thread_running_user_code,
            Local_executor_is_being_disposed,
            Local_executor_returns_processing_result,
            Queueing_user_task,
            Received_execute_callback,
            Remote_TryJoin_failed_because_remote_thread_is_still_running,
            Remote_TryJoin_timed_out,
            Remote_TryJoin_failed_with_exception,
            Starting_local_executor_with_callback,
            Starting_local_executor_without_callback,
            Sending_callback_with_result,
            Send_callback_failed
        }

        public static void ExceptionMessage(
            Exception exception,
            Activity activity,
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

            if (activity != Activity.Info || WriteInfoToConsole)
            {
                Debug.WriteLine(formattedMessage);
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(formattedMessage);
                Console.ForegroundColor = oldColor;
            }

            if (!logLocallyOnly)
            {
                Log.WriteToStorageList(activity, message);
            }
        }

        [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1001:CommasMustBeSpacedCorrectly", Justification = "Reviewed. Suppression is OK here."), SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines", Justification = "Reviewed. Suppression is OK here."), SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1111:ClosingParenthesisMustBeOnLineOfLastParameter", Justification = "Reviewed. Suppression is OK here."), SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1113:CommaMustBeOnSameLineAsPreviousParameter", Justification = "Reviewed. Suppression is OK here."), SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:ClosingParenthesisMustBeSpacedCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        [Conditional("DEBUG")]
        public static void TraceMessage(
            Activity activity,
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
            if (activity != Activity.Info || WriteInfoToConsole)
            {
                Debug.WriteLine(formattedMessage);
                Console.WriteLine(formattedMessage);
            }

            if (!logLocallyOnly)
            {
                Log.WriteToStorageList(activity, message);
            }
        }

        private static void WriteToStorageList(Activity activity, string message)
        {
            var resource = BluepathListener.NodeGuid.ToString().Substring(6);

            if (activity != Activity.Custom)
            {
                message = activity.ToString().Replace('_', ' ');
            }

            try
            {
                var logThread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            int retryCount = 5;
                            int retryNo = 0;
                            var storage = new RedisStorage(RedisHost);
                            var counter = new Bluepath.Storage.Structures.DistributedCounter(storage, string.Format("{0}_counter", RedisXesLogKey));
                            var tmpDateTime = DateTime.Now;
                            var dateTime = new DateTime(tmpDateTime.Year, tmpDateTime.Month, tmpDateTime.Day, tmpDateTime.Hour, tmpDateTime.Minute, tmpDateTime.Second, counter.GetAndIncrease() % 1000);
                            var list = new DistributedList<EventType>(storage, RedisXesLogKey);
                            while (retryNo < retryCount)
                            {
                                try
                                {
                                    list.Add(new EventType(message, resource, dateTime, EventType.Transition.Start));
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    ExceptionMessage(ex, Log.Activity.Info, string.Format("Error on saving log to Redis[{0}]. {1}", retryNo, ex.StackTrace), logLocallyOnly: true);
                                    retryNo++;
                                }
                            }

                            retryNo = 0;
                            while (retryNo < retryCount)
                            {
                                try
                                {
                                    list.Add(new EventType(message, resource, dateTime, EventType.Transition.Complete));
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    ExceptionMessage(ex, Log.Activity.Info, string.Format("Error on saving log to Redis[{0}]. {1}", retryNo, ex.StackTrace), logLocallyOnly: true);
                                    retryNo++;
                                }
                            }
                        }
                        catch
                        {

                        }
                    });
                logThread.IsBackground = false;
                logThread.Start();
            }
            catch (NotSupportedException ex)
            {
                ExceptionMessage(ex, Log.Activity.Info, "Exception on saving log to DistributedList", logLocallyOnly: true);
            }
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
                using (StreamReader reader = new StreamReader(fileName))
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

            if (clearListAfterSave)
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
