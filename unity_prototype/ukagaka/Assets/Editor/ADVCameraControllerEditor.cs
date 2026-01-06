using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ADVCameraController))]
[CanEditMultipleObjects]
public class ADVCameraControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // デフォルトのInspectorを描画
        DrawDefaultInspector();

        // ターゲットが正しく取得できているか確認
        if (target == null)
        {
            EditorGUILayout.HelpBox("ターゲットが見つかりません", MessageType.Error);
            return;
        }

        var controller = target as ADVCameraController;
        if (controller == null)
        {
            EditorGUILayout.HelpBox("ADVCameraControllerコンポーネントが見つかりません", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("プリセット操作", EditorStyles.boldLabel);
        
        if (GUILayout.Button("現在の設定を保存", GUILayout.Height(30)))
        {
            controller.SaveCurrentPreset();
            EditorUtility.DisplayDialog("保存完了", "プリセットを保存しました", "OK");
        }
    }
}
