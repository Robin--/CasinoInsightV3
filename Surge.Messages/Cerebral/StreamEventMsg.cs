using Surge.Models.Cerebral;
using Surge.Models.System;

namespace Surge.Messages.Cerebral
{
    public class StreamEventMsg
    {
        public StreamEventMsg(StreamEvent @event)
        {
            Event = @event;
        }

        public StreamEvent Event { get; private set; }
    }
}
