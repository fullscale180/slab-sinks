slab-sinks
==========
Semantic Logging Application Block Sinks

A sink to write [Semantic Logging Application Block (SLAB)](http://slab.codeplex.com) to [Elasticsearch](http://www.elasticsearch.org).

##Create an event source
Create a class derived from [EventSource](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource(v=vs.110).aspx)

You could also use [EventSourceProxy](https://github.com/jonwagner/EventSourceProxy)

##Install NuGet
```
Install-Package EnterpriseLibrary.SemanticLogging.Elasticsearch
```
##Create a listener and enable events
```C#
var listener = new ObservableEventListener();

listener.EnableEvents(CommonEventSource.Log, EventLevel.LogAlways, ~EventKeywords.None);
```

##Send events to Elasticsearch
```
listener.LogToElasticsearch(
    Environment.MachineName,
    "http://localhost:9200",
    "wiserair",
    "slab");
```
