using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Surge.Models.Cerebral;

namespace Surge.Messages.Cerebral
{
    public class RegisterScenarioMsg
    {
        public RegisterScenarioMsg(ScenarioDefinition definition)
        {
            Definition = definition;
        }

        public ScenarioDefinition Definition { get; private set; }
    }
}
