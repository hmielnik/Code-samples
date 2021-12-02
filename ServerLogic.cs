using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class ServerLogic : MonoBehaviour
{
    public static int maxPlayers;
    public static int port;

    public static Dictionary<int, Client> clients = new Dictionary<int, Client>();

    public static TcpListener tcpListener;
    public static UdpClient udpListener;

    public static void StartServer(int _maxPlayers, int _port)
    {
        maxPlayers = _maxPlayers;
        port = _port;

        GameServerClient.Instance = new GameServerClient();
        GameServerClient.Instance.ConnectToServer();
        for (int i = 1; i <= maxPlayers; i++)
        {
            clients.Add(i, new Client(i));
        }

        tcpListener = new TcpListener(IPAddress.Any, port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(TCPConnCallback, null);

        udpListener = new UdpClient(port);
        udpListener.BeginReceive(UDPReceiveCallback, null);

        Logger.LogAnnouncement($"Server instance started using port: {port}", Priority.HIGH);
    }

    public static void StopServer()
    {
        Sender.ServerAnnouncementSend("The server has closed");

        foreach(Client client in clients.Values)
        {
            if(client.player != null)
            client.Disconnect();
        }
        tcpListener.Stop();
        udpListener.Close();
    }

    public static void TCPConnCallback(IAsyncResult _result)
    {
        TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
        tcpListener.BeginAcceptTcpClient(TCPConnCallback, null);
        Logger.Log($"Connection attempt from {_client.Client.RemoteEndPoint}", Priority.HIGH);

        for (int i = 1; i <= maxPlayers; i++)
        {
            if (clients[i].tcp.clientSocket == null)
            {
                clients[i].tcp.Connect(_client);
                return;
            }
        }

        Logger.LogError($"{_client.Client.RemoteEndPoint} connection terminated with message: Server is full", Priority.HIGH);
    }

    private static void UDPReceiveCallback(IAsyncResult _result)
    {
        try
        {
            IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            if (_data.Length < 4)
            {
                return;
            }

            using (Packet _packet = new Packet(_data))
            {
                int _clientId = _packet.ReadInt();

                if (_clientId == 0)
                {
                    return;
                }

                if (clients[_clientId].udp.endPoint == null)
                {
                    clients[_clientId].udp.Connect(_clientEndPoint);
                    return;
                }

                if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                {
                    clients[_clientId].udp.HandleData(_packet);
                }
            }
        }
        catch (Exception _ex)
        {
            Logger.LogError($"Error receiving UDP data: {_ex}", Priority.HIGH);
        }
    }

    public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
    {
        try
        {
            if (_clientEndPoint != null)
            {
                udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
            }
        }
        catch (Exception _ex)
        {
            Logger.LogError($"Error sending data to {_clientEndPoint} via UDP: {_ex}", Priority.HIGH);
        }
    }

}
