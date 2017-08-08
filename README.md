# Collector Serilog Sinks AzureEventHub

[![Build status](https://ci.appveyor.com/api/projects/status/qhv5yfucxj456a8d/branch/master?svg=true)](https://ci.appveyor.com/project/CollectorHeimdal/serilog-sinks-azureeventhub/branch/master)

A Serilog sink that writes events to Azure EventHubs

## Usage

```csharp
var eventHubClient = EventHubClient.CreateFromConnectionString(eventHubConnectionString, "entityPath");
var logger = new LoggerConfiguration()
                .WriteTo.Sink(new AzureEventHubSink(
                    eventHubClient: eventHubClient,
                ...
                .CreateLogger();
```
