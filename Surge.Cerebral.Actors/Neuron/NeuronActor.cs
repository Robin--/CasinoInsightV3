using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Akka.Actor;
using Castle.Core.Internal;
using NLog;
using Surge.Cerebral.Actors.Nucleus;
using Surge.Core.Exceptions;
using Surge.Messages.Cerebral;
using Surge.Models.Cerebral;
using Surge.Models.System;

namespace Surge.Cerebral.Actors.Neuron
{
    internal enum QueryItemState
    {
        Unknown,
        Started,
        Stopped
    }

    internal enum EventTypeItemState
    {
        Unknown,
        Registered
    }

    internal class StreamDefinitionRegisterItem
    {
        public StreamDefinitionRegisterItem(StreamDefinition definition)
        {
            Definition = definition;
        }

        public EventTypeItemState State { get; set; }

        public StreamDefinition Definition { get; private set; }
    }

    internal class QueryRegisterItem
    {
        public QueryRegisterItem(StandingQuery query)
        {
            Query = query;
        }

        public QueryItemState State { get; set; }

        public StandingQuery Query { get; private set; }
    }

    public class NeuronActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly INucleusConfig _config;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        public IStash Stash { get; set; }
        private readonly ConcurrentDictionary<string, QueryRegisterItem> _queryRegister;
        private readonly ConcurrentDictionary<string, StreamDefinitionRegisterItem> _streamRegister;
        private IActorRef _nucleus;
        private ScenarioDefinition _scenarioDefinition;
        private ICancelable _streamScheduler;
        private ICancelable _queryScheduler;
        private readonly AllForOneStrategy _supervisiOneForOneStrategy;

