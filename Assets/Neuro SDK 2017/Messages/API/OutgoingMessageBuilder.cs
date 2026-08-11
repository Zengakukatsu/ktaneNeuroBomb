
using System;
using NeuroSdk.Websocket;

namespace NeuroSdk.Messages.API
{
    public abstract class OutgoingMessageBuilder
    {
        protected abstract string Command { get; }

        protected virtual object Data
        {
            get
            {
                return this;
            }
        }

        public virtual bool Merge(OutgoingMessageBuilder other)
        {
            return false;
        }

        public WsMessage GetWsMessage()
        {
            if (WebsocketConnection.Instance == null)
            {
                throw new InvalidOperationException(
                    "Cannot get WsMessage without a WebsocketConnection instance.");
            }

            return new WsMessage(
                Command,
                Data,
                WebsocketConnection.Instance.game);
        }
    }
}
