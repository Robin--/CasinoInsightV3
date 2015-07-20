using System.Collections.Generic;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Surge.Models.Cerebral;
using Surge.Models.System;

namespace Surge.Cerebral.EventSourcing
{
    /// <summary>
    /// Responsible to storing events in capped collections in mongo
    /// </summary>
    public class EventSource : IEventSource
    {
        private readonly IEventSourceConfig _config;
        private readonly MongoUrl _mongoUrl;
        private readonly MongoServer _server;
        private readonly MongoDatabase _database;
        private readonly CollectionOptionsBuilder _collectionOptionsBuilder;
        private long _collectionExists;

        public EventSource(IEventSourceConfig config)
        {
            _config = config;
            _mongoUrl = new MongoUrl(_config.ConnectionString);
            var settings = MongoServerSettings.FromUrl(_mongoUrl);
            settings.GuidRepresentation = GuidRepresentation.CSharpLegacy;            
            _server = new MongoServer(settings);            
            _database = _server.GetDatabase(_mongoUrl.DatabaseName);
            _collectionOptionsBuilder = new CollectionOptionsBuilder();
            _collectionOptionsBuilder.SetCapped(true);
            _collectionOptionsBuilder.SetMaxSize(_config.EventMax);
            _collectionOptionsBuilder.SetMaxDocuments(_config.EventMax);
            _collectionOptionsBuilder.SetAutoIndexId(true);
        }

        public int GetNumberOfPages(string collectionname)
        {
            //TODO: Needs to return the number of pages to retrieve
            return 0;
        }

        public List<TemporalEvent> RetrievePage(string collectionname, int page)
        {
            //TODO: Need to retrieve the events and remove then form the collection
            return new List<TemporalEvent>();
        }

        public void Append(string collectionname, TemporalEvent @event)
        {
            if (Interlocked.Read(ref _collectionExists) == 0)
            {
                if (!_database.CollectionExists(collectionname))
                {
                    if (_database.CreateCollection(collectionname, _collectionOptionsBuilder).Ok)
                    {
                        Interlocked.Increment(ref _collectionExists);
                    }
                }
                else
                {
                    Interlocked.Increment(ref _collectionExists);
                }
            }
            var collection = _database.GetCollection(collectionname);
            var bson = @event.ToBsonDocument();
            bson["_id"] = @event.Id;
            collection.Save(bson);
        }
    }
}
