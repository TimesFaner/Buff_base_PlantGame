using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.Chat
{
    public class ChatUI : NetworkBehaviour
    {
        // This is only set on client to the name of the local player
        internal static string localPlayerName;

        // Server-only cross-reference of connections to player names
        internal static readonly Dictionary<NetworkConnectionToClient, string> connNames = new();

        [Header("UI Elements")] [SerializeField]
        private Text chatHistory;

        [SerializeField] private Scrollbar scrollbar;
        [SerializeField] private InputField chatMessage;
        [SerializeField] private Button sendButton;

        public override void OnStartServer()
        {
            connNames.Clear();
        }

        public override void OnStartClient()
        {
            chatHistory.text = "";
        }

        [Command(requiresAuthority = false)]
        private void CmdSend(string message, NetworkConnectionToClient sender = null)
        {
            if (!connNames.ContainsKey(sender))
                connNames.Add(sender, sender.identity.GetComponent<Player>().playerName);

            if (!string.IsNullOrWhiteSpace(message))
                RpcReceive(connNames[sender], message.Trim());
        }

        [ClientRpc]
        private void RpcReceive(string playerName, string message)
        {
            var prettyMessage = playerName == localPlayerName
                ? $"<color=red>{playerName}:</color> {message}"
                : $"<color=blue>{playerName}:</color> {message}";
            AppendMessage(prettyMessage);
        }

        private void AppendMessage(string message)
        {
            StartCoroutine(AppendAndScroll(message));
        }

        private IEnumerator AppendAndScroll(string message)
        {
            chatHistory.text += message + "\n";

            // it takes 2 frames for the UI to update ?!?!
            yield return null;
            yield return null;

            // slam the scrollbar down
            scrollbar.value = 0;
        }

        // Called by UI element ExitButton.OnClick
        public void ExitButtonOnClick()
        {
            // StopHost calls both StopClient and StopServer
            // StopServer does nothing on remote clients
            NetworkManager.singleton.StopHost();
        }

        // Called by UI element MessageField.OnValueChanged
        public void ToggleButton(string input)
        {
            sendButton.interactable = !string.IsNullOrWhiteSpace(input);
        }

        // Called by UI element MessageField.OnEndEdit
        public void OnEndEdit(string input)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetButtonDown("Submit"))
                SendMessage();
        }

        // Called by OnEndEdit above and UI element SendButton.OnClick
        public void SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(chatMessage.text))
            {
                CmdSend(chatMessage.text.Trim());
                chatMessage.text = string.Empty;
                chatMessage.ActivateInputField();
            }
        }
    }
}