
using NeuroSdk.Messages.API;

namespace NeuroSdk.Messages.Outgoing
{
    public sealed class Startup : OutgoingMessageBuilder
    {
        protected override string Command
        {
            get
            {
                return "startup";
            }
        }
        protected override object Data
        {
            get
            {
                return null;
            }
        }       
    }
}
