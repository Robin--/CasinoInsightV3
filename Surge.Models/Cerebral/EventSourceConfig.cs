using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surge.Models.Cerebral
{
    public class EventSourceConfig : IEventSourceConfig
    {
        public int PageSize { get; set; }
        public int EventMax { get; set; }
        public string ConnectionString { get; set; }
    }
}
