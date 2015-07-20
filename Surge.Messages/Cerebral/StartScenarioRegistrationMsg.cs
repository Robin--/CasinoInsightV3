using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace Surge.Messages.Cerebral
{
    public class StartScenarioRegistrationMsg
    {
        public StartScenarioRegistrationMsg(IActorRef nucleus)
        {
            Nucleus = nucleus;
        }

        public IActorRef Nucleus { get; private set; }
    }
}
