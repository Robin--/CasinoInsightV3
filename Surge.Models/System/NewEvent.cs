using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surge.Models.System
{
    public class NewEvent
    {
        public NewEvent(string eventType, DateTime receivedOn, Dictionary<string, object> payload,
            Dictionary<string, object> header, string messageId, string consumerTag, string correlationId)
        {
            EventType = eventType;
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
        public string EventType { get; private set; }

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
