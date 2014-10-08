slab-sinks
==========
This project contains Semantic Logging Application Block (SLAB) sinks to persist application events published to ETW and consumed by SLAB.

#Sinks
* Elasticsearch (Where else would you write events?)

##Elasticsearch Sink
A sink to write [Semantic Logging Application Block (SLAB)](http://slab.codeplex.com) to [Elasticsearch](http://www.elasticsearch.org).

###0 Create an event source
Create a class derived from [EventSource](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource(v=vs.110).aspx)

You could also use [EventSourceProxy](https://github.com/jonwagner/EventSourceProxy)

###1 Install NuGet
```
Install-Package EnterpriseLibrary.SemanticLogging.Elasticsearch
```
###2 Create a listener and enable events
```C#
var listener = new ObservableEventListener();

listener.EnableEvents(CommonEventSource.Log, EventLevel.LogAlways, ~EventKeywords.None);
```

###3 Send events to Elasticsearch
```
listener.LogToElasticsearch(
    Environment.MachineName,
    "http://localhost:9200",
    "slab",
    "mylogs");
```
