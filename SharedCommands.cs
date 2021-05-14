using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public enum NetworkStatus {
      Connected = 200,
      Connecting = 100,
      Disconnected = 400
}
[Serializable]
public class SharedCommands {
    public static void SendCallback(IAsyncResult _result){
      try{
         Socket _client = (Socket)_result.AsyncState;
         _client.EndSend(_result);
      }catch{}

    }

    public static void CW(ConsoleColor _color, string title, string text){
      Console.ForegroundColor = _color;
      Console.Write("[{0}] ", title);
      Console.ResetColor();
      Console.WriteLine(text);
   }
    public static byte[] ObjectToByteArray(object obj){
    BinaryFormatter bf = new BinaryFormatter();
      using (var ms = new MemoryStream()){
         bf.Serialize(ms, obj);
         return ms.ToArray();
      }
   }

   public static object ByteArrayToObject(byte[] arrBytes){
      using (var memStream = new MemoryStream()){
         var binForm = new BinaryFormatter();
         memStream.Write(arrBytes, 0, arrBytes.Length);
         memStream.Seek(0, SeekOrigin.Begin);
         try{
            return binForm.Deserialize(memStream);
         }
         catch{
            return null;
         }
      }
   }
}