        public NeuronActor(INucleusConfig config)
        {
            _config = config;
            _queryRegister = new ConcurrentDictionary<string, QueryRegisterItem>();
            _streamRegister = new ConcurrentDictionary<string, StreamDefinitionRegisterItem>();
            _supervisiOneForOneStrategy = new AllForOneStrategy(_config.RestartMax, TimeSpan.FromMilliseconds(_config.RestartDelay), (e) =>
            {
                if (e is NucleusCatchupFailedException)
                {
                    _logger.Warn(Self.Path + " nucleus failed to catchup, but will continue at risk or invalid state");
                    return Directive.Resume;
                }
                if (e is StandingQueryStartException)
                {
                    _logger.Fatal(Self.Path + "nuclues failed to start standingquery, will restart nuclues");
                    _streamScheduler.Cancel();
                    _queryScheduler.Cancel();
                    Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(_config.RestartDelay), Self, new StartScenarioRegistrationMsg(Self), Self);
                    UnbecomeStacked();
                    BecomeStacked(Waiting);
                    return Directive.Restart;
                }
                return Directive.Resume;
            });
        }

        #region Message Handlers

        private void Handle_StartScenarioRegistration(StartScenarioRegistrationMsg message)
        {
            _logger.Info(message.Nucleus.Path + " requesting the scenarion registration to start");
            if (_scenarioDefinition == null) return;
            _queryRegister.Clear();
            _streamRegister.Clear();
            Self.Tell(new RegisterScenarioMsg(_scenarioDefinition), Self);
        }

        private void Handle_ScenarionRegistration(RegisterScenarioMsg message)
        {
            _logger.Info(Self.Path + " registering Scenario " + message.Definition.ScenarionName + " version " + message.Definition.Version);
            _scenarioDefinition = message.Definition;
            message.Definition.Queries.ToDictionary(x => x.StandingQueryId).ForEach(x => _queryRegister.TryAdd(x.Key, new QueryRegisterItem(x.Value)));
            message.Definition.Streams.ToDictionary(x => x.StreamName).ForEach(x => _streamRegister.TryAdd(x.Key, new StreamDefinitionRegisterItem(x.Value)));
            UnbecomeStacked();
            BecomeStacked(Initializing);
            _nucleus.Tell(new InitializeEngineMsg());

        }

        private void Handle_EventTypeRegistered(StreamInitializedMsg message)
        {
            _logger.Info(Self.Path + " eventtype is now registered " + message.StreamName);
            var item = _streamRegister[message.StreamName];
            var newItem = new StreamDefinitionRegisterItem(item.Definition) { State = EventTypeItemState.Registered };
            _streamRegister.TryUpdate(message.StreamName, newItem, item);
        }

        private void Handle_NucleusInitialized(NucleusInitializedMsg message)
        {
            _logger.Info(Self.Path + " nucleus initialized");
            _logger.Info(Self.Path + " asking for eventtypes to be registered");
            _streamRegister.ForEach(e => _nucleus.Tell(new RegisterStreamMsg(e.Value.Definition)));
            _streamScheduler = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000), Self, new CheckScenarioRegisterationMsg(CheckScenarioRegisterationMsg.RegistrationMode.EventTypes), Self);
            _queryScheduler = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000), Self, new CheckScenarioRegisterationMsg(CheckScenarioRegisterationMsg.RegistrationMode.StandingQueries), Self);
        }

        private void Handle_StandingQueryStarted(StandingQueryStartedMsg message)
        {
            _logger.Info(Self.Path + " standingquery " + message.StandingQueryId + " started");
            var item = _queryRegister[message.StandingQueryId];
            var newItem = new QueryRegisterItem(item.Query) { State = QueryItemState.Started };
            _queryRegister.TryUpdate(message.StandingQueryId, newItem, item);
        }

        private void Handle_StandingQueryStopped(StandingQueryStoppedMsg message)
        {
            _logger.Info(Self.Path + " standingquery " + message.StandingQueryId + " stopped");
            var item = _queryRegister[message.StandingQueryId];
            var newItem = new QueryRegisterItem(item.Query) { State = QueryItemState.Stopped };
            _queryRegister.TryUpdate(message.StandingQueryId, newItem, item);
        }

        private void Handle_CheckScenarionRegistration(CheckScenarioRegisterationMsg message)
        {
            _logger.Info(Self.Path + " checking the Scenario Registration");
            switch (message.Mode)
            {
                case CheckScenarioRegisterationMsg.RegistrationMode.EventTypes:
                    {
                        _logger.Info(Self.Path + " checking the eventtypes");
                        if (_streamRegister.Values.All(e => e.State == EventTypeItemState.Registered))
                        {
                            _nucleus.Tell(new StreamRegistrationCompleteMsg());
                            _logger.Info(Self.Path + " asking for standingqueries to be started");
                            _queryRegister.ForEach(e => _nucleus.Tell(new StartStandingQueryMsg(e.Value.Query)));
                            _streamScheduler.Cancel();
                        }
                        break;
                    }
                case CheckScenarioRegisterationMsg.RegistrationMode.StandingQueries:
                    {
                        _logger.Info(Self.Path + " checking the standingqueries");
                        if (_queryRegister.Values.All(e => e.State == QueryItemState.Started))
                        {
                            _logger.Info(Self.Path + " all standingqueries are started in the register and will now request catchup to be performed");
                            _nucleus.Tell(new ScenarioRegisteredMsg());
                            _logger.Info(Self.Path + " asking that the nucleus perform the catchup process");
                            _nucleus.Tell(new CatchupMsg());
                            _queryScheduler.Cancel();
                        }
                        break;
                    }
            }
        }

        private void Handle_CatchupCompleted(CatchupCompletedMsg message)
        {
            _logger.Info(Self.Path + " catchup completed ready for processing");
            UnbecomeStacked();
            BecomeStacked(Ready);
        }

        private void Handle_StreamEvent(StreamEventMsg message)
        {
            _logger.Trace(Self.Path + " received event " + message.Event.MessageId);
            _nucleus.Tell(message);
        }

        private void Handle_StreamStash(StreamEventMsg message)
        {
            _logger.Trace(Self.Path + " stashed event " + message.Event.MessageId);
            Stash.Stash();
        }

        #endregion

        #region Protected Actor Overrides

        protected override void PreStart()
        {
            _logger.Info(Self.Path + " starting...");
            _nucleus =
                Context.ActorOf(
                    Props.Create(() => new NucleusActor(Self, _config)), "nucleus");
            UnbecomeStacked();
            BecomeStacked(Waiting);
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return _supervisiOneForOneStrategy;
        }

        #endregion


        #region Behaviours

        private void Waiting()
        {
            _logger.Warn(Self.Path + " now idle and ready for scenario registration [Waiting]");
            Receive<StartScenarioRegistrationMsg>(r => Handle_StartScenarioRegistration(r));
            Receive<RegisterScenarioMsg>(r => Handle_ScenarionRegistration(r));
        }

        private void Initializing()
        {
            _logger.Warn(Self.Path + " now initializing");
            Receive<NucleusInitializedMsg>(r => Handle_NucleusInitialized(r));
            Receive<StreamInitializedMsg>(r => Handle_EventTypeRegistered(r));
            Receive<CheckScenarioRegisterationMsg>(r => Handle_CheckScenarionRegistration(r));
            Receive<StandingQueryStartedMsg>(r => Handle_StandingQueryStarted(r));
            Receive<StandingQueryStoppedMsg>(r => Handle_StandingQueryStopped(r));
            Receive<CheckScenarioRegisterationMsg>(r => Handle_CheckScenarionRegistration(r));
            Receive<CatchupCompletedMsg>(r => Handle_CatchupCompleted(r));
            Receive<StreamEventMsg>(r => Handle_StreamStash(r));
        }

        private void Ready()
        {
            _logger.Warn(Self.Path + " now ready");
            Receive<NucleusInitializedMsg>(r => Handle_NucleusInitialized(r));
            Receive<StreamInitializedMsg>(r => Handle_EventTypeRegistered(r));
            Receive<CheckScenarioRegisterationMsg>(r => Handle_CheckScenarionRegistration(r));
            Receive<StandingQueryStartedMsg>(r => Handle_StandingQueryStarted(r));
            Receive<StandingQueryStoppedMsg>(r => Handle_StandingQueryStopped(r));
            Receive<CheckScenarioRegisterationMsg>(r => Handle_CheckScenarionRegistration(r));
            Receive<CatchupCompletedMsg>(r => Handle_CatchupCompleted(r));
            Receive<StreamEventMsg>(r => Handle_StreamEvent(r));
            Stash.UnstashAll((e)=>e.Message is StreamEventMsg);
        }

        #endregion
    }
}
