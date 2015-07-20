using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surge.Models.System
{
    public class EventType
    {
        public string EventTypeName { get; set; }
        public Dictionary<string, object> Map { get; set; }
    }
}
