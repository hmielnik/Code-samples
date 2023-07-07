using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogger : MonoBehaviour
{
    [SerializeField] private GameObject chatEntry;
    private List<GameObject> chatEntries = new List<GameObject>();
    [SerializeField] private Transform content;
    public static ChatLogger Instance;
    [SerializeField] private InputField chatInputField;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if(chatInputField.text.Length > 0 && Input.GetKeyDown(KeyCode.Return))
        {
            SendEntry();
        }

        if(chatInputField.isFocused)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            if(PlayerController.Instance != null && CameraController.Instance != null)
            {
                PlayerController.Instance.isFrozen = true;
                CameraController.Instance.isFrozen = true;
            }
        }

        if(Input.GetKey(KeyCode.LeftAlt))
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            if (CameraController.Instance != null)
                CameraController.Instance.isFrozen = true;
        }
        else
        {
            if(!chatInputField.isFocused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                if (PlayerController.Instance != null && CameraController.Instance != null)
                {
                    PlayerController.Instance.isFrozen = false;
                    CameraController.Instance.isFrozen = false;
                }
            }
        }
    }

    public void SendEntry()
    {
        if(chatInputField.text.StartsWith("/w"))
        {
            string[] message = chatInputField.text.Split(' ');
            string msg = "";
            for(int i = 2; i < message.Length; i++)
            {
                msg += message[i] + " ";
            }
            Sender.PlayerDirectChat(message[1], msg);
            AddEntry(GameManager.Instance.username + ": " + msg, MessageType.Whisper);
        }
        else
        {
            Sender.PlayerChat(chatInputField.text);
            AddEntry(GameManager.Instance.username + ": " + chatInputField.text);
        }
        chatInputField.text = "";
    }

    public void AddEntry(string _message, MessageType _messageType = MessageType.Global)
    {
        var obj = Instantiate(chatEntry);
        Text _text = obj.gameObject.GetComponentInChildren<Text>();
        _text.text = _message;
        switch(_messageType)
        {
            case MessageType.Global:
                _text.color = Color.green;
                break;
            case MessageType.Whisper:
                _text.color = Color.magenta;
                break;
            case MessageType.ServerMessage:
                _text.color = Color.yellow;
                _text.fontStyle = FontStyle.Bold;
                break;
        }
        obj.gameObject.transform.parent = content;
        chatEntries.Add(obj.gameObject);
        if(chatEntries.Count > 21)
        {
            GameObject entry = chatEntries[0];
            chatEntries.RemoveAt(0);
            Destroy(entry);
        }
    }

    public enum MessageType
    {
        Whisper = 0,
        Global = 1,
        ServerMessage = 2
    }

}
