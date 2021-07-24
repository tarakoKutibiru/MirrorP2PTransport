using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror.WebRTC.Samples
{
    public class ReturnHomeView : MonoBehaviour
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
            if (SceneManager.GetActiveScene().name != "HomeScene")
            {
                this.OnReturnButtonGUI();
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
                    var manager = NetworkManager.singleton;
                    // stop host if host mode
                    if (NetworkServer.active && NetworkClient.isConnected)
                    {
                        manager.StopHost();
                    }
                    // stop client if client-only
                    else if (NetworkClient.isConnected)
                    {
                        manager.StopClient();
                    }
                    // stop server if server-only
                    else if (NetworkServer.active)
                    {
                        manager.StopServer();
                    }
                    SceneManager.LoadScene("HomeScene");
                }

            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
