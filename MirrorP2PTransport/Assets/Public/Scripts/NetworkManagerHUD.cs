﻿// vis2k: GUILayout instead of spacey += ...; removed Update hotkeys to avoid
// confusion if someone accidentally presses one.
using System.ComponentModel;
using UnityEngine;

namespace Mirror.WebRTC
{
    /// <summary>
    /// An extension for the NetworkManager that displays a default HUD for controlling the network state of the game.
    /// <para>This component also shows useful internal state for the networking system in the inspector window of the editor. It allows users to view connections, networked objects, message handlers, and packet statistics. This information can be helpful when debugging networked games.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/NetworkManagerHUD")]
    [RequireComponent(typeof(NetworkManager))]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [HelpURL("https://mirror-networking.com/docs/Components/NetworkManagerHUD.html")]
    public class NetworkManagerHUD : MonoBehaviour
    {
        NetworkManager manager;
        MirrorP2PTransport transport;

        /// <summary>
        /// Whether to show the default control HUD at runtime.
        /// </summary>
        public bool showGUI = true;

        /// <summary>
        /// The horizontal offset in pixels to draw the HUD runtime GUI at.
        /// </summary>
        public int offsetX;

        /// <summary>
        /// The vertical offset in pixels to draw the HUD runtime GUI at.
        /// </summary>
        public int offsetY;

        void Awake()
        {
            manager = GetComponent<NetworkManager>();
            this.transport = GetComponent<MirrorP2PTransport>();
        }

        void OnGUI()
        {
            if (!showGUI)
                return;

            GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, 300, 9999));
            if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }

            // client ready
            if (NetworkClient.isConnected && !ClientScene.ready)
            {
                if (GUILayout.Button("Client Ready"))
                {
                    ClientScene.Ready(NetworkClient.connection);

                    if (ClientScene.localPlayer == null)
                    {
                        ClientScene.AddPlayer(NetworkClient.connection);
                    }
                }
            }

            StopButtons();

            GUILayout.EndArea();
        }

        void StartButtons()
        {
            if (!NetworkClient.active)
            {
                // Server + Client
                if (GUILayout.Button("Host (Server + Client)"))
                {
                    manager.StartHost();
                }

                if (GUILayout.Button("Client"))
                {
                    manager.StartClient();
                }

                GUILayout.BeginVertical();
                GUILayout.Label("SignalingURL");
                this.transport.signalingURL = GUILayout.TextField(this.transport.signalingURL);
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.Label("SignalingKey");
                this.transport.signalingKey = GUILayout.TextField(this.transport.signalingKey);
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.Label("roomId");
                this.transport.roomId = GUILayout.TextField(this.transport.roomId);
                GUILayout.EndVertical();
            }
            else
            {
                // Connecting
                GUILayout.Label("Connecting to " + manager.networkAddress + "..");
                if (GUILayout.Button("Cancel Connection Attempt"))
                {
                    manager.StopClient();
                }
            }
        }

        void StatusLabels()
        {
            // server / client status message
            if (NetworkServer.active)
            {
                GUILayout.Label("Server: active. Transport: " + Transport.activeTransport);
            }
            if (NetworkClient.isConnected)
            {
                GUILayout.Label("Client: address=" + manager.networkAddress);
            }
        }

        void StopButtons()
        {
            // stop host if host mode
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                if (GUILayout.Button("Stop Host"))
                {
                    manager.StopHost();
                }
            }
            // stop client if client-only
            else if (NetworkClient.isConnected)
            {
                if (GUILayout.Button("Stop Client"))
                {
                    manager.StopClient();
                }
            }
            // stop server if server-only
            else if (NetworkServer.active)
            {
                if (GUILayout.Button("Stop Server"))
                {
                    manager.StopServer();
                }
            }
        }
    }
}
