using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Newtonsoft.Json;
using NLog;
using Surge.Messages.Cerebral;

namespace Surge.Cerebral.Actors.Nucleus
{
    public class QueryListenerActor : ReceiveActor
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public QueryListenerActor()
        {
            Receive<StandingQueryResultMsg>(r => Handle_RecordResult(r));
        }

        #region Message Handlers

        public void Handle_RecordResult(StandingQueryResultMsg message)
        {
            var json = JsonConvert.SerializeObject(message.Payload);
            _logger.Trace("result -> {0} - {1} - {2}", message.TimeStamp, message.StandingQueryId, json);
        }

        #endregion

        #region Protected Actor Overrides

        protected override void PostStop()
        {
            _logger.Info(Self.Path + " stopping");
        }

        #endregion

    }
}
