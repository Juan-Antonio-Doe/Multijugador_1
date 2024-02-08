using ExitGames.Client.Photon;
using Photon.Chat;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour, IChatClientListener {

    private ChatClient client { get; set; }

    [field: SerializeField] private string defaultChannel { get; set; } = "General";
    [field: SerializeField] private Text chatText { get; set; }
    [field: SerializeField] private KeyCode chatKey { get; set; } = KeyCode.T;
    [field: SerializeField] private KeyCode chatSendKey { get; set; } = KeyCode.Return;
    [field: SerializeField] private InputField chatTypingInput { get; set; }
    [field: SerializeField] private ScrollRect scrollRect { get; set; }

    private bool isChatActive { get; set; }
    private string nickNameChat { get; set; } = $"{PhotonNetwork.NickName}: ";
   
    void Start() {
        client = new ChatClient(this);
        client.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, PhotonNetwork.AppVersion, new AuthenticationValues(PhotonNetwork.NickName));
    }

    void Update() {
        if (client == null)
            return;

        client.Service();

        if (Input.GetKeyDown(chatKey) && !isChatActive) {
            isChatActive = true;
            chatTypingInput.ActivateInputField();
            chatTypingInput.Select();
        }

        if (Input.GetKeyDown(chatSendKey) && isChatActive) {
            isChatActive = false;
            chatTypingInput.DeactivateInputField();

            //string message = nickNameChat + chatTypingInput.text + "\n";

            if (chatTypingInput.text.Length > 0) {
                //chatText.text += message;
                client.PublishMessage(defaultChannel, chatTypingInput.text);
            }

            chatTypingInput.text = string.Empty;
        }

    }

    public void DebugReturn(DebugLevel level, string message) {

    }

    public void OnChatStateChange(ChatState state) {

    }

    public void OnConnected() {
        Debug.Log("Conected to chat.");
        client.Subscribe(new string[] { defaultChannel });
    }

    public void OnDisconnected() {
        client.Unsubscribe(new string[] { defaultChannel });
        //client.Disconnect();
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages) {
        for (int i = 0; i < senders.Length; i++) {
            chatText.text += $"{senders[i]}: {messages[i]}\n";
        }

        // Scroll to bottom
        scrollRect.normalizedPosition = new Vector2(0, 0);
    }

    public void OnPrivateMessage(string sender, object message, string channelName) {
        
    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) {
        
    }

    public void OnSubscribed(string[] channels, bool[] results) {
        
    }

    public void OnUnsubscribed(string[] channels) {
        
    }

    public void OnUserSubscribed(string channel, string user) {
        
    }

    public void OnUserUnsubscribed(string channel, string user) {
        
    }
}
