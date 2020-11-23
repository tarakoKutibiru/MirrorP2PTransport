using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror.WebRTC.Samples
{
    public class HomeSceneManager : MonoBehaviour
    {
        private void Awake()
        {
            var find = GameObject.FindGameObjectsWithTag("HomeSceneManager");
            if (1 < find.Length)
            {
                Destroy(this);
                return;
            }

            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            SceneManager.sceneLoaded += this.OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "HomeScene") return;

            var find = GameObject.FindGameObjectsWithTag("NetworkManager");
            if (find == default) return;
            find.ToList().ForEach(x => Destroy(x));
        }

        private void OnGUI()
        {
            if (SceneManager.GetActiveScene().name == "HomeScene")
            {
                this.ShowHomeSceneGUI();
            }
            else
            {
                this.OnReturnButtonGUI();
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

        void OnReturnButtonGUI()
        {
            GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                float mingHeight = Screen.height / 20f;
                float minWidth = Screen.width / 10f;

                float margin = Screen.width / 100f;
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);

                if (GUILayout.Button("Return", buttonStyle, GUILayout.MinWidth(minWidth), GUILayout.MinHeight(mingHeight)))
                {
                    SceneManager.LoadScene("HomeScene");
                }

            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
