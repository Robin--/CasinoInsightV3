namespace Surge.Messages.Cerebral
{
    public class StopStandingQueryMsg
    {
        public StopStandingQueryMsg(string standingQueryId)
        {
            StandingQueryId = standingQueryId;
        }
        public string StandingQueryId { get; private set; }
    }
}
