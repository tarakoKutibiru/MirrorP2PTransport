using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror.WebRTC.Samples
{
    public class HomeSceneManager : MonoBehaviour
    {
        private void OnGUI()
        {
            var sceneNames = new List<string>()
            {
            "SignalingTestScene",
            "SendMessageTestScene",
            "SignalingTestScene",
            "SyncTranformTestScene",

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
            sceneNames.ForEach(s => this.OnButtonGUI(s));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndArea();
        }

        void OnButtonGUI(string sceneName)
        {
            float mingHeight = Screen.height / 20f;
            float margin = Screen.width / 100f;
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);

            if (GUILayout.Button(sceneName, buttonStyle, GUILayout.MinWidth(Screen.width - margin * 2f), GUILayout.MinHeight(mingHeight)))
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
