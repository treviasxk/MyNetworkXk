using System;
using System.Net;
using System.Net.Sockets;

public delegate void ClientReceiveTCP(object sender, object[] arg);
public delegate void ClientReceiveUDP(object sender, object[] arg);
public delegate void ClientStatus(object sender, NetworkStatus code);


[Serializable]
public class MyNetworkClient {

   public static event ClientReceiveTCP OnClientReceiveTCP;
   public static event ClientReceiveUDP OnClientReceiveUDP;
   public static event ClientStatus OnClientStatus;
   static Socket ClientTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
   static byte[] receiveBuffer = new byte[1024];
   static MyNetworkClient _myNetworkClient = new MyNetworkClient();
   public static NetworkStatus MyStatus = NetworkStatus.Disconnected;
   static UdpClient ClientUDP;
   static IPEndPoint MyHost;
     
    public static void ConnectServer(IPEndPoint Host){
       Console.Clear();
      try{
         _myNetworkClient.RaiseClientStatus(NetworkStatus.Connecting);
         MyHost = Host;
         ClientUDP = new UdpClient();
         ClientUDP.Connect(Host);
         ClientUDP.BeginReceive(new AsyncCallback(ClientReceiveUDPCallback), null);

         ClientTCP.Connect(Host.Address, Host.Port);
         ClientTCP.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ClientReceiveTCPCallback), ClientTCP);
      }catch(SocketException){
         _myNetworkClient.RaiseClientStatus(NetworkStatus.Disconnected);
      }
    }


    public static void SendTCP(object[] _data){
        try{
            byte[] buffer = SharedCommands.ObjectToByteArray(_data);
            ClientTCP.BeginSend(buffer,0,buffer.Length, SocketFlags.None, new AsyncCallback(SharedCommands.SendCallback), ClientTCP);
        }catch{
            _myNetworkClient.RaiseClientStatus(NetworkStatus.Disconnected);
        }
    }

    private static void ClientReceiveTCPCallback(IAsyncResult _result){
      Socket _client = (Socket)_result.AsyncState;
      try{
         int received = _client.EndReceive(_result);
         if(received <= 0){
            _client.Close();
            return;
         }else{
            byte[] buffer = new byte[received];
            Array.Copy(receiveBuffer, buffer, received);
            object[] DataReceive = (object[])SharedCommands.ByteArrayToObject(buffer);
            switch((string)DataReceive[0]){
               case "registerudp":
                  SendUDP(DataReceive);
               break;
               case "connectedudp":
                  _myNetworkClient.RaiseClientStatus(NetworkStatus.Connected);
               break;
               default:
                  _myNetworkClient.RaiseClientReceiveTCP(DataReceive);
               break;
            }
            _client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ClientReceiveTCPCallback), _client);
         }
      }
      catch (SocketException) {
         _myNetworkClient.RaiseClientStatus(NetworkStatus.Disconnected);
         _client.Close();
         return;
      }
   }

   private void RaiseClientReceiveTCP(object[] _data){
      OnClientReceiveTCP.Invoke(this, _data);
   }

   private void RaiseClientReceiveUDP(object[] _data){
      OnClientReceiveUDP.Invoke(this, _data);
   }

   private void RaiseClientStatus(NetworkStatus code){
      MyStatus = code;
      OnClientStatus.Invoke(this, code);
   }

   public static void SendUDP(object[] _data){
      if(ClientUDP != null){
         byte[] buffer = SharedCommands.ObjectToByteArray(_data);
         ClientUDP.BeginSend(buffer, buffer.Length, null, null);
      }
   }

   private static void ClientReceiveUDPCallback(IAsyncResult _result){
      try{
         byte[] data = ClientUDP.EndReceive(_result, ref MyHost);
         if(data.Length < 4){
            //pacote perdido
            return;
         }
         object[] DataReceive = (object[])SharedCommands.ByteArrayToObject(data);
         _myNetworkClient.RaiseClientReceiveUDP(DataReceive);
         ClientUDP.BeginReceive(new AsyncCallback(ClientReceiveUDPCallback), null);
      }catch{
         //pacote perdido
      }
   }
}