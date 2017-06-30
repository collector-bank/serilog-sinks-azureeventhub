// Copyright 2014 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Diagnostics.CodeAnalysis;
using Serilog.Sinks.AzureEventHub;

#if NET45
using Microsoft.ServiceBus.Messaging;
#else 
using Microsoft.Azure.EventHubs;
#endif


using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.AzureEventHub() extension metho to <see cref="LoggerConfiguration"/>.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [Obsolete("Use Collector.Serilog.Sinks.AzureEventHub instead")]
    public static class LoggerConfigurationAzureEventHubExtensions
    {
        const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";

        /// <summary>
        /// A reasonable default for the number of events posted in each batch.
        /// </summary>
        private const int DefaultBatchPostingLimit = 50;

        /// <summary>
        /// A reasonable default time to wait between checking for event batches.
        /// </summary>
        private static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(2);

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="eventHubClient">The Event Hub to use to insert the log entries to.</param>
        /// <param name="applicationName">The name of the application associated with the logs.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp} [{Level}] {Message}{NewLine}{Exception}".</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="eventDataAction">An optional action for setting extra properties on each EventData.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        [Obsolete("Use Collector.Serilog.Sinks.AzureEventHub instead")]
        public static LoggerConfiguration AzureEventHub(
            this LoggerSinkConfiguration loggerConfiguration,
            EventHubClient eventHubClient,
            string applicationName,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null,
            Action<EventData, LogEvent> eventDataAction = null
            )
        {
            if (loggerConfiguration == null) 
                throw new ArgumentNullException(nameof(loggerConfiguration));
            if (eventHubClient == null)
                throw new ArgumentNullException(nameof(eventHubClient));
            if (outputTemplate == null) 
                throw new ArgumentNullException(nameof(outputTemplate));

            var formatter = new ScalarValueTypeSuffixJsonFormatter(renderMessage: true);

            ILogEventSink sink = writeInBatches ?
                new AzureEventHubBatchingSink(
                    eventHubClient,
                    applicationName,
                    eventDataAction,
                    period ?? DefaultPeriod,
                    formatter,
                    batchPostingLimit ?? DefaultBatchPostingLimit
                    ) as ILogEventSink :
                new AzureEventHubSink(
                    eventHubClient,
                    applicationName,
                    formatter,
                    eventDataAction
                    );

            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
        }

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="connectionString">The Event Hub connection string.</param>
        /// <param name="eventHubName">The Event Hub name.</param>
        /// <param name="applicationName">Enriches every log message with a Type (_type) with this name</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp} [{Level}] {Message}{NewLine}{Exception}".</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="eventDataAction">An optional action for setting extra properties on each EventData.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        [Obsolete("Use Collector.Serilog.Sinks.AzureEventHub instead")]
        public static LoggerConfiguration AzureEventHub(
            this LoggerSinkConfiguration loggerConfiguration,
            string connectionString,
            string eventHubName,
            string applicationName,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null,
            Action<EventData, LogEvent> eventDataAction = null
            )
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException("loggerConfiguration");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("connectionString");
            if (string.IsNullOrWhiteSpace(eventHubName))
                throw new ArgumentNullException("eventHubName");

            EventHubClient client = null;
#if NET45
            client = EventHubClient.CreateFromConnectionString(
                connectionString, eventHubName);
#else
            client = EventHubClient.CreateFromConnectionString(connectionString + ";EntityPath=" + eventHubName);
#endif
            return AzureEventHub(
                loggerConfiguration,
                client,
                applicationName,
                outputTemplate,
                formatProvider,
                restrictedToMinimumLevel,
                writeInBatches,
                period,
                batchPostingLimit,
                eventDataAction
                );
        }
    }
}
