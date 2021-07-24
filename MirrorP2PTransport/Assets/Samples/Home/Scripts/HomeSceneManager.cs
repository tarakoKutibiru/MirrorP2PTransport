using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mirror.WebRTC.Samples
{
    public class HomeSceneManager : MonoBehaviour
    {
        [SerializeField] Canvas signalingView = default;

        [SerializeField] InputField url = default;
        [SerializeField] InputField key = default;
        [SerializeField] InputField roomId = default;

        enum State
        {
            SelectScene,
            Signaling,
        }

        State state = State.Signaling;

        private void Start()
        {
            this.url.text = MirrorP2PTransport.GetSettings().signalingUrl;
            this.key.text = MirrorP2PTransport.GetSettings().signalingKey;
            this.roomId.text = MirrorP2PTransport.GetSettings().roomId;
        }

        private void OnGUI()
        {
            if (this.state == State.Signaling)
            {
                this.signalingView.gameObject.SetActive(true);
            }
            else if (this.state == State.SelectScene)
            {
                this.signalingView.gameObject.SetActive(false);
                this.ShowHomeSceneGUI();
            }
        }

        void ShowHomeSceneGUI()
        {
            var sceneNames = new List<string>()
            {
            "SignalingTestScene",
            "SendMessageTestScene",
            "SyncTransformTestScene",

            "BasicScene",
            "ChatScene",
            "PongScene",
            "RigidbodyPhysicsScene",
            "TanksScene",
            };

            float margin = Screen.width / 100f;
            GUILayout.BeginArea(new Rect(margin, margin, Screen.width - margin * 2f, Screen.height - margin * 2f));

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            sceneNames.ForEach(s => this.OnSceneButtonGUI(s));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndArea();
        }

        void OnSceneButtonGUI(string sceneName)
        {
            float mingHeight = Screen.height / 20f;
            float margin = Screen.width / 100f;
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);

            if (GUILayout.Button(sceneName, buttonStyle, GUILayout.MinWidth(Screen.width - margin * 2f), GUILayout.MinHeight(mingHeight)))
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public void OnApplyButton()
        {
            MirrorP2PTransport.GetSettings().signalingUrl = this.url.text;
            MirrorP2PTransport.GetSettings().signalingKey = this.key.text;
            MirrorP2PTransport.GetSettings().roomId = this.roomId.text;

            this.state = State.SelectScene;
        }
    }
}
