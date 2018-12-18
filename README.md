# Collector Serilog Sinks AzureEventHub

[![Build status](https://ci.appveyor.com/api/projects/status/qhv5yfucxj456a8d/branch/master?svg=true)](https://ci.appveyor.com/project/CollectorHeimdal/serilog-sinks-azureeventhub/branch/master)

A Serilog sink that writes events to Azure EventHubs
## Update 3.0
We removed the non-batching version of the sink because Azure Eventhub has terrible performance when logging synchronously, and logging towards it should always be performed in asynchronous batches.

## Usage

```csharp
var eventHubClient = EventHubClient.CreateFromConnectionString(eventHubConnectionString, "entityPath");
var logger = new LoggerConfiguration()
                .WriteTo.Sink(new AzureEventHubBatchingSink(
                    eventHubClient: eventHubClient,
					period: TimeSpan.FromSeconds(15),
                ...
                .CreateLogger();
```

IMPORTANT!
Since this sink is asyncronous it is vital that you properly flush all log messages, using the Log.CloseAndFlush() method, and if you are using sub-loggers then you need to manually dispose each sub-logger.