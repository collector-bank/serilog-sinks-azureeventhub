using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;

#if NET45
using Microsoft.ServiceBus.Messaging;
using System.Transactions;
#else
using Microsoft.Azure.EventHubs;
#endif

namespace Collector.Serilog.Sinks.AzureEventHub
{
    /// <summary>
    /// Writes log events to an Azure Event Hub in batches.
    /// </summary>
    public class AzureEventHubBatchingSink : PeriodicBatchingSink
    {
        const int EVENTHUB_MESSAGE_SIZE_LIMIT_IN_BYTES = 256000;
        const int EVENTHUB_HEADER_SIZE_IN_BYTES = 6000;

        readonly EventHubClient _eventHubClient;
        readonly ITextFormatter _formatter;
        private readonly string _applicationName;
        readonly Action<EventData, LogEvent> _eventDataAction;

        /// <summary>
        /// Construct a sink that saves log events to the specified EventHubClient.
        /// </summary>
        /// <param name="eventHubClient">The EventHubClient to use in this sink.</param>
        /// <param name="applicationName">The name of the application associated with the logs.</param>
        /// <param name="formatter">Provides formatting for outputting log data</param>
        /// <param name="batchSizeLimit">Default is 5 messages at a time</param>
        /// <param name="period">How often the batching should be done</param>
        /// <param name="eventDataAction">An optional action for setting extra properties on each EventData.</param>
        public AzureEventHubBatchingSink(
            EventHubClient eventHubClient,
            string applicationName,
            TimeSpan period,
            Action<EventData, LogEvent> eventDataAction = null,
            ITextFormatter formatter = null,
            int batchSizeLimit = 5
            )
            : base(batchSizeLimit, period)
        {
            formatter = formatter ?? new ScalarValueTypeSuffixJsonFormatter();

            if (batchSizeLimit < 1 || batchSizeLimit > 100)
            {
                throw new ArgumentException("batchSizeLimit must be between 1 and 100.");
            }

            _eventHubClient = eventHubClient;
            _formatter = formatter;
            _applicationName = applicationName;
            _eventDataAction = eventDataAction;
        }

        /// <summary>
        /// Emit a batch of log events, running to completion synchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        protected override void EmitBatch(IEnumerable<LogEvent> events)
        {
            var logEvents = events as LogEvent[] ?? events.ToArray();
            var batchedEvents = ConvertLogEventsToEventData(logEvents).ToList();

#if NET45
            var totalSizeOfAllEventsInBytes = batchedEvents.Sum(x => x.SerializedSizeInBytes);

            if (totalSizeOfAllEventsInBytes > GetAllowedMessageSize(batchedEvents.Count))
            {
                SendBatchOneEventAtATime(logEvents);
                return;
            }
#endif
            SendBatchAsOneChunk(batchedEvents);
        }

        private static int GetAllowedMessageSize(int numberOfEvents)
        {
            var headerSize = EVENTHUB_HEADER_SIZE_IN_BYTES * numberOfEvents;
            return EVENTHUB_MESSAGE_SIZE_LIMIT_IN_BYTES - headerSize;
        }

        private IEnumerable<EventData> ConvertLogEventsToEventData(IEnumerable<LogEvent> events)
        {
            var batchPartitionKey = Guid.NewGuid().ToString();

            foreach (var logEvent in events)
            {
                yield return ConvertLogEventToEventData(logEvent, batchPartitionKey);
            }
        }

        private EventData ConvertLogEventToEventData(LogEvent logEvent, string batchPartitionKey = null)
        {
            if (batchPartitionKey == null)
                batchPartitionKey = Guid.NewGuid().ToString();

            byte[] body;
            using (var render = new StringWriter())
            {
                _formatter.Format(logEvent, render);
                body = Encoding.UTF8.GetBytes(render.ToString());
            }

            var eventHubData = new EventData(body)
            {
#if NET45
                PartitionKey = batchPartitionKey
#endif
            };

            eventHubData = eventHubData.AsCompressed();
            if (!string.IsNullOrWhiteSpace(_applicationName) && !eventHubData.Properties.ContainsKey("Type"))
            {
                eventHubData.Properties.Add("Type", _applicationName);
            }

            _eventDataAction?.Invoke(eventHubData, logEvent);
            return eventHubData;
        }

        private void SendBatchOneEventAtATime(IEnumerable<LogEvent> events)
        {
            foreach (var logEvent in events)
            {
                var eventData = ConvertLogEventToEventData(logEvent);

#if NET45
                if (eventData.SerializedSizeInBytes > GetAllowedMessageSize(1))
                {
                    SelfLog.WriteLine("Message too large to send with eventhub");
                    continue;
                }
#endif

                try
                {
#if NET45
                    using (new TransactionScope(TransactionScopeOption.Suppress))
                    {
                        _eventHubClient.Send(eventData);
                    }
#endif
                    _eventHubClient.SendAsync(eventData).Wait();
                }
                catch
                {
                    try
                    {
                        Emit(logEvent);
                    }
                    catch
                    {
                        SelfLog.WriteLine("Could not Emit message");
                    }
                }
            }
        }

        private void SendBatchAsOneChunk(IEnumerable<EventData> batchedEvents)
        {
#if NET45
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                _eventHubClient.SendBatch(batchedEvents);
            }
#else
            _eventHubClient.SendAsync(batchedEvents).Wait();
#endif
        }
    }
}
