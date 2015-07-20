namespace Surge.Messages.Cerebral
{
    public class StreamInitializedMsg
    {
        public StreamInitializedMsg(string streamname)
        {
            StreamName = streamname;
        }

        public string StreamName { get; private set; }
    }
}
