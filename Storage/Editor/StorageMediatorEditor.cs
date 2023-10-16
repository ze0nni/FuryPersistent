#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Fury.Storage.Editor
{
    [CustomEditor(typeof(StorageMediator))]
    public class StorageMediatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var mediator = (StorageMediator)target;
            var controller = mediator.Controller;

            foreach (var key in controller.Keys.List)
            {
                GUILayout.Label($"{key.Id}: {key.Data}");
            }
        }
    }
}
#endif