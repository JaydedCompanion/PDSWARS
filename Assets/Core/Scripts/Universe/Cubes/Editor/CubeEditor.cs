using UnityEditor;
using UnityEngine;

namespace PDSWARS.Core.Universe.Cubes.Editor {

    [CustomEditor(typeof(Cube))]
    public class CubeEditor : UnityEditor.Editor {
        
        private bool _autogenerate;
        private int _lastVolume;

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            _autogenerate = GUILayout.Toggle(_autogenerate, "Autogenerate");
            EditorGUI.BeginDisabledGroup(_autogenerate);
            if ((_autogenerate && _lastVolume != ((Cube)target).volume) || GUILayout.Button("Generate Cube"))
                ((Cube)target).Initialize();
            EditorGUI.EndDisabledGroup();
            _lastVolume = ((Cube)target).volume;
        }

    }

}