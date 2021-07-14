using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Sandbox.AyameDataChannelSample
{
    public class Presenter : MonoBehaviour
    {
        #region AyameSettings
        [SerializeField] InputField signalingUrl = default;
        [SerializeField] InputField signalingKey = default;
        [SerializeField] InputField roomId       = default;

        public string SignalingUrl => this.signalingUrl.text;
        public string SignalingKey => this.signalingKey.text;
        public string RoomId       => this.roomId.text;
        #endregion

        [SerializeField] Text  historyText = default;
        [SerializeField] Ayame ayame       = default;

        [SerializeField] InputField message = default;
        public string               Message => this.message.text;

        [SerializeField] Text statusText = default;

        void Start()
        {
            this.ayame.MessageHandler      += this.OnMessage;
            this.ayame.ConnectedHandler    += () => { this.statusText.text = "Connected"; };
            this.ayame.DisconnectedHandler += () => { this.statusText.text = "Disconnected"; };
        }

        public void Connect()
        {
            this.statusText.text = "Signaling";
            Ayame.Connect(this.SignalingUrl, this.SignalingKey, this.RoomId);
        }

        public void Disconnected()
        {
            Ayame.Disconnect();
        }

        public void SendMessage()
        {
            Ayame.SendData(this.Message);
        }

        void OnMessage(string message)
        {
            this.historyText.text += $"{DateTime.Now.ToShortTimeString()} :{message}\n";
        }
    }
}
