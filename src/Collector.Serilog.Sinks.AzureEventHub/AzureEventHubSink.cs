using System;
using System.IO;
using System.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

#if NET45
using Microsoft.ServiceBus.Messaging;
using System.Transactions;
#else
using Microsoft.Azure.EventHubs;
#endif

namespace Collector.Serilog.Sinks.AzureEventHub
{

    /// <summary>
    /// Writes log events to an Azure Event Hub.
    /// </summary>
    public class AzureEventHubSink : ILogEventSink
    {
        private readonly EventHubClient _eventHubClient;
        private readonly ITextFormatter _formatter;
#if NET45
        private readonly int? _compressionTreshold;
#endif

        /// <summary>
        /// Construct a sink that saves log events to the specified EventHubClient.
        /// </summary>
        /// <param name="eventHubClient">The EventHubClient to use in this sink.</param>
        /// <param name="formatter">Provides formatting for outputting log data</param>
#if NET45
        /// <param name="compressionTreshold">An optional setting to configure when to start compressing messages with gzip. Specified in bytes</param>
#endif
        public AzureEventHubSink(
            EventHubClient eventHubClient,
            ITextFormatter formatter = null
#if NET45
            ,int? compressionTreshold = null
#endif
            )
        {
            _eventHubClient = eventHubClient;
            _formatter = formatter ?? new ScalarValueTypeSuffixJsonFormatter();
#if NET45
            _compressionTreshold = compressionTreshold;
#endif
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            byte[] body;
            
            using (var render = new StringWriter())
            {
                _formatter.Format(logEvent, render);
                body = Encoding.UTF8.GetBytes(render.ToString());
            }
            
            var eventHubData = new EventData(body)
            {
#if NET45
                PartitionKey = Guid.NewGuid().ToString()
#endif
            };
            eventHubData.Properties.Add("LogItemId", Guid.NewGuid().ToString());
#if NET45
            if (_compressionTreshold != null && eventHubData.SerializedSizeInBytes > _compressionTreshold)
                eventHubData = eventHubData.AsCompressed();

            using (var transaction = new TransactionScope(TransactionScopeOption.Suppress))
            {
                _eventHubClient.Send(eventHubData);
            }
#else
            _eventHubClient.SendAsync(eventHubData).Wait();
#endif
        }
    }
}
