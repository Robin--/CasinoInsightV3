namespace Surge.Messages.Cerebral
{
    public class StandingQueryStoppedMsg
    {
        public StandingQueryStoppedMsg(string standingQueryId)
        {
            StandingQueryId = standingQueryId;
        }

        public string StandingQueryId { get; private set; }
    }
}
