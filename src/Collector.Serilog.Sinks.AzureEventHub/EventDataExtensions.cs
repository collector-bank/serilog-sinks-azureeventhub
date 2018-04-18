using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

#if NET45
using Microsoft.ServiceBus.Messaging;
#else
using Microsoft.Azure.EventHubs;
#endif

namespace Collector.Serilog.Sinks.AzureEventHub
{
    internal static class EventDataExtensions
    {
        private const string CONTENT_ENCODING = "Content-Encoding";
        private const string GZIP = "gzip";

        public static EventData AsCompressed(this EventData eventData)
        {
            var newStream = new MemoryStream();
            using (var bodyStream = GetStream(eventData))
            {
                using (var compressionStream = new GZipStream(newStream, CompressionMode.Compress, true))
                {
                    bodyStream.CopyTo(compressionStream);
                }

                newStream.Position = 0;
            }

            var compressedEventData = new EventData(newStream.ToArray());
#if NET45
            compressedEventData.PartitionKey = eventData.PartitionKey;
#endif

            foreach (var eventDataProperty in eventData.Properties)
                compressedEventData.Properties.Add(eventDataProperty);

            compressedEventData.Properties.Add(CONTENT_ENCODING, GZIP);
            return compressedEventData;
        }

        private static Stream GetStream(EventData eventData)
        {
#if NET45
            return eventData.GetBodyStream();
#else
            return new MemoryStream(eventData.Body.Array);
#endif
        }

        public static void Decompress(this EventData eventData)
        {
            if (!eventData.IsCompressed())
                throw new InvalidOperationException("The message is not compressed");

            var bodyStreamMember = GetBodyStreamMember();

            using (var currentStream = (Stream)bodyStreamMember.GetValue(eventData))
            {
                var newStream = new MemoryStream();
                using (var decompressionStream = new GZipStream(currentStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(newStream);
                }

                newStream.Position = 0;

                bodyStreamMember.SetValue(eventData, newStream);
            }
        }

        public static bool IsCompressed(this EventData eventData)
        {
            if (!eventData.Properties.ContainsKey(CONTENT_ENCODING))
                return false;

            if ((string)eventData.Properties[CONTENT_ENCODING] != GZIP)
                return false;

            return true;
        }

        private static FieldInfo GetBodyStreamMember()
        {
            return typeof(EventData).GetField("bodyStream", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}