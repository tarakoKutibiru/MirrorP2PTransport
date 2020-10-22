using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mirror.WebRTC
{
    public class MirrorP2PTransport : Transport
    {
        void OnValidate()
        {

        }

        MirrorP2PClient client;
        MirrorP2PServer server;

        public override bool Available()
        {
            return true;
        }
        public override int GetMaxPacketSize(int channelId = 0)
        {
            return 0;
        }

        void Awake()
        {

        }

        public override void Shutdown()
        {

        }

        #region Client

        public override bool ClientConnected()
        {
            return false;
        }

        public override void ClientConnect(string hostname)
        {

        }

        public override void ClientDisconnect()
        {

        }

        public override bool ClientSend(int channelId, ArraySegment<byte> segment)
        {
            return false;
        }
        #endregion

        #region Server
        public override bool ServerActive()
        {
            return false;
        }

        public override void ServerStart()
        {

        }

        public override void ServerStop()
        {

        }

        public override bool ServerDisconnect(int connectionId)
        {
            return false;
        }

        public override bool ServerSend(List<int> connectionIds, int channelId, ArraySegment<byte> segment)
        {
            return false;
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            return "";
        }

        public override Uri ServerUri()
        {
            UriBuilder builder = new UriBuilder();
            return builder.Uri;
        }
        #endregion
    }
}
