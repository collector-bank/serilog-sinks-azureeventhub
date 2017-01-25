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
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Microsoft.ServiceBus.Messaging;

namespace Serilog
{
    internal static class EventDataExtensions
    {
        private const string CONTENT_ENCODING = "Content-Encoding";
        private const string GZIP = "gzip";

        public static EventData AsCompressed(this EventData eventData)
        {
            var newStream = new MemoryStream();
            using (var bodyStream = eventData.GetBodyStream())
            {
                using (var compressionStream = new GZipStream(newStream, CompressionMode.Compress, true))
                {
                    bodyStream.CopyTo(compressionStream);
                }

                newStream.Position = 0;
            }

            var compressedEventData = new EventData(newStream)
            {
                PartitionKey = Guid.NewGuid().ToString()
            };

            foreach (var eventDataProperty in eventData.Properties)
                compressedEventData.Properties.Add(eventDataProperty);

            compressedEventData.Properties.Add(CONTENT_ENCODING, GZIP);
            return compressedEventData;
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