using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Surge.Models.System;

namespace Surge.Models.Cerebral
{
    public class ScenarioDefinition
    {
        public string ScenarionName { get; set; }
        public List<StreamDefinition> Streams { get; set; }
        public List<StandingQuery> Queries { get; set; }
        public List<ActionDefinition> Actions { get; set; }
        public string Version { get; set; }
    }
}
