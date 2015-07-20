using System;
using System.Collections.Generic;
using System.IO;
using Akka.Actor;
using Newtonsoft.Json;
using NUnit.Framework;
using Sharpen;
using Surge.Cerebral.Actors.Neuron;
using Surge.Messages.Cerebral;
using Surge.Models.Cerebral;
using Surge.Models.System;

namespace Surge.Cerebral.Actors.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        private readonly ScenarioDefinition _scenarioDefinition = new ScenarioDefinition()
        {
            Actions = new AList<ActionDefinition> { new ActionDefinition { } },
            Streams =
                new AList<StreamDefinition>
                {
                    new StreamDefinition
                    {
                        StreamType = 
                            new EventType()
                            {
                                EventTypeName = "wager",
                                Map =
                                    new Dictionary<string, object>
                                    {
                                        {"userid", "long"},
                                        {"accountnumber", "string"},
                                        {"transactiondate", "datetime"},
                                        {"amount", "int"},
                                        {"eventtype", "string"},
                                        {"eventtime", "datetime"}
                                    }
                            },
                        StreamName = "wagerstream"          
                    }
                },
            Queries =
                new AList<StandingQuery>
                {
                    new StandingQuery
                    {
                        Description = "Wagers greater than 50 in the last 10 sec",
                        EplStatement = "select * from wagerstream(amount>50).win:time(10 sec)",
                        StandingQueryId = "06B94034-CA07-4E35-A80D-6AAC2B12E70D"
                    }
                },
            ScenarionName = "Wager over 500",
            Version = "1"
        };

        private readonly NucleusConfig _nucleusConfig = new NucleusConfig()
        {
            EventSource = new EventSourceConfig { PageSize = 1000, ConnectionString = "mongodb://localhost:27017/surge_cerebral_eventsource", EventMax = 10000 },
            NumberOfListeners = 10,
            NumberOfPublishers = 10
        };


        [Test]
        public void register_scenariondefinition_with_neuron()
        {
            using (var system = ActorSystem.Create("Cerebral"))
            {
                var neuron = system.ActorOf(Props.Create(() => new NeuronActor(_nucleusConfig)), "neuron");
                neuron.Tell(new RegisterScenarioMsg(_scenarioDefinition));
                system.AwaitTermination();
            }
        }

        [TestCase(1000)]
        [TestCase(10)]
        public void publish_wager_events_through_standing_query(int iterations)
        {
            using (var system = ActorSystem.Create("Cerebral"))
            {
                var neuron = system.ActorOf(Props.Create(() => new NeuronActor(_nucleusConfig)), "neuron");
                neuron.Tell(new RegisterScenarioMsg(_scenarioDefinition));
                for (var i = 0; i < iterations; i++)
                {
                    neuron.Tell(
                        new StreamEventMsg(new StreamEvent("wagerstream", DateTime.Now, new Dictionary<string, object>
                        {
                            {"userid", new Random().Next(200000)},
                            {"accountnumber", "acc" + new Random().Next(200000)},
                            {"transactiondate", DateTime.Now},
                            {"amount", new Random().Next(10000)},
                            {"eventtype", "wager"},
                            {"eventtime", DateTime.Now}
                        }, new Dictionary<string, object>(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                            Guid.NewGuid().ToString())));
                }
                system.AwaitTermination(TimeSpan.FromSeconds(60));
            }
        }

        [Test]
        public void generate_sample_scenariondefition()
        {
            using (var writer = new StreamWriter("./Tests/json/sample.streamdefinition.json"))
            {
                var json = JsonConvert.SerializeObject(_scenarioDefinition);
                writer.Write(json);
            }
        }
    }
}
