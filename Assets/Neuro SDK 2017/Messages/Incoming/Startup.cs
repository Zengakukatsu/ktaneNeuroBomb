
using NeuroSdk.Messages.API;
using NeuroSdk.Websocket;
using Newtonsoft.Json.Linq;

namespace NeuroSdk.Messages.Incoming
{
    // ReSharper disable once UnusedType.Global
    public sealed class Startup : IncomingMessageHandler<Startup.ParsedData>
    {
        public sealed class ParsedData
        {
            public ParsedData(string characterId, string displayName)
            {
                CharacterId = characterId;
                DisplayName = displayName;
            }

            public string CharacterId { get; }
            public string DisplayName { get; }
        }

        public override bool CanHandle(string command){
            return command == "startup";
        }

        protected override ExecutionResult Validate(string command, MessageJData messageData, out ParsedData parsedData)
        {
            parsedData = null;

            JObject root = messageData.Data as JObject;
            if (root == null) return ExecutionResult.Success();

            JObject session = root["session"] as JObject;
            if (session == null) return ExecutionResult.Success();

            JToken characterIdToken = session["characterId"];

            string characterId = characterIdToken != null ? characterIdToken.Value<string>(): "";
            if (characterId.Length == 0) return ExecutionResult.Success();

            JToken displayNameToken = session["displayName"];

            string displayName = displayNameToken != null ? displayNameToken.Value<string>() : characterId;
            parsedData = new ParsedData(characterId, displayName.Length == 0 ? characterId : displayName);
            return ExecutionResult.Success();
        }

        protected override void ReportResult(ParsedData parsedData, ExecutionResult result)
        {
        }

        protected override void Execute(ParsedData parsedData)
        {
            if (parsedData == null) return;
            WebsocketConnection.Instance.SetCharacterMetadata(
                new CharacterMetadata(parsedData.CharacterId, parsedData.DisplayName)
            );
        }
    }
}
