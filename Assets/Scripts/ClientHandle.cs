using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
    public static void Welcome(Packet packet)
   {
      string message = packet.ReadString();
      int myId = packet.ReadInt();

      Debug.Log($"Welcome! Message from server: {message}");
      Client.instance.myId = myId;
      ClientSend.WelcomeReceived();

      Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
   }

   internal static void UDPTest(Packet packet)
   {
      string message = packet.ReadString();

      Debug.Log($"Message UDP from server: {message}");
      ClientSend.UDPTestReceived();
   }
}
