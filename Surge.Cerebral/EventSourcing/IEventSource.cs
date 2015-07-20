using System.Collections.Generic;
using Surge.Models.Cerebral;
using Surge.Models.System;

namespace Surge.Cerebral.EventSourcing
{
    public interface IEventSource
    {
        /// <summary>
        /// Get the number of pages to collect from
        /// </summary>
        /// <param name="collectionname"></param>
        /// <returns></returns>
        int GetNumberOfPages(string collectionname);
        /// <summary>
        /// Retrieves the TemporalEvents from the colleciton
        /// </summary>
        /// <param name="collectionname">Collection in which the events are stored</param>
        /// <param name="page"></param>
        /// <returns></returns>
        List<TemporalEvent> RetrievePage(string collectionname, int page);
        /// <summary>
        /// Appends to the collection
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="event"></param>
        void Append(string collectionName, TemporalEvent @event);
    }
}
