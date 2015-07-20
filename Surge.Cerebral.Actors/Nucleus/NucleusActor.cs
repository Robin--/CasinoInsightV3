using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Akka.Actor;
using Akka.Routing;
using com.espertech.esper.client;
using com.espertech.esper.compat.collections;
using com.espertech.esper.core.service;
using Castle.Core.Internal;
using Newtonsoft.Json;
using NLog;
using Surge.Cerebral.EventSourcing;
using Surge.Core.Exceptions;
using Surge.Messages.Cerebral;
using Surge.Models.Cerebral;
using EventSource = Surge.Cerebral.EventSourcing.EventSource;

namespace Surge.Cerebral.Actors.Nucleus
{
    /// <summary>
    /// This is the Complex Event Processor for Surge
    /// </summary>
    public class NucleusActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly INucleusConfig _configModel;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IEventSource _eventSource;
        private EPRuntime _runtime;
        private EPServiceProvider _service;
        private HashMap<string, EPStatement> _statements;
        private IActorRef _listeners;
        private IActorRef _publishers;
        private IActorRef _neuron;
        private long _mayoutput;

        public IStash Stash { get; set; }

        public NucleusActor(IActorRef neuron, INucleusConfig configModel)
        {
            _neuron = neuron;
            _configModel = configModel;
            _eventSource = new EventSource(configModel.EventSource);
        }

        #region Message Handlers

        private void Handle_Initialize(InitializeEngineMsg message)
        {
            _logger.Info(Self.Path + " initializing the Nesper Engine");
            var config = new Configuration();
            config.EngineDefaults.LoggingConfig.IsEnableExecutionDebug = true;
            config.EngineDefaults.LoggingConfig.IsEnableTimerDebug = true;
            config.EngineDefaults.LoggingConfig.IsEnableQueryPlan = true;
            _service = EPServiceProviderManager.GetDefaultProvider(config);
            _runtime = _service.EPRuntime;
            _statements = new HashMap<string, EPStatement>();

            _listeners =
                Context.ActorOf(
                    Props.Create(() => new QueryListenerActor())
                        .WithRouter(new SmallestMailboxPool(_configModel.NumberOfListeners)), "listeners");

            _publishers =
                Context.ActorOf(
                    Props.Create(() => new OutputPublishActor())
                        .WithRouter(new SmallestMailboxPool(_configModel.NumberOfPublishers)), "publishers");

            _logger.Info(Self.Path + " initialization is not complete");
            _neuron.Tell(new NucleusInitializedMsg());
            UnbecomeStacked();
            BecomeStacked(Initializing);
        }

        private void Handle_Catchup()
        {
            try
            {
                _logger.Warn(Self.Path + " now catching up with previous events");
                var pages = _eventSource.GetNumberOfPages(Self.Path.ToStringWithoutAddress());
                if (pages == 0)
                {
                    _logger.Info(Self.Path + "No catchup required");
                }
                _logger.Info(Self.Path + " has " + pages + " to catchup  each of " + _configModel.EventSource.PageSize);
                for (var page = 1; page <= pages; page++)
                {
                    _logger.Info(Self.Path + " catching up page" + page);
                    var events = _eventSource.RetrievePage(Self.Path.ToStringWithoutAddress(), page);
                    foreach (var @event in events)
                    {
                        _runtime.SendEvent(@event.Event.Payload, @event.Event.StreamName);
                    }
                }
                _logger.Info(Self.Path + " has now catched up all previous events");
                _neuron.Tell(new CatchupCompletedMsg());
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw new NucleusCatchupFailedException(ex.Message, ex);
            }

        }

        private void Handle_StreamEvent(StreamEventMsg message)
        {
            _eventSource.Append(Self.Path.ToStringWithoutAddress(), new TemporalEvent { CreatedOn = DateTime.Now, Event = message.Event, NucleusPath = Self.Path.ToStringWithAddress() });
            _runtime.SendEvent(message.Event.Payload, message.Event.StreamName);
        }

        private void Handle_StreamStash(StreamEventMsg message)
        {
            _logger.Trace(Self.Path + " stashing event " + message.Event.MessageId);
            Stash.Stash();
        }

        private void Handle_RegisterEventType(RegisterStreamMsg message)
        {
            _logger.Info(Self.Path + " registering eventtype " + message.Definition.StreamName);
            RegisterEventType(message.Definition.StreamName, message.Definition.StreamType.Map);
            _neuron.Tell(new StreamInitializedMsg(message.Definition.StreamName));
        }

        public void Handle_EventRegistrationComplete(StreamRegistrationCompleteMsg message)
        {
            _logger.Info(Self.Path + " all registration is now completed");
        }

        private void Handle_StartStandingQuery(StartStandingQueryMsg message)
        {
            try
            {
                if (_statements.ContainsKey(message.Query.StandingQueryId) &&
                    _statements[message.Query.StandingQueryId].IsStarted) return;
                var statement = _service.EPAdministrator.CreateEPL(message.Query.EplStatement,
                    message.Query.StandingQueryId,
                    message.Query);
                statement.AddEventHandlerWithReplay(SimpleEventHandler);
                _statements.Add(message.Query.StandingQueryId, statement);
                statement.Start();
                _logger.Info("StandingQuery {0} - {1} Started", message.Query.StandingQueryId, message.Query.Description);
                _neuron.Tell(new StandingQueryStartedMsg(message.Query.StandingQueryId));
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw new StandingQueryStartException(ex.Message, ex);
            }
        }

