using System;
using System.Net;

class Program{
   static IPEndPoint MyHost = new IPEndPoint(IPAddress.Any, 26950);
    static void Main(string[] args){
      MyNetworkClient.OnClientReceiveTCP += new ClientReceiveTCP(OnClientReceiveTCP);
      MyNetworkClient.OnClientReceiveUDP += new ClientReceiveUDP(OnClientReceiveUDP);
      MyNetworkClient.OnClientStatus += new ClientStatus(OnClientStatus);
      MyNetworkServer.OnServerReceiveTCP += new ServerReceiveTCP(OnServerReceiveTCP);
      MyNetworkServer.OnServerReceiveUDP += new ServerReceiveUDP(OnServerReceiveUDP);
      MyNetworkServer.OnServerStatus += new ServerStatus(OnServerStatus);
      command();
   }
   static void command(){
      Console.WriteLine("1 - Server");
      Console.WriteLine("2 - Client");
      Console.WriteLine("3 - Sair");
      switch(Console.ReadLine()){
         case "1":
            MyNetworkServer.StartServer(MyHost);            
         break;
         case "2":
            MyNetworkClient.ConnectServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), MyHost.Port));
            mywrite();
         break;
         case "3":
         break;
         default: 
            Console.WriteLine("Opção selecionado inválido!");
            command();
         break;
      }
   }

   //Send data TCP/UDP to clients
   static void mywrite(){
      while(true){
         object[] DataSend = new object[5];
         DataSend[0] = Console.ReadLine();
         MyNetworkClient.SendTCP(DataSend);
         MyNetworkClient.SendUDP(DataSend);
      }
   }

   //========================== EVENTS SERVER ==========================
   static void OnServerStatus(object sender, DataServerReceiveArgs e){
      if(e.NetworkStatus == MyNetworkServer.NetworkStatus.Connected){
         Console.WriteLine($"{e.IPClient} conectou-se.");
      }
      if(e.NetworkStatus == MyNetworkServer.NetworkStatus.Disconnected){
         Console.WriteLine($"{e.IPClient} desconectou-se.");
      }
   }

   static void OnServerReceiveTCP(object sender, DataServerReceiveArgs e){
      Console.WriteLine($"{e.IPClient}: {(string)e.Data[0]}");
      MyNetworkServer.SendBroadcastTCP(e.Data); //Send data to all clients TCP connected.
   }

   static void OnServerReceiveUDP(object sender, DataServerReceiveArgs e){
       Console.WriteLine($"{e.IPClient}: {(string)e.Data[0]}");
       MyNetworkServer.SendBroadcastUDP(e.Data); //Send data to all clients UDP connected.
   }


   //========================== EVENTS CLIENT ==========================
   static void OnClientStatus(object sender, MyNetworkClient.NetworkStatus e){
     if(e == MyNetworkClient.NetworkStatus.Connected){
         Console.WriteLine("Conectado com o servidor.");
      }
      if(e == MyNetworkClient.NetworkStatus.Disconnected){
         Console.WriteLine("Sem conexão com o servidor.");
      }
   }

   static void OnClientReceiveTCP(object sender, object[] data){
      Console.WriteLine($"SERVER: {(string)data[0]}");
   }
   static void OnClientReceiveUDP(object sender, object[] data){
      Console.WriteLine($"SERVER: {(string)data[0]}");
   }
}