namespace Surge.Messages.Cerebral
{
    public class StandingQueryStartedMsg
    {
        public StandingQueryStartedMsg(string standingQueryId)
        {
            StandingQueryId = standingQueryId;
        }

        public string StandingQueryId { get; private set; }
    }
}
