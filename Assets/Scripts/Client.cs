using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class Client : MonoBehaviour
{
   public static Client instance;
   public static int dataBufferSize = 4096;

   public string ip = "127.0.0.1";
   public int port = 26950;

   public TCP tcp;
   internal int myId = 0;

   private delegate void PacketHandler(Packet packet);
   private static Dictionary<int, PacketHandler> packetHandlers;

   private void Awake()
   {
      if (instance == null) {
         instance = this;
      } else if (instance != this) {
         Destroy(this);
      }
   }

   private void Start()
   {
      tcp = new TCP();
   }

   public void ConnectedToServer()
   {
      InitializeClientData();
      tcp.Connect();
   }

   public class TCP
   {
      public TcpClient socket;

      private NetworkStream stream;
      private byte[] receiveBuffer;

      private Packet receivedData;

      public void Connect()
      {
         socket = new TcpClient {
            ReceiveBufferSize = dataBufferSize,
            SendBufferSize = dataBufferSize,
         };

         receiveBuffer = new byte[dataBufferSize];
         socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
      }

      private void ConnectCallback(IAsyncResult result)
      {
         socket.EndConnect(result);

         if(!socket.Connected) {
            return;
         }

         stream = socket.GetStream();

         receivedData = new Packet();

         stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
      }

      public void SendData(Packet packet)
      {
         try {

            if (socket != null) {
               stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
            }


         } catch(Exception ex) {

            Debug.Log($"Error sending data to server via TCP: {ex.Message}");
         }
      }

      private void ReceiveCallback(IAsyncResult result)
      {
         try {
            int byteLength = stream.EndRead(result);

            if(byteLength <= 0) {
               return;
            }

            byte[] data = new byte[byteLength];
            Array.Copy(receiveBuffer, data, byteLength);

            receivedData.Reset(HandleData(data));

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

         } catch(Exception ex) {

            
         }
      }

      private bool HandleData(byte[] data)
      {
         int packetLength = 0;

         receivedData.SetBytes(data);

         if (receivedData.UnreadLength() >= 4) {
            packetLength = receivedData.ReadInt();

            if (packetLength <= 0) {
               return true;
            }
         }

         while (packetLength > 0 && packetLength <= receivedData.UnreadLength()) {
            byte[] packetBytes = receivedData.ReadBytes(packetLength);
            ThreadManager.ExecuteOnMainThread(() => {
               using(Packet packet = new Packet(packetBytes)) {

                  int packetId = packet.ReadInt();
                  packetHandlers[packetId](packet);
               }
            });

            packetLength = 0;
            if(receivedData.UnreadLength() >= 4) {
               packetLength = receivedData.ReadInt();

               if(packetLength <= 0) {
                  return true;
               }
            }
         }

         if(packetLength <= 1) {
            return true;
         }

         return false;
      }
   }

   private void InitializeClientData()
   {
      packetHandlers = new Dictionary<int, PacketHandler>() {
         { (int)ServerPackets.welcome, ClientHandle.Welcome }
      };
      Debug.Log("Initialize client data.");
   }
}
