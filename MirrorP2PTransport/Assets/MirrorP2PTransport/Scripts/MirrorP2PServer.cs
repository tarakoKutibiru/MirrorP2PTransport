﻿using System;
using Unity.WebRTC;

namespace Mirror.WebRTC
{
    public class MirrorP2PServer : Common
    {
        public event Action<int> OnConnectedAction; // connectionId
        public event Action<int, byte[], int> OnReceivedDataAction; // connectionId, data, channnelId
        public event Action<int> OnDisconnectedAction; // connectionId
        public event Action<int, Exception> OnReceivedErrorAction; // connectionId

        public void Start(string signalingURL, string signalingKey, string roomId)
        {
            this.signalingURL = signalingURL;
            this.signalingKey = signalingKey;
            this.roomId = roomId;

            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public bool Send(int connectionId, byte[] data)
        {
            return base.SendMessage(data);
        }

        public bool Disconnect(int connectionId)
        {
            return false;
        }

        public bool IsAlive()
        {
            return base.IsConnected();
        }

        protected override void OnMessage(RTCDataChannel channel, byte[] bytes)
        {
            base.OnMessage(channel, bytes);

            this.OnReceivedDataAction?.Invoke(1, bytes, 0);
        }

        protected override void OnConnected()
        {
            base.OnConnected();

            UnityEngine.Debug.Log("Server: OnConnected");

            this.OnConnectedAction?.Invoke(1);
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();

            this.OnDisconnectedAction?.Invoke(1);
        }
    }
}