using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public delegate void ServerReceiveTCP(object sender, DataServerReceiveArgs arg);
public delegate void ServerReceiveUDP(object sender, DataServerReceiveArgs arg);
public delegate void ServerStatus(object sender, DataServerReceiveArgs args);
public class DataServerReceiveArgs : EventArgs{
    public EndPoint IPClient;
    public object[] Data;
    public NetworkStatus NetworkStatus;
}
[Serializable]
public class MyNetworkServer{
   public static NetworkStatus MyStatus = NetworkStatus.Disconnected;
   public static Dictionary<EndPoint, Socket> ClientsTCP = new Dictionary<EndPoint, Socket>();
   public static Dictionary<EndPoint, IPEndPoint> ClientsUDP = new Dictionary<EndPoint, IPEndPoint>();
   public static event ServerReceiveTCP OnServerReceiveTCP;
   public static event ServerReceiveUDP OnServerReceiveUDP;
   public static event ServerStatus OnServerStatus;
   static MyNetworkServer _myNetworkServer = new MyNetworkServer();
   static Socket ServerTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
   static byte[] receiveBuffer = new byte[1024];
   static bool onserver = true;
   static UdpClient ServerUDP;
   public static void StartServer(IPEndPoint Host){
      Console.Clear();
      Console.WriteLine(" ===================== My Network Xk =====================");
      Console.WriteLine("  DESENVOLVEDOR:                          TREVIAS XK");
      Console.WriteLine("  LICENÇA:                                GPL-3.0 License");
      Console.WriteLine("  VERSÃO:                                 1.0.0.0");
      Console.WriteLine(" =========================================================");
      SharedCommands.CW(ConsoleColor.Green,"SERVER", "Iniciando servidor...");
      MyStatus = NetworkStatus.Connecting;
      ServerUDP = new UdpClient(Host);
      ServerUDP.BeginReceive(new AsyncCallback(ServerReceiveUDPCallback), null);

      ServerTCP.Bind(Host);
      ServerTCP.Listen(1);
      ServerTCP.BeginAccept(new AsyncCallback(ServerTCPCallback), null);
      SharedCommands.CW(ConsoleColor.Green,"SERVER","Servidor TCP/UDP está hospedado na porta: " + Host.Port.ToString());
      MyStatus = NetworkStatus.Connected;
      while(onserver){
         switch(Console.ReadLine()){
            case "q":
               StopServer();
            break;
            case "onlines":
               SharedCommands.CW(ConsoleColor.Green,"INFO", $"Onlines: {MyNetworkServer.ClientsTCP.Count}");
            break;
            default: 
               SharedCommands.CW(ConsoleColor.Green,"INFO", "Comando inválido!");
            break;
         }
      }
   }

   public static void StopServer(){
      Console.Clear();
      SharedCommands.CW(ConsoleColor.Green,"SERVER", "Desligando servidor...");
      ServerUDP.Close();
      ServerTCP.Close();
      MyStatus = NetworkStatus.Disconnected;
      onserver = false;
   }

