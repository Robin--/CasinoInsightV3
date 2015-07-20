using System.Threading;
using Akka.Actor;
using Newtonsoft.Json;
using NLog;
using Surge.Messages.Cerebral;

namespace Surge.Cerebral.Actors.Nucleus
{
    public class OutputPublishActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private long _mayoutput;

        public IStash Stash { get; set; }


        #region Message Handlers

        private void Handle_StopOutput()
        {
            _logger.Info("Stopping the output of events");
            Interlocked.Decrement(ref _mayoutput);
            BecomeStacked(Idle);
        }

        private void Handle_MayOutput()
        {
            _logger.Info("May output events");
            Interlocked.Increment(ref _mayoutput);
            UnbecomeStacked();
            BecomeStacked(ReadyToPublish);
        }

        private void Handle_QueryResult(StandingQueryResultMsg message)
        {
            _logger.Trace(Self.Path + " mayoutput is now " + _mayoutput);
            if (Interlocked.Read(ref _mayoutput) == 1)
            {
                //TODO: Publish to RabbitMQ Exchange
                var json = JsonConvert.SerializeObject(message.Payload);
                _logger.Trace("result -> {0} - {1} - {2}", message.TimeStamp, message.StandingQueryId, json);
            }
            _logger.Trace(Self.Path + " ignoring query results as output is disabled");
        }

        #endregion

        #region Protected Actor Overrides

        protected override void PreStart()
        {
            _logger.Info(Self.Path + " now starting...");
            BecomeStacked(NotReady);
        }

        protected override void PostStop()
        {
            _logger.Info(Self.Path + " stopping");
        }

        #endregion

        #region Behaviours

        private void NotReady()
        {
            _logger.Warn(Self.Path + " not ready to publish events");
            Stash.Stash();
        }

        private void ReadyToPublish()
        {
            _logger.Warn(Self.Path + " now ready to publish events");
            Receive<StopOutputEventMsg>(r => Handle_StopOutput());
            Receive<StandingQueryResultMsg>(r => Handle_QueryResult(r));
            Stash.UnstashAll();
        }

        private void Idle()
        {
            _logger.Warn(Self.Path + " now idle and will not publish any events until permitted");
            Receive<MayOutputEventMsg>(r => Handle_MayOutput());
        }

        #endregion
    }
}
