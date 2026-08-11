using System;
using System.Collections;
using System.Collections.Generic;
using NeuroSdk.Internal;
using NeuroSdk.Messages.API;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using UnityEngine;
using UnityEngine.Events;

namespace NeuroSdk.Websocket
{
    public sealed class CharacterMetadata
    {
        public CharacterMetadata(string characterId, string displayName)
        {
            CharacterId = characterId;
            DisplayName = displayName;
        }

        public string CharacterId { get; }
        public string DisplayName { get; }
    }

    public sealed class WebsocketConnection : MonoBehaviour
    {
        private const float RECONNECT_INTERVAL = 3;

        private static bool _checkingSelf;

        private static WebsocketConnection _instance;
        public static WebsocketConnection Instance
        {
            get
            {
                if (!_instance && !_checkingSelf) Debug.LogWarning("Accessed WebsocketConnection.Instance without an instance being present");
                _checkingSelf = false;
                return _instance;
            }
            private set{
                _instance = value;
            }
        }

        private static WebSocket _socket;

        private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
        private readonly object _mainThreadQueueLock = new object();

        public string game = "";
        public MessageQueue messageQueue = null;
        public CommandHandler commandHandler = null;
        public CharacterMetadata Character { get; private set; }

        public UnityEvent onConnected;
        public UnityEvent<string> onError;
        public UnityEvent<CloseStatusCode> onDisconnected;
        public UnityEvent<CharacterMetadata> onCharacterChanged;

        private void Awake()
        {
            _checkingSelf = true;
            if (Instance)
            {
                Debug.Log("Destroying duplicate WebsocketConnection instance");
                Destroy(this);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;

            Debug.Log("NeuroSdk WebsocketConnection is now awake");
        }

        private void Start(){
            this.StartCoroutine(StartWs());
        }

        private IEnumerator Reconnect()
        {
            yield return new WaitForSecondsRealtime(RECONNECT_INTERVAL);
            yield return StartWs();
        }

        private IEnumerator StartWs()
        {
            try
            {
                if (_socket != null &&
                    (_socket.ReadyState == WebSocketState.Open ||
                    _socket.ReadyState == WebSocketState.Connecting))
                {
                    _socket.Close();
                }
            }
            catch
            {
                // ignored
            }

            string websocketUrl = null;
            yield return WsUrlFinder.FindWsUrl(result => websocketUrl = result);

            if (websocketUrl == null || websocketUrl == "")
            {
                string errMessage = "Could not retrieve websocket URL.";
#if UNITY_EDITOR || !UNITY_WEBGL
                errMessage += " You should set the NEURO_SDK_WS_URL environment variable.";
#endif
#if UNITY_WEBGL
                errMessage += " You need to specify a WebSocketURL query parameter in the URL or open a local server that serves the NEURO_SDK_WS_URL environment variable. See the documentation for more information.";
#endif
                Debug.LogError(errMessage);
                yield break;
            }

            // Websocket callbacks get run on separate threads! Watch out
            _socket = new WebSocket(websocketUrl);
            
            _socket.OnOpen += (sender, e) =>
            {
                QueueMainThread(delegate
                {
                    HandleOpen();
                });
            };
            _socket.OnMessage += (sender, e) =>
            {
                if (!e.IsText)
                {
                    return;
                }
                QueueMainThread(delegate
                {
                    ReceiveMessage(e.Data);
                });
            };
            _socket.OnError += (sender, e) =>
            {
                
                QueueMainThread(delegate
                {
                    HandleError(e.Message);
                });
            };
            _socket.OnClose += (sender, e) =>
            {
                CloseStatusCode closeCode = (CloseStatusCode)e.Code;
                QueueMainThread(delegate
                {
                    HandleClose(closeCode);
                });
            };

            _socket.Connect();
        }
        private void HandleOpen()
        {
            if (onConnected != null)
            {
                onConnected.Invoke();
            }
        }
        private void HandleError(string error)
        {
            if(onError != null){
                onError.Invoke(error);
            }
            if (error != "Unable to connect to the remote server")
            {
                Debug.LogError("Websocket connection has encountered an error!");
                Debug.LogError(error);
            }
        }
        private void HandleClose(CloseStatusCode code)
        {
            if(onDisconnected != null)
            {
                onDisconnected.Invoke(code);
            }
            if (code != CloseStatusCode.Abnormal) Debug.LogWarning(string.Format("Websocket connection has been closed with code {0}!", code));
            this.StartCoroutine(Reconnect());
        }

        private void Update()
        {
            ProcessMainThreadQueue();

            if (_socket == null || _socket.ReadyState != WebSocketState.Open)
            {
                return;
            }
            while (messageQueue.Count > 0)
            {
                OutgoingMessageBuilder builder = messageQueue.Dequeue();
                if (builder != null)
                {
                    SendMessage(builder);
                }
            }
        }

        private void QueueMainThread(Action action)
        {
            if (action == null)
            {
                return;
            }
            lock (_mainThreadQueueLock)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }

        private void SendMessage(OutgoingMessageBuilder builder)
        {
            string message = Jason.Serialize(builder.GetWsMessage());

            Debug.Log(string.Format("Sending ws message {0}", message));
            try
            {
                _socket.SendAsync(
                    message,
                    delegate(bool completed)
                    {
                        if (!completed)
                        {
                            messageQueue.Enqueue(builder);
                        }
                    });
            }
            catch (Exception e)
            {
                Debug.LogError(
                    string.Format(
                        "Failed to start websocket send for message {0}: {1}",
                        message,
                        e));

                messageQueue.Enqueue(builder);
            }
        }

        public void Send(OutgoingMessageBuilder messageBuilder){
            messageQueue.Enqueue(messageBuilder);
        }

        public void SetCharacterMetadata(CharacterMetadata metadata)
        {
            Character = metadata;
            if(onCharacterChanged != null){
                onCharacterChanged.Invoke(metadata);
            }
        }

        public void SendImmediate(OutgoingMessageBuilder messageBuilder)
        {
            string message = Jason.Serialize(messageBuilder.GetWsMessage());

            if (_socket == null || _socket.ReadyState != WebSocketState.Open)
            {
                Debug.LogError(string.Format("WS not open - failed to send immediate ws message {0}", message));
                return;
            }

            Debug.Log(string.Format("Sending immediate ws message {0}", message));

            _socket.Send(message);
        }

        private void ReceiveMessage(string msgData)
        {
            try
            {
                Debug.Log("Received ws message " + msgData);

                JObject message = JObject.Parse(msgData);
                string command = null;
                JToken commandToken = message["command"];
                if (commandToken != null)
                {
                    command = commandToken.Value<string>();
                }
                MessageJData data = new MessageJData(message["data"]);

                if (command == null)
                {
                    Debug.LogError("Received command that could not be deserialized. Wtf are you doing?");
                    return;
                }
                commandHandler.Handle(command, data);
            }
            catch (Exception e)
            {
                Debug.LogError("Received invalid message");
                Debug.LogError(e.ToString());
            }
        }
        private void ProcessMainThreadQueue()
        {
            while (true)
            {
                Action action;

                lock (_mainThreadQueueLock)
                {
                    if (_mainThreadQueue.Count == 0){break;}
                    action = _mainThreadQueue.Dequeue();
                }
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception while processing websocket callback on the main thread.");
                    Debug.LogError(e.ToString());
                }
            }
        }
    }
}
