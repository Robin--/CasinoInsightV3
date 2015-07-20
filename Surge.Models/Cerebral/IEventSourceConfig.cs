namespace Surge.Models.Cerebral
{
    public interface IEventSourceConfig
    {
        /// <summary>
        /// The page size when events are retrieved
        /// </summary>
        int PageSize { get; set; }
        /// <summary>
        /// Maximum number of events stored in the capped collection
        /// </summary>
        int EventMax { get; set; }
        /// <summary>
        /// ConnectionString to the Mongo database
        /// </summary>
        string ConnectionString { get; set; }
    }
}