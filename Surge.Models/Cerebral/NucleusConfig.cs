using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surge.Models.Cerebral
{
    public class NucleusConfig : INucleusConfig
    {
        public NucleusConfig()
        {
            EventSource = new EventSourceConfig();
            RestartDelay = 10000;// 10sec
            RestartMax = 100;
        }
        /// <summary>
        /// Number of Listeners in the pool
        /// </summary>
        public int NumberOfListeners { get; set; }
        /// <summary>
        /// Number of Publishers in the pool
        /// </summary>
        public int NumberOfPublishers { get; set; }
        /// <summary>
        /// Number of milliseconds to wait before the neuron decides to start the registration process again
        /// </summary>
        public int RestartDelay { get; set; }
        /// <summary>
        /// Maxmimu number of restarts
        /// </summary>
        public int RestartMax { get; set; }
        /// <summary>
        /// Configuration with regards to the EventSource
        /// </summary>
        public EventSourceConfig EventSource { get; set; }
    }
}
