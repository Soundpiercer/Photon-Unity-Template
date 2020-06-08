using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Chat;

namespace Slite.Client
{
    public class PhotonChatController : MonoBehaviour, IChatClientListener
    {
        public Text chatText;
        public InputField messageInputField;

        private ChatClient chatClient;
        private bool isConnected = false;
        private string playerName = string.Empty;

        private const string PHOTON_CHAT_ID = ""; // must be defined
        private const string DEFAULT_CHANNEL_NAME = "channel01";

        public void Init()
        {
            StartCoroutine(InitEnumerator());
        }

        // Initializes Photon Chat Synchronously
        private IEnumerator InitEnumerator()
        {
            playerName = GetPlayerName();

            chatClient = new ChatClient(this);
            StartCoroutine(ServiceEnumerator());

            yield return new WaitUntil(() => chatClient.CanChat);
            Debug.Log("<b>[Photon Chat Controller] >>>>>> Photon Chat Init Success!</b>");

            isConnected = true;
        }

        private string GetPlayerName()
        {
            return Guid.NewGuid().ToString();
        }

        // Caution : you have to call ChatClient.Service() continuously to maintain Photon connection.
        // calling service on every frame (16ms) is recommended, but in this case the interval is set to 250ms
        private IEnumerator ServiceEnumerator()
        {
            chatClient.Service();
            chatClient.Connect(PHOTON_CHAT_ID, "1.0", new AuthenticationValues(playerName));

            while (chatClient != null)
            {
                chatClient.Service();
                yield return new WaitForSeconds(0.25f);
            }
        }

        #region IChatClientListener
        public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message)
        {
            if (level == ExitGames.Client.Photon.DebugLevel.ERROR)
            {
                Debug.LogError(message);
            }
            else if (level == ExitGames.Client.Photon.DebugLevel.WARNING)
            {
                Debug.LogWarning(message);
            }
            else
            {
                Debug.Log(message);
            }
        }

        public void OnConnected()
        {
            chatText.text = "Chat Connection Success";
            chatClient.Subscribe(new string[] { DEFAULT_CHANNEL_NAME }, 10);
        }

        public void OnDisconnected()
        {
            chatText.text = string.Empty;
        }

        public void OnChatStateChange(ChatState state)
        {
            Debug.Log("[Photon Chat] >>>>>> OnChatStateChange = " + state);
        }

        public void OnSubscribed(string[] channels, bool[] results)
        {
            chatText.text += string.Format("\nEntered to Channel ({0})", string.Join(",", channels));
        }

        public void OnUnsubscribed(string[] channels)
        {
        }

        public void OnUserSubscribed(string channel, string user)
        {
            chatText.text += "\n" + user + "entered the chat";
        }

        public void OnUserUnsubscribed(string channel, string user)
        {
            chatText.text += "\n" + user + "left the chat";
        }

        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            for (int i = 0; i < messages.Length; i++)
            {
                chatText.text += "\n" + (string.Format("{0} : {1}", senders[i], messages[i].ToString()));
            }
        }

        public void OnPrivateMessage(string sender, object message, string channelName)
        {
            Debug.Log("OnPrivateMessage : " + message);
        }

        public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
        {
            Debug.Log("status : " + string.Format("{0} is {1}, Message : {2} ", user, status, message));
        }

        public void Input_OnEndEdit(string text)
        {
            /*
            if (chatClient.State == ChatState.ConnectedToFrontEnd)
            {
                //chatClient.PublishMessage(currentChannelName, text);
                chatClient.PublishMessage(currentChannelName, inputField.text);

                inputField.text = "";
            }
            */
        }
        #endregion

        public void Send()
        {
            chatClient.PublishMessage(DEFAULT_CHANNEL_NAME, "\nMessage from " + playerName + " : " + messageInputField.text);
            messageInputField.text = string.Empty;
        }

        public void DeInit()
        {
            chatClient.Unsubscribe(new string[] { DEFAULT_CHANNEL_NAME});
            chatClient.Disconnect();

            playerName = string.Empty;
            chatText.text = string.Empty;

            isConnected = false;
            chatClient = null;
        }
    }
}
