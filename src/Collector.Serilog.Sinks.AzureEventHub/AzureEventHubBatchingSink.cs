using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;
using Microsoft.Azure.EventHubs;

namespace Collector.Serilog.Sinks.AzureEventHub
{
    /// <summary>
    /// Writes log events to an Azure Event Hub in batches.
    /// </summary>
    public class AzureEventHubBatchingSink : PeriodicBatchingSink
    {
        readonly EventHubClient _eventHubClient;
        readonly ITextFormatter _formatter;

        /// <summary>
        /// Construct a sink that saves log events to the specified EventHubClient.
        /// </summary>
        /// <param name="eventHubClient">The EventHubClient to use in this sink.</param>
        /// <param name="formatter">Provides formatting for outputting log data</param>
        /// <param name="batchSizeLimit">Default is 5 messages at a time</param>
        /// <param name="period">How often the batching should be done</param>
        public AzureEventHubBatchingSink(
            EventHubClient eventHubClient,
            TimeSpan period,
            ITextFormatter formatter = null,
            int batchSizeLimit = 5)
            : base(batchSizeLimit, period)
        {
            formatter = formatter ?? new ScalarValueTypeSuffixJsonFormatter();

            if (batchSizeLimit < 1 || batchSizeLimit > 100)
            {
                throw new ArgumentException("batchSizeLimit must be between 1 and 100.");
            }

            _eventHubClient = eventHubClient;
            _formatter = formatter;
        }

        /// <summary>
        /// Emit a batch of log events, running to completion synchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        protected override void EmitBatch(IEnumerable<LogEvent> events)
        {
            var logEvents = events as LogEvent[] ?? events.ToArray();
            var batchedEvents = ConvertLogEventsToEventData(logEvents).ToList();

            SendBatchAsOneChunk(batchedEvents);
        }

        private IEnumerable<EventData> ConvertLogEventsToEventData(IEnumerable<LogEvent> events)
        {
            foreach (var logEvent in events)
            {
                yield return ConvertLogEventToEventData(logEvent);
            }
        }

        private EventData ConvertLogEventToEventData(LogEvent logEvent)
        {
            byte[] body;
            using (var render = new StringWriter())
            {
                _formatter.Format(logEvent, render);
                body = Encoding.UTF8.GetBytes(render.ToString());
            }

            var eventHubData = new EventData(body)
            {

            };

            eventHubData = eventHubData.AsCompressed();

            eventHubData.Properties.Add("LogItemId", Guid.NewGuid().ToString());
            return eventHubData;
        }

        private void SendBatchAsOneChunk(IEnumerable<EventData> batchedEvents)
        {
            _eventHubClient.SendAsync(batchedEvents).Wait();
        }
    }
}
