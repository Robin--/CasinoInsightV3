using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Surge.Models.System;

namespace Surge.Models.Cerebral
{
    public class StreamDefinition
    {
        public string StreamName { get; set; }
        public Dictionary<string, EventType> EventType { get; set; }
        public EventType StreamType { get; set; }
    }
}
