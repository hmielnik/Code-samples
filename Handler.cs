using System.Net;
using UnityEngine;

public class Handler : MonoBehaviour
{
    public static void ConnectionMessageReceived(Packet _packet)
    {
        string message = _packet.ReadString();
        int ID = _packet.ReadInt();

        Debug.Log(message + " " + ID);
        Client.Instance.clientID = ID;
        Sender.ConnectionMessageResponse();
        if(message.Contains("[Game]"))
        {
            Client.Instance.udp.Connect(((IPEndPoint)Client.Instance.tcp.clientSocket.Client.LocalEndPoint).Port);
        }
        else
        {
            LoginUI loginUI = UIManager.Instance.UIWindows["Login"].GetComponent<LoginUI>();
            loginUI.ChangeConnectionState(LoginUI.ConnectionState.Connected);
        }
    }

    public static void SpawnPlayer(Packet _packet)
    {
        int _id = _packet.ReadInt();
        string _username = _packet.ReadString();
        Vector3 position = _packet.ReadVector3();
        Quaternion rotation = _packet.ReadQuaternion();
        Debug.Log($"{_id} {_username}");
        GameManager.Instance.SpawnPlayer(_id, _username, position, rotation);
    }

    public static void PlayerPosition(Packet _packet)
    {
        int id = _packet.ReadInt();
        Vector3 position = _packet.ReadVector3();
        try
        {
            GameManager.players[id].transform.position = position;
        }
        catch
        {

        }
    }

    public static void PlayerRotation(Packet _packet)
    {
        int id = _packet.ReadInt();
        Quaternion rotation = _packet.ReadQuaternion();
        try
        {
            GameManager.players[id].transform.rotation = rotation;
        }
        catch
        {

        }
    }

    public static void PlayerDisconnected(Packet _packet)
    {
        int id = _packet.ReadInt();
        Destroy(GameManager.players[id].gameObject);
        GameManager.players.Remove(id);
    }

    public static void UDPTest(Packet _packet)
    {
        string _msg = _packet.ReadString();

        Debug.Log($"Received packet via UDP. Contains message: {_msg}");
        Sender.UDPTestReceived();
    }

    public static void LoginFailed(Packet _packet)
    {
        bool message = _packet.ReadBool();

        Debug.Log(message);
    }

    public static void LoginOK(Packet _packet)
    {
        int id = _packet.ReadInt();
        string token = _packet.ReadString();
        ServerListUI serverList = UIManager.Instance.UIWindows["ServerList"].GetComponent<ServerListUI>();
        for (int i = 0; i<_packet.UnreadLength();i+=3)
        {
            GameServerData data = new GameServerData();
            data.name = _packet.ReadString();
            data.ip = _packet.ReadString();
            data.port = _packet.ReadInt();
            data.ping = data.getPing();
            serverList.servers.Add(data);
        }

        GameManager.Instance.id = id;
        GameManager.Instance.token = token;
        Debug.Log($"ID: {id} \n TOKEN: {token}");
        UIManager.Instance.ChangeUIWindow("ServerList");
        serverList.GenerateListing();
        GameManager.Instance.hasToken = true;
    }

    public static void SaltReceived(Packet _packet)
    {
        string message = _packet.ReadString();
        Debug.Log(message);
        LoginUI loginUI = UIManager.Instance.UIWindows["Login"].GetComponent<LoginUI>();
        string password = Encrypter.HashPasswordGiven(loginUI.password, message);
       
        Sender.LoginRequest(loginUI.username, password);
    }

    public static void ChatMessage(Packet _packet)
    {
        string senderName = _packet.ReadString();
        string message = _packet.ReadString();
        Debug.Log($"{senderName} {message}");
        ChatLogger.Instance.AddEntry(senderName + ": " + message);
    }

    public static void DirectMessage(Packet _packet)
    {
        string senderName = _packet.ReadString();
        string message = _packet.ReadString();
        Debug.Log($"{senderName} {message}");
        ChatLogger.Instance.AddEntry(senderName + ": " + message, ChatLogger.MessageType.Whisper);
    }

    public static void PopulateSpawnZones(Packet _packet)
    {
        int _zoneID = _packet.ReadInt();
        Vector3 _pos = _packet.ReadVector3();
        bool _isSpawned = _packet.ReadBool();
        int _zoneType = _packet.ReadInt();

        GameManager.Instance.CreateSpawnZones(_zoneID, _pos, _isSpawned, _zoneType);
    }

    public static void UpdateZone(Packet _packet)
    {
        int _zoneID = _packet.ReadInt();
        bool _isSpawned = _packet.ReadBool();

        GameManager.spawnZones[_zoneID].UpdateZone(_isSpawned);
    }

    public static void PickupSpawnedZone(Packet _packet)
    {
        int _zoneID = _packet.ReadInt();
        bool _isSpawned = _packet.ReadBool();
        int _itemAdd = _packet.ReadInt();

        GameManager.spawnZones[_zoneID].UpdateZone(_isSpawned);
        PlayerController.Instance.items += _itemAdd;
        ChatLogger.Instance.AddEntry("Gathered an item! Total items: " + PlayerController.Instance.items, ChatLogger.MessageType.ServerMessage);
    }

    public static void PopulateNPCs(Packet _packet)
    {
        int _npcID = _packet.ReadInt();
        Vector3 _pos = _packet.ReadVector3();

        NPCManager.Instance.SpawnNPC(_npcID, _pos);
    }

    public static void NPCTransform(Packet _packet)
    {
        int id = _packet.ReadInt();
        Vector3 position = _packet.ReadVector3();
        Quaternion rotation = _packet.ReadQuaternion();
        if(NPC.npcs.ContainsKey(id))
            NPC.npcs[id].Move(position, rotation);
    }

    public static void PopulateMonster(Packet _packet)
    {
        int _monsterID = _packet.ReadInt();
        Vector3 _pos = _packet.ReadVector3();

        MonsterManager.Instance.SpawnMonster(_monsterID, _pos);
    }

    public static void MonsterTransform(Packet _packet)
    {
        int id = _packet.ReadInt();
        Vector3 position = _packet.ReadVector3();
        Quaternion rotation = _packet.ReadQuaternion();
        if (Monster.monsters.ContainsKey(id))
            Monster.monsters[id].Move(position, rotation);
    }

    public static void MonsterDeath(Packet _packet)
    {
        int id = _packet.ReadInt();
        Monster.monsters[id].Die();
    }

    public static void ServerMessage(Packet _packet)
    {
        string message = _packet.ReadString();
        ChatLogger.Instance.AddEntry("[SERVER] " + message, ChatLogger.MessageType.ServerMessage);
    }

}
