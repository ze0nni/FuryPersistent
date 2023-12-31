#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Fury.Settings.UI
{
    public abstract class SettingsGUIEditorWindow : EditorWindow
    {
        SettingsGUI _gui;

        private SettingsGUI GUI
        {
            get
            {
                if (_gui == null)
                {
                    var (userId, settingsType) = GetDefault();
                    Setup(userId, settingsType);
                }
                return _gui;
            }
        }

        protected abstract (string userId, Type settingsType) GetDefault();

        public void Setup(string userId, Type settingsType)
        {
            var controller = SettingsController.Get(userId, settingsType);
            _gui = new SettingsGUI(GUIMode.Editor, controller, GetGUISize, null, Repaint);
        }

        private void GetGUISize(out Matrix4x4 matrix, out Vector2 screenSize, out Rect pageSize)
        {
            matrix = Matrix4x4.identity;
            screenSize = new Vector2(position.width, position.height);
            pageSize = new Rect(0, 0, screenSize.x, screenSize.y);
        }

        protected virtual void Update()
        {
            GUI?.Update();
        }

        private void OnGUI()
        {
            GUI?.OnGUI();
        }
    }
}
#endif