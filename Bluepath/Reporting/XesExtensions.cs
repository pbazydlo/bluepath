namespace Bluepath.Reporting.OpenXes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    public class Constants
    {
        public const string Namespace = "http://www.xes-standard.org";
        public const string Creator = "Bluepath Reporting";
        public const string Library = "Bluepath.Reporting.OpenXes";
    }

    [XmlRoot(Namespace = Constants.Namespace, ElementName = "log")]
    public partial class LogType
    {
        public static LogType Create(IEnumerable<TraceType> cases)
        {
            var logType = new LogType();
            logType.xesversion = 1;

            //<extension name="Concept" prefix="concept" uri="http://www.xes-standard.org/concept.xesext"/>
            //<extension name="Lifecycle" prefix="lifecycle" uri="http://www.xes-standard.org/lifecycle.xesext"/>
            //<extension name="Time" prefix="time" uri="http://www.xes-standard.org/time.xesext"/>
            //<extension name="Organizational" prefix="org" uri="http://www.xes-standard.org/org.xesext"/>
            var extensions = new List<ExtensionType>();
            extensions.Add(new ExtensionType() { name = "Concept", prefix = "concept", uri = "http://www.xes-standard.org/concept.xesext" });
            extensions.Add(new ExtensionType() { name = "Lifecycle", prefix = "lifecycle", uri = "http://www.xes-standard.org/lifecycle.xesext" });
            extensions.Add(new ExtensionType() { name = "Time", prefix = "time", uri = "http://www.xes-standard.org/time.xesext" });
            extensions.Add(new ExtensionType() { name = "Organizational", prefix = "org", uri = "http://www.xes-standard.org/org.xesext" });
            logType.extension = extensions.ToArray();

            //<global scope="trace">
            //    <string key="concept:name" value="name"/>
            //</global>
            //<global scope="event">
            //    <string key="concept:name" value="name"/>
            //    <string key="lifecycle:transition" value="transition"/>
            //    <string key="org:resource" value="resource"/>
            //    <date key="time:timestamp" value="2014-06-01T17:44:47.804+02:00"/>
            //    <string key="Activity" value="string"/>
            //    <string key="Resource" value="string"/>
            //</global>
            var globals = new List<GlobalsType>();
            globals.Add(new GlobalsType() { scope = "trace", Items = new AttributableType[] { new AttributeStringType() { key = "concept:name", value = "name" } } });
            globals.Add(new GlobalsType()
            {
                scope = "event",
                Items = new AttributableType[]
                            {
                                new AttributeStringType() { key = "concept:name", value = "name" }, 
                                new AttributeStringType() { key = "lifecycle:transition", value = "transition" }, 
                                new AttributeStringType() { key = "org:resource", value = "resource" }, 
                                new AttributeDateType() { key = "time:timestamp", value = DateTime.Now }, 
                                new AttributeStringType() { key = "Activity", value = "string" }, 
                                new AttributeStringType() { key = "Resource", value = "string" }, 
                            }
            });
            logType.global = globals.ToArray();

            //<classifier name="Activity" keys="Activity"/>
            //<classifier name="Resource" keys="Resource"/>
            var classifiers = new List<ClassifierType>();
            classifiers.Add(new ClassifierType() { name = "Activity", keys = "Activity" });
            classifiers.Add(new ClassifierType() { name = "Resource", keys = "Resource" });
            logType.classifier = classifiers.ToArray();

            //<string key="lifecycle:model" value="standard"/>
            //<string key="creator" value="Fluxicon Disco"/>
            //<string key="library" value="Fluxicon Octane"/>
            var metadata = new AttributeType[]
                               {
                                   new AttributeStringType() { key = "lifecycle:model", value = "standard" },
                                   new AttributeStringType() { key = "creator", value = Constants.Creator },
                                   new AttributeStringType() { key = "library", value = Constants.Library }
                               };
            logType.Items = metadata;

            logType.trace = cases.ToArray();

            return logType;
        }

        public static LogType Deserialize(Stream stream)
        {
            var xmlSerializer = new XmlSerializer(typeof(LogType), Constants.Namespace);

            using (var xmlReader = XmlReader.Create(stream))
            {
                return xmlSerializer.Deserialize(xmlReader) as LogType;
            }
        }

        public string Serialize()
        {
            var xml = default(string);
            var xmlSerializer = new XmlSerializer(typeof(LogType), Constants.Namespace);

            using (var stream = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(stream))
                {
                    xmlSerializer.Serialize(xmlWriter, this);
                    var bytes = stream.ToArray();
                    xml = Encoding.UTF8.GetString(bytes);
                }
            }

            return xml;
        }

        public void Serialize(Stream stream)
        {
            var xml = default(string);
            var xmlSerializer = new XmlSerializer(typeof(LogType), Constants.Namespace);

            using (var xmlWriter = XmlWriter.Create(stream))
            {
                xmlSerializer.Serialize(xmlWriter, this);
            }
        }
    }

    public partial class TraceType
    {
        public TraceType()
        {
        }

        public TraceType(string caseIdentifier, IEnumerable<EventType> events)
        {
            //<trace>
            //    <string key="concept:name" value="Case1"/>
            //    <string key="creator" value="Fluxicon Disco"/>
            //    <event>
            //        <string key="concept:name" value="Start"/>
            //        <string key="lifecycle:transition" value="start"/>
            //        <string key="org:resource" value="Start"/>
            //        <date key="time:timestamp" value="2010-03-09T08:05:00.000+01:00"/>
            //        <string key="Activity" value="Start"/>
            //        <string key="Resource" value="Start"/>
            //    </event>

            this.Items = new AttributableType[]
                              {
                                  new AttributeStringType() { key = "concept:name", value = caseIdentifier },
                                  new AttributeStringType() { key = "creator", value = Constants.Creator },
                                  
                              };
            this.@event = events.ToArray();
        }
    }

    public partial class EventType
    {
        public EventType()
        {
        }

        public EventType(string activity, string resource, DateTime timestamp, Transition transition)
        {
            //    <event>
            //        <string key="concept:name" value="Start"/>
            //        <string key="lifecycle:transition" value="complete"/>
            //        <string key="org:resource" value="Start"/>
            //        <date key="time:timestamp" value="2010-03-09T08:05:00.000+01:00"/>
            //        <string key="Activity" value="Start"/>
            //        <string key="Resource" value="Start"/>
            //    </event>

            this.Items = new AttributableType[]
                             {
                                 new AttributeStringType() { key = "concept:name", value = activity }, 
                                 new AttributeStringType() { key = "lifecycle:transition", value = transition.ToString().ToLower() },
                                 new AttributeStringType() { key = "org:resource", value = resource },
                                 new AttributeDateType() { key = "time:timestamp", value = timestamp },
                                 new AttributeStringType() { key = "Activity", value = activity },
                                 new AttributeStringType() { key = "Resource", value = resource }
                             };
        }

        public enum Transition
        {
            /// <summary>The activity is scheduled for execution.</summary>
            Schedule,

            /// <summary>The activity is assigned to a resource for execution.</summary>
            Assign,

            /// <summary>Assignment has been revoked.</summary>
            Withdraw,

            /// <summary>Assignment after priror revocation.</summary>
            Reassign,

            /// <summary>Execution of the activity commences.</summary>
            Start,

            /// <summary>Execution is being paused.</summary>
            Suspend,

            /// <summary>Execution is restarted.</summary>
            Resume,

            /// <summary>The whole execution process is aborted for this case.</summary>
            Pi_Abort,

            /// <summary>Execution of the activity is aborted.</summary>
            Ate_Abort,

            /// <summary>Execution of the activity is completed.</summary>
            Complete,

            /// <summary>The activity has been skipped by the system.</summary>
            Autoskip,

            /// <summary>The activity has been skipped on purpose.</summary>
            Manualskip,

            /// <summary>Any lifecycle transition not captured by the above categories.</summary>
            Unknown,
        }
    }
}
