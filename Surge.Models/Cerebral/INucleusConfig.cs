namespace Surge.Models.Cerebral
{
    public interface INucleusConfig
    {
        /// <summary>
        /// Number of Listeners in the pool
        /// </summary>
        int NumberOfListeners { get; set; }

        /// <summary>
        /// Number of Publishers in the pool
        /// </summary>
        int NumberOfPublishers { get; set; }

        /// <summary>
        /// Number of milliseconds to wait before the neuron decides to start the registration process again
        /// </summary>
        int RestartDelay { get; set; }

        /// <summary>
        /// Maximum Number of restarts
        /// </summary>
        int RestartMax { get; set; }

        /// <summary>
        /// Configuration with regards to the EventSource
        /// </summary>
        EventSourceConfig EventSource { get; set; }
    }
}