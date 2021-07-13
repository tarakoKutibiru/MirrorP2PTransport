using UnityEngine;
using UnityEngine.Events;
using System.Runtime.InteropServices;
using System.IO;
using Mirror.WebRTC.Samples;
using UnityEngine.SceneManagement;

namespace Sandbox.JavaScriptPlayground
{
    public class Test : MonoBehaviour
    {
        #region DllImport
        [DllImport("__Internal")]
        private static extern void HelloWorld();

        [DllImport("__Internal")]
        private static extern void HelloWorldString(string str);

        [DllImport("__Internal")]
        private static extern void ShowTestJsHelloWorld();

        [DllImport("__Internal")]
        private static extern void StartSignaling(string signalingUrl, string roomId, string signalingKey);

        [DllImport("__Internal")]
        private static extern void InjectionJs(string url, string id);

        #endregion

        string message = "";

        void Awake()
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            {
                var url = Path.Combine(Application.streamingAssetsPath, "Test.js");
                var id  = "0";
                InjectionJs(url, id);
            }

            {
                var url = "https://cdn.jsdelivr.net/npm/@open-ayame/ayame-web-sdk@2020.3.0/dist/ayame.js";
                var id  = "1";
                InjectionJs(url, id);
            }
            #endif
        }

        #region View
        void OnGUI()
        {
            float margin = Screen.width / 100f;
            GUILayout.BeginArea(new Rect(margin, margin, Screen.width - margin * 2f, Screen.height - margin * 2f));

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            if (this.OnButtonGUI("HelloWolrd"))
            {
                HelloWorld();
            }

            if (this.OnButtonGUI("ShowTestJsHelloWorld"))
            {
                ShowTestJsHelloWorld();
            }

            {
                GUILayout.FlexibleSpace();
                // this.message = GUILayout.TextField(this.message);
                this.message = this.OnTextFieldGUI(this.message);
                if (this.OnButtonGUI("Show Dialog"))
                {
                    HelloWorldString(this.message);
                }
                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();

            if (this.OnButtonGUI("Start Signaling"))
            {
                var settings = Resources.Load<AyameSignalingSettings>("AyameSignalingSettings");

                StartSignaling(settings.signalingUrl, settings.roomBaseId + "_" + SceneManager.GetActiveScene().name, settings.signalingKey);
            }

            GUILayout.EndVertical();

            GUILayout.EndArea();
        }

        bool OnButtonGUI(string buttonName)
        {
            float    mingHeight  = Screen.height / 20f;
            float    margin      = Screen.width / 100f;
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);

            return GUILayout.Button(buttonName, buttonStyle, GUILayout.MinWidth(Screen.width - margin * 2f), GUILayout.MinHeight(mingHeight));
        }

        string OnTextFieldGUI(string str)
        {
            float    mingHeight = Screen.height / 20f;
            float    margin     = Screen.width / 100f;
            GUIStyle style      = new GUIStyle(GUI.skin.textField);

            return GUILayout.TextField(str, style, GUILayout.MinWidth(Screen.width - margin * 2f), GUILayout.MinHeight(mingHeight));
        }
        #endregion
    }
}
