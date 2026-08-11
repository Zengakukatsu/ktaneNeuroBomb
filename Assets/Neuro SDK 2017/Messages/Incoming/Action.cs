
using System;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.API;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace NeuroSdk.Messages.Incoming
{
    // ReSharper disable once UnusedType.Global
    public sealed class Action : IncomingMessageHandler<Action.ParsedData>
    {
        public class ParsedData
        {
            public ParsedData(string id){
                Id = id;
            }

            public readonly string Id;
            public INeuroAction Action;
            public object Data;
        }

        public override bool CanHandle(string command){
            return command == "action";
        }

        protected override ExecutionResult Validate(string command, MessageJData messageData, out ParsedData parsedData)
        {
            if (messageData.Data == null)
            {
                parsedData = null;
                return ExecutionResult.VedalFailure(NeuroSdkStrings.ActionFailedNoData);
            }

            JToken idToken = messageData.Data["id"];
            string id = idToken == null ? null : idToken.Value<string>();

            if (id == null || id == "")
            {
                parsedData = null;
                return ExecutionResult.VedalFailure(NeuroSdkStrings.ActionFailedNoId);
            }

            parsedData = new ParsedData(id);

            try
            {
                JToken nameToken = messageData.Data["name"];
                string name = nameToken == null ? null : nameToken.Value<string>();

                JToken dataToken = messageData.Data["data"];
                string stringifiedData = dataToken == null ? null : dataToken.Value<string>();

                if (name == null || name == "") return ExecutionResult.VedalFailure(NeuroSdkStrings.ActionFailedNoName);

                INeuroAction registeredAction = NeuroActionHandler.GetRegistered(name);
                if (registeredAction == null)
                {
                    if (NeuroActionHandler.IsRecentlyUnregistered(name))
                    {
                        return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedUnregistered);
                    }
                    return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedUnknownAction.Format(name));
                }
                parsedData.Action = registeredAction;

                ActionJData jData;
                if (!ActionJData.TryParse(stringifiedData, out jData))
                {
                    return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedInvalidJson);
                }

                object parsedActionData;
                ExecutionResult actionValidationResult = registeredAction.Validate(jData, out parsedActionData);
                parsedData.Data = parsedActionData;

                return actionValidationResult;
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Exception caught while validating action {0}", id));
                Debug.LogError(e.ToString());

                return ExecutionResult.Failure(NeuroSdkStrings.ActionFailedCaughtException.Format(e.Message));
            }
        }

        protected override void ReportResult(ParsedData parsedData, ExecutionResult result)
        {
            if (parsedData == null)
            {
                Debug.LogError(string.Format("ReportResult received null data. It probably could not be parsed in the action. Received result: {0}",result.Message));
                return;
            }

            WebsocketConnection.Instance.Send(new ActionResult(parsedData.Id, result));
        }

        protected override void Execute(ParsedData parsedData)
        {
            parsedData.Action.Execute(parsedData.Data);
        }
    }
}
