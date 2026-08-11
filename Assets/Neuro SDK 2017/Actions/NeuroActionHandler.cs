
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using UnityEngine;

namespace NeuroSdk.Actions
{
    public sealed class NeuroActionHandler : MonoBehaviour
    {
        private static List<INeuroAction> _currentlyRegisteredActions = new List<INeuroAction>();
        private static readonly List<INeuroAction> _dyingActions = new List<INeuroAction>();

        public static INeuroAction GetRegistered(string name)
        {
            return _currentlyRegisteredActions.FirstOrDefault(a => a.Name == name);
        }
        public static bool IsRecentlyUnregistered(string name)
        {
            return _dyingActions.Any(a => a.Name == name);
        }

        private void OnApplicationQuit()
        {
            WebsocketConnection.Instance.SendImmediate(new ActionsUnregister(_currentlyRegisteredActions));
            _currentlyRegisteredActions = null;
        }

        public static void RegisterActions(IEnumerable<INeuroAction> newActions)
        {
            _currentlyRegisteredActions.RemoveAll(oldAction => newActions.Any(newAction => oldAction.Name == newAction.Name));
            _dyingActions.RemoveAll(oldAction => newActions.Any(newAction => oldAction.Name == newAction.Name));
            _currentlyRegisteredActions.AddRange(newActions);
            WebsocketConnection.Instance.Send(new ActionsRegister(newActions));
        }

        public static void RegisterActions(params INeuroAction[] newActions)
        {
            RegisterActions((IEnumerable<INeuroAction>)newActions);
        }

        public static void UnregisterActions(IEnumerable<string> removeActionsList)
        {
            INeuroAction[] actionsToRemove = _currentlyRegisteredActions.Where(oldAction => removeActionsList.Any(removeAction => oldAction.Name == removeAction)).ToArray();

            _currentlyRegisteredActions.RemoveAll(actionsToRemove.Contains);
            _dyingActions.AddRange(actionsToRemove);

            WebsocketConnection connection = WebsocketConnection.Instance;
            connection.StartCoroutine(RemoveActions(actionsToRemove));
            connection.Send(new ActionsUnregister(removeActionsList));
        }

        private static IEnumerator RemoveActions(INeuroAction[] actionsToRemove)
        {
            yield return new WaitForSeconds(10);
            _dyingActions.RemoveAll(actionsToRemove.Contains);
        }

        public static void UnregisterActions(IEnumerable<INeuroAction> removeActionsList)
        {
            UnregisterActions(removeActionsList.Select(a => a.Name));
        }

        public static void UnregisterActions(params INeuroAction[] removeActionsList)
        {
            UnregisterActions(removeActionsList);
        }

        public static void UnregisterActions(params string[] removeActionNamesList)
        {
            UnregisterActions(removeActionNamesList);
        }

        public static void ResendRegisteredActions()
        {
            WebsocketConnection.Instance.Send(new ActionsRegister(_currentlyRegisteredActions));
        }
    }
}
