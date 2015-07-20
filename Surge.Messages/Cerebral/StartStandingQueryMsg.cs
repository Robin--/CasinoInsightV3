using Surge.Models.Cerebral;

namespace Surge.Messages.Cerebral
{
    public class StartStandingQueryMsg
    {
        public StartStandingQueryMsg(StandingQuery query)
        {
            Query = query;
        }

        public StandingQuery Query { get; private set; }
    }
}
