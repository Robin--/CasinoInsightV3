namespace Surge.Messages.Cerebral
{
    public class CheckScenarioRegisterationMsg
    {
        public CheckScenarioRegisterationMsg(RegistrationMode mode)
        {
            Mode = mode;
        }

        public enum RegistrationMode
        {
            EventTypes,
            StandingQueries
        }

        public RegistrationMode Mode { get; private set; }

    }
}
