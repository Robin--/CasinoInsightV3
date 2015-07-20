using System;
using System.Collections.Generic;

namespace Surge.Models.Cerebral
{
    public class StreamEvent
    {
        public StreamEvent(string streamName, DateTime receivedOn, Dictionary<string, object> payload,
            Dictionary<string, object> header, string messageId, string consumerTag, string correlationId)
        {
            StreamName = streamName;
            ReceivedOn = receivedOn;
            Payload = payload;
            Header = header;
            MessageId = messageId;
            ConsumerTag = consumerTag;
            CorrelationId = correlationId;
        }

        /// <summary>
        ///     Name of the stream this will be targeting
        /// </summary>
        public string StreamName { get; private set; }

        /// <summary>
        ///     DateTime it was received
        /// </summary>
        public DateTime ReceivedOn { get; private set; }

        /// <summary>
        ///     Map containing the event payload
        /// </summary>
        public Dictionary<string, object> Payload { get; private set; }

        /// <summary>
        ///     Headers received with the payload
        /// </summary>
        public Dictionary<string, object> Header { get; private set; }

        public string MessageId { get; private set; }
        public string ConsumerTag { get; private set; }
        public string CorrelationId { get; private set; }
    }
}
