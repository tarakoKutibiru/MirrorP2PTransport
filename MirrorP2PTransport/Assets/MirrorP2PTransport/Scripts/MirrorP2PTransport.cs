﻿using System;
using System.Collections.Generic;

namespace Mirror.WebRTC
{
    public class MirrorP2PTransport : Transport
    {
        public string signalingURL = null;
        public string signalingKey = null;
        public string roomId = null;

        MirrorP2PClient client = null;
        MirrorP2PServer server = null;

        public override bool Available()
        {
            return true;
        }
        public override int GetMaxPacketSize(int channelId = 0)
        {
            return 16 * 1024;
        }

        protected virtual void Awake()
        {
            this.client = new MirrorP2PClient();
            this.server = new MirrorP2PServer();

            this.client.OnReceivedDataAction += (data, channelId) => { this.OnClientDataReceived?.Invoke(new ArraySegment<byte>(data), channelId); };
            this.client.OnConnectedAction += () => { this.OnClientConnected?.Invoke(); };
            this.client.OnDisconnectedAction += () => { this.OnClientDisconnected?.Invoke(); };

            this.server.OnReceivedDataAction += (connectionId, data, channelId) => { this.OnServerDataReceived?.Invoke(connectionId, new ArraySegment<byte>(data), channelId); };
            this.server.OnConnectedAction += (connectionid) => { this.OnServerConnected?.Invoke(connectionid); };
            this.server.OnDisconnectedAction += (connectionid) => { this.OnServerDisconnected?.Invoke(connectionid); };

            Unity.WebRTC.WebRTC.Initialize(Unity.WebRTC.EncoderType.Software);
        }

        private void Start()
        {
            StartCoroutine(Unity.WebRTC.WebRTC.Update());
        }

        private void OnDestroy()
        {
            Unity.WebRTC.WebRTC.Dispose();
        }

        public override void Shutdown()
        {
            this.client.Disconnect();
            this.server.Stop();
        }

        #region Client

        public override bool ClientConnected()
        {
            return this.client.Connected();
        }

        public override void ClientConnect(string hostname)
        {
            this.client.Connect(this.signalingURL, this.signalingKey, this.roomId);
        }

        public override void ClientDisconnect()
        {
            this.client.Disconnect();
        }

        public override bool ClientSend(int channelId, ArraySegment<byte> segment)
        {
            byte[] data = new byte[segment.Count];
            Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);
            return this.client.Send(data);
        }

        #endregion

        #region Server
        public override bool ServerActive()
        {
            return this.server.IsAlive();
        }

        public override void ServerStart()
        {
            this.server.Start(this.signalingURL, this.signalingKey, this.roomId);
        }

        public override void ServerStop()
        {
            this.server.Stop();
        }

        public override bool ServerDisconnect(int connectionId)
        {
            return this.server.Disconnect(connectionId);
        }

        public override bool ServerSend(List<int> connectionIds, int channelId, ArraySegment<byte> segment)
        {
            // telepathy doesn't support allocation-free sends yet.
            // previously we allocated in Mirror. now we do it here.
            byte[] data = new byte[segment.Count];
            Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);

            // send to all
            bool result = true;
            foreach (int connectionId in connectionIds)
            {
                result &= server.Send(connectionId, data);
            }

            return result;
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