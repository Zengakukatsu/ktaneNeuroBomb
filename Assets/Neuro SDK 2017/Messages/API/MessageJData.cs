
using NeuroSdk.Json;
using Newtonsoft.Json.Linq;

namespace NeuroSdk.Messages.API
{
    public struct MessageJData : IJTokenWrapper
    {   
        public JToken Data { get; private set;}

        public MessageJData(JToken data)
        {
            Data = data;
        }
    };
}
