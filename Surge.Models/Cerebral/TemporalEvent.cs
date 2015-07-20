using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Surge.Models.System;

namespace Surge.Models.Cerebral
{
    public class TemporalEvent
    {
        public Guid Id { get; set; }
        public string NucleusPath { get; set; }
        public DateTime CreatedOn { get; set; }
        public StreamEvent Event { get; set; }
    }
}