   private static void ServerTCPCallback(IAsyncResult _result){
      Socket _client;
      try{
          _client = ServerTCP.EndAccept(_result);
      }
      catch (ObjectDisposedException){
         return;
      }
      ConnectClient(_client);
      _client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ServerReceiveTCPCallback), _client);
      ServerTCP.BeginAccept(new AsyncCallback(ServerTCPCallback), null);
   }

   private static void ServerReceiveTCPCallback(IAsyncResult _result){
      Socket _client = (Socket)_result.AsyncState;
      try{
         int received = _client.EndReceive(_result);
         if(received <= 0){
            DisconnectClient(_client);
            return;
         }else{
            byte[] buffer = new byte[received];
            Array.Copy(receiveBuffer, buffer, received);
            object[] DataReceive = (object[])SharedCommands.ByteArrayToObject(buffer);
            if(DataReceive != null){
               _myNetworkServer.RaiseServerReceiveTCP(_client.RemoteEndPoint, DataReceive);
               _client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ServerReceiveTCPCallback), _client);
            }else{
               SharedCommands.CW(ConsoleColor.Red,"AVISO", "O servidor está sobrecarregado!");
               DisconnectClient(_client);
            }
         }
      }
      catch (SocketException) {
         DisconnectClient(_client);
         return;
      }
   }

   static void RegisterClientUDP(Socket _client){
      object[] DataSend = new object[5];
      DataSend[0] = "registerudp";
      DataSend[1] = _client.RemoteEndPoint;
      SendUnicastTCP(_client, DataSend);
   }

   static void RemoveClientUDP(EndPoint _endPoint){
      if(MyNetworkServer.ClientsUDP.ContainsKey(_endPoint)){
         MyNetworkServer.ClientsUDP.Remove(_endPoint);
      }
   }

   public static void ConnectClient(Socket _client){
      if(!MyNetworkServer.ClientsTCP.ContainsKey(_client.RemoteEndPoint)){
         MyNetworkServer.ClientsTCP.Add(_client.RemoteEndPoint, _client);
         RegisterClientUDP(_client);
      }
   }

   public static void DisconnectClient(Socket _client){
      _myNetworkServer.RaiseServerStatus(_client.RemoteEndPoint, NetworkStatus.Disconnected);
      if(MyNetworkServer.ClientsTCP.ContainsKey(_client.RemoteEndPoint)){
         MyNetworkServer.ClientsTCP.Remove(_client.RemoteEndPoint);
         RemoveClientUDP(_client.RemoteEndPoint);
         _client.Close();
      }
   }


    public void RaiseServerReceiveTCP(EndPoint _endPoint, object[] _data){
      DataServerReceiveArgs e = new DataServerReceiveArgs();
      e.IPClient = _endPoint;
      e.Data = _data;
      OnServerReceiveTCP.Invoke(this, e);
    }

   public void RaiseServerReceiveUDP(EndPoint _endPoint, object[] _data){
      DataServerReceiveArgs e = new DataServerReceiveArgs();
      e.IPClient = _endPoint;
      e.Data = _data;
      OnServerReceiveUDP.Invoke(this, e);
    }

   public void RaiseServerStatus(EndPoint _endPoint, NetworkStatus code){
      DataServerReceiveArgs e = new DataServerReceiveArgs();
      e.IPClient = _endPoint;
      e.NetworkStatus = code;
      e.Data = null;
      OnServerStatus.Invoke(this, e);
   }
   
   public static void SendBroadcastTCP(object[] _data){
      byte[] buffer = SharedCommands.ObjectToByteArray(_data);
      foreach (Socket _client in MyNetworkServer.ClientsTCP.Values){
         _client.BeginSend(buffer,0,buffer.Length, SocketFlags.None, new AsyncCallback(SharedCommands.SendCallback), _client);
      }
   }

   public static void SendUnicastTCP(Socket _client, object[] _data){
      byte[] buffer = SharedCommands.ObjectToByteArray(_data);
      _client.BeginSend(buffer,0,buffer.Length, SocketFlags.None, new AsyncCallback(SharedCommands.SendCallback), _client);
   }

   public static void SendMulticastTCP(Socket[] Clients, object[] _data){
      byte[] buffer = SharedCommands.ObjectToByteArray(_data);
      foreach (Socket _client in Clients){
         _client.BeginSend(buffer,0,buffer.Length, SocketFlags.None, new AsyncCallback(SharedCommands.SendCallback), _client);
      }
   }

   static void ServerReceiveUDPCallback(IAsyncResult _result){
      try{
         IPEndPoint _client = new IPEndPoint(IPAddress.Any, 0);
         byte[] data = ServerUDP.EndReceive(_result, ref _client);
         if(data.Length < 4){
            return; // pacotes perdido
         }
         object[] DataReceive = (object[])SharedCommands.ByteArrayToObject(data);
         switch((string)DataReceive[0]){
            case "registerudp":
               ConnectClientUDP(_client, DataReceive);
            break;
            default:
               _myNetworkServer.RaiseServerReceiveUDP((EndPoint)_client, DataReceive);
            break;
         }
         ServerUDP.BeginReceive(new AsyncCallback(ServerReceiveUDPCallback), null);
      }catch{
         return; // pacotes perdido
      }
   }

   static void ConnectClientUDP(IPEndPoint _client, object[] _data){
      EndPoint _endPoint = (EndPoint)_data[1];
      if(!MyNetworkServer.ClientsUDP.ContainsKey(_endPoint)){
         MyNetworkServer.ClientsUDP.Add(_endPoint, _client);
         _myNetworkServer.RaiseServerStatus(_endPoint, NetworkStatus.Connected);
         _data = new object[1];
         _data[0] = "connectedudp";
         SendUnicastTCP(ClientsTCP[_endPoint], _data);
      }
   }

   public static void SendUnicastUDP(IPEndPoint _client, object[] _data){
      byte[] buffer = SharedCommands.ObjectToByteArray(_data);
      ServerUDP.BeginSend(buffer, buffer.Length, _client, null, null);
   }

   public static void SendBroadcastUDP(object[] _data){
      byte[] buffer = SharedCommands.ObjectToByteArray(_data);
      foreach (IPEndPoint _client in MyNetworkServer.ClientsUDP.Values){
         ServerUDP.BeginSend(buffer, buffer.Length, _client, null, null);
      }
   }
}