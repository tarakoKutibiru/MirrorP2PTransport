using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
using System.IO;
#endif

namespace Mirror.WebRTC
{
    public class MirrorP2PTransport : Transport
    {
        public string signalingURL = null;
        public string signalingKey = null;
        public string roomId = null;

        [Tooltip("for webgl settings")]
#if UNITY_WEBGL
#else
        [HideInInspector]
#endif
        [SerializeField] string ayameJsURL = "StreamingAssets/Ayame.js";

        MirrorP2PClient client = null;
        MirrorP2PServer server = null;

#if UNITY_WEBGL
        [DllImport("__Internal")]
        static extern void InjectionJs(string url, string id);
#endif

        public override bool Available()
        {
#if UNITY_EDITOR || UNITY_IOS || UNITY_STANDALONE || UNITY_WEBGL
            return true;
#else
            return false;
#endif
        }
        public override int GetMaxPacketSize(int channelId = 0)
        {
            return 16 * 1024;
        }

        protected virtual void Awake()
        {
            this.client = new MirrorP2PClient(signalingURL: this.signalingURL, signalingKey: this.signalingKey);
            this.server = new MirrorP2PServer();

            this.client.OnReceivedDataAction += (data, channelId) => { this.OnClientDataReceived?.Invoke(new ArraySegment<byte>(data), channelId); };
            this.client.OnConnectedAction += () => { this.OnClientConnected?.Invoke(); };
            this.client.OnDisconnectedAction += () => { this.OnClientDisconnected?.Invoke(); };

            this.server.OnReceivedDataAction += (connectionId, data, channelId) => { this.OnServerDataReceived?.Invoke(connectionId, new ArraySegment<byte>(data), channelId); };
            this.server.OnConnectedAction += (connectionid) => { this.OnServerConnected?.Invoke(connectionid); };
            this.server.OnDisconnectedAction += (connectionid) => { this.OnServerDisconnected?.Invoke(connectionid); };
#if UNITY_WEBGL
            {
                // Note: UnityRoomで使う場合
                //       UnityRoomはStreamingAssetsが使えない。.jsファイルは別途HostingServiceなどでアップしておく必要がある。
                // var url = Path.Combine(Application.streamingAssetsPath, "Ayame.js");
                var url = ayameJsURL;
                var id = "0";
                InjectionJs(url, id);
            }

            {
                var url = "https://cdn.jsdelivr.net/npm/@open-ayame/ayame-web-sdk@2020.3.0/dist/ayame.js";
                var id = "1";
                InjectionJs(url, id);
            }
#else
            Unity.WebRTC.WebRTC.Initialize(Unity.WebRTC.EncoderType.Software);
#endif
            Debug.Log("MirrorP2PTransport initialized!");
        }

        private void Start()
        {
#if !UNITY_WEBGL
            StartCoroutine(Unity.WebRTC.WebRTC.Update());
#endif
        }

        private void OnDestroy()
        {
#if !UNITY_WEBGL
            Unity.WebRTC.WebRTC.Dispose();
#endif
        }

        public override void Shutdown()
        {
            this.client.Stop();
            this.server.Stop();
        }

        #region Client

        public override bool ClientConnected()
        {
            this.client.RoomId = this.roomId;
            this.client.Run();

            return true;
        }

        public override void ClientConnect(string hostname)
        {
            this.client.RoomId = this.roomId;
            this.client.Run();
        }

        public override void ClientDisconnect()
        {
            this.client.Stop();
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
            return this.server.IsRunning();
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