        private void Handle_StopStandingQuery(StopStandingQueryMsg message)
        {
            if (!_statements.ContainsKey(message.StandingQueryId) || !_statements[message.StandingQueryId].IsStarted)
                return;
            _statements[message.StandingQueryId].Stop();
            _logger.Info("StandingQuery {0} Stopped", message.StandingQueryId);
            _neuron.Tell(new StandingQueryStoppedMsg(message.StandingQueryId));
        }

        private void Handle_StopOutputEvent(StopOutputEventMsg message)
        {
            Interlocked.Decrement(ref _mayoutput);
            _publishers.Tell(message);
        }

        private void Handle_MayOutputEvent(MayOutputEventMsg message)
        {
            Interlocked.Increment(ref _mayoutput);
            _publishers.Tell(message);
        }

        private void Handle_ScenarioRegistered(ScenarioRegisteredMsg message)
        {
            _logger.Info(Self.Path + " scenario is now registered");
            UnbecomeStacked();
            BecomeStacked(Ready);
        }

        #endregion

        #region Protected Actor Overrides

        protected override void PreStart()
        {
            _logger.Info(Self.Path + " is now starting...");
            UnbecomeStacked();
            BecomeStacked(Idle);
            // _neuron.Tell(new StartScenarioRegistrationMsg(Self));
        }

        protected override void PostStop()
        {
            _logger.Info(Self.Path + " is now stopping all running StandingQueries");
            _statements.ForEach(s => s.Value.Stop());
        }

        #endregion

        #region Behaviours

        private void Idle()
        {
            _logger.Warn(Self.Path + " is now idle");
            Receive<InitializeEngineMsg>(r => Handle_Initialize(r));
        }

        /// <summary>
        /// Nesper Engine is initializing
        /// </summary>
        private void Initializing()
        {
            _logger.Warn(Self.Path + " is now initializing");
            Receive<RegisterStreamMsg>(r => Handle_RegisterEventType(r));
            Receive<StreamRegistrationCompleteMsg>(r => Handle_EventRegistrationComplete(r));
            Receive<StartStandingQueryMsg>(r => Handle_StartStandingQuery(r));
            Receive<StopStandingQueryMsg>(r => Handle_StopStandingQuery(r));
            Receive<ScenarioRegisteredMsg>(r => Handle_ScenarioRegistered(r));
            Receive<CatchupMsg>(r => Handle_Catchup());
            Receive<StartStandingQueryMsg>(r => Handle_StartStandingQuery(r));
            Receive<StopStandingQueryMsg>(r => Handle_StopStandingQuery(r));
            Receive<StreamEventMsg>(r => Handle_StreamStash(r));
            Receive<StopOutputEventMsg>(r => Handle_StopOutputEvent(r));
            Receive<MayOutputEventMsg>(r => Handle_MayOutputEvent(r));
        }

        private void Ready()
        {
            _logger.Warn(Self.Path + " is now ready");
            Receive<RegisterStreamMsg>(r => Handle_RegisterEventType(r));
            Receive<StreamRegistrationCompleteMsg>(r => Handle_EventRegistrationComplete(r));
            Receive<StartStandingQueryMsg>(r => Handle_StartStandingQuery(r));
            Receive<StopStandingQueryMsg>(r => Handle_StopStandingQuery(r));
            Receive<ScenarioRegisteredMsg>(r => Handle_ScenarioRegistered(r));
            Receive<CatchupMsg>(r => Handle_Catchup());
            Receive<StartStandingQueryMsg>(r => Handle_StartStandingQuery(r));
            Receive<StopStandingQueryMsg>(r => Handle_StopStandingQuery(r));
            Receive<StreamEventMsg>(r => Handle_StreamEvent(r));
            Receive<StopOutputEventMsg>(r => Handle_StopOutputEvent(r));
            Receive<MayOutputEventMsg>(r => Handle_MayOutputEvent(r));
            Stash.UnstashAll((e) => e.Message is StreamEventMsg);
        }

        #endregion

        #region Private Methods

        private void RegisterEventType(string eventType, IDictionary<string, object> map)
        {
            if (_service.EPAdministrator.Configuration.EventTypes.All(et => et.Name != eventType))
            {
                _service.EPAdministrator.Configuration.AddEventType(eventType, map);
            }
        }

        #endregion

        #region Private EventHandlers

        private void SimpleEventHandler(object o, UpdateEventArgs args)
        {
            if (args.NewEvents != null)
            {
                // ReSharper disable once CanBeReplacedWithTryCastAndCheckForNull
                if (o is StatementResultServiceImpl)
                {
                    var statement = (StatementResultServiceImpl)o;
                    args.NewEvents.ForEach(e =>
                    {
                        var json = JsonConvert.SerializeObject(e.Underlying);
                        //_log.Info("new -> {0} -> {1} -> eventhandler", e.EventType.Name, json);
                        _listeners.Tell(new StandingQueryResultMsg(statement.StatementName,
                            JsonConvert.DeserializeObject<Dictionary<string, object>>(json), DateTime.Now));
                    });
                }
            }
            if (args.OldEvents != null)
            {
                // ReSharper disable once CanBeReplacedWithTryCastAndCheckForNull
                if (o is StatementResultServiceImpl)
                {
                    var statement = (StatementResultServiceImpl)o;
                    args.NewEvents.ForEach(e =>
                    {
                        var json = JsonConvert.SerializeObject(e.Underlying);
                        //_log.Info("old -> {0} -> {1} -> eventhandler", e.EventType.Name, json);
                        _listeners.Tell(new StandingQueryResultMsg(statement.StatementName,
                            JsonConvert.DeserializeObject<Dictionary<string, object>>(json), DateTime.Now));
                    });
                    return;
                }
            }
            _logger.Info("No Events");
        }

        #endregion
    }
}
