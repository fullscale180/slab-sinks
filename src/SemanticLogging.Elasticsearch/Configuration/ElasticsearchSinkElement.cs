using System;
using System.Xml.Linq;
using FullScale180.SemanticLogging.Sinks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;

namespace FullScale180.SemanticLogging.Configuration
{
    internal class ElasticsearchSinkElement : ISinkElement
    {
        private readonly XName sinkName = XName.Get("elasticsearchSink", Constants.Namespace);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated with Guard class")]
        public bool CanCreateSink(XElement element)
        {
            Guard.ArgumentNotNull(element, "element");

            return element.Name == sinkName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated with Guard class")]
        public IObserver<EventEntry> CreateSink(XElement element)
        {
            Guard.ArgumentNotNull(element, "element");

            var bufferInterval = element.Attribute("bufferingIntervalInSeconds").ToTimeSpan();

            return new ElasticsearchSink(
                (string)element.Attribute("instanceName"),
                (string)element.Attribute("connectionString"),
                (string)element.Attribute("index") ?? "logstash",
                (string)element.Attribute("type") ?? "etw",
                (bool?)element.Attribute("flattenPayload") ?? true,
                bufferInterval == null ? Buffering.DefaultBufferingInterval : bufferInterval.Value,
                (int?)element.Attribute("bufferingCount") ?? Buffering.DefaultBufferingCount,
                (int?)element.Attribute("maxBufferSize") ?? Buffering.DefaultMaxBufferSize,
                element.Attribute("bufferingFlushAllTimeoutInSeconds").ToTimeSpan() ?? Constants.DefaultBufferingFlushAllTimeout,
                (string)element.Attribute("jsonGlobalContextExtension"));
        }
    }
}