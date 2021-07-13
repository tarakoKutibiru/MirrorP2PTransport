using UnityEngine;
using UnityEngine.Events;
using System.Runtime.InteropServices;

namespace Sandbox.JavaScriptPlayground
{
    public class Test : MonoBehaviour
    {
        #region DllImport
        [DllImport("__Internal")]
        private static extern void HelloWorld();

        [DllImport("__Internal")]
        private static extern void HelloWorldString(string str);
        #endregion

        string message = "";

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
    }
}
