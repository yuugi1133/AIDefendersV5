using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SpawnGroup))]
public class SpawnGroupDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float gap = EditorGUIUtility.standardVerticalSpacing;

        if (!property.isExpanded)
            return line;

        SerializedProperty targets = property.FindPropertyRelative("targets");
        return line + gap
            + line + gap
            + line + gap
            + EditorGUI.GetPropertyHeight(targets, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect fold = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(fold, property.isExpanded, label, true);
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;
        float y = fold.yMax + EditorGUIUtility.standardVerticalSpacing;
        float line = EditorGUIUtility.singleLineHeight;
        float gap = EditorGUIUtility.standardVerticalSpacing;

        SerializedProperty spawnTime = property.FindPropertyRelative("spawnTime");
        SerializedProperty spawnerId = property.FindPropertyRelative("spawnerId");
        SerializedProperty targets = property.FindPropertyRelative("targets");

        Rect timeRect = new Rect(position.x, y, position.width, line);
        EditorGUI.PropertyField(timeRect, spawnTime);
        y += line + gap;

        Rect spawnerRect = new Rect(position.x, y, position.width, line);
        DrawSpawnerField(spawnerRect, spawnerId);
        y += line + gap;

        Rect targetsRect = new Rect(
            position.x,
            y,
            position.width,
            EditorGUI.GetPropertyHeight(targets, true)
        );
        EditorGUI.PropertyField(targetsRect, targets, true);

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    static void DrawSpawnerField(Rect rect, SerializedProperty spawnerId)
    {
        EnemySpawner[] spawners = Object.FindObjectsByType<EnemySpawner>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (spawners == null || spawners.Length == 0)
        {
            EditorGUI.PropertyField(rect, spawnerId, new GUIContent("Spawner"));
            return;
        }

        var ids = new System.Collections.Generic.List<string>();
        var labels = new System.Collections.Generic.List<string>();
        ids.Add("");
        labels.Add("(기본 스포너)");

        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] == null)
                continue;

            string id = spawners[i].Id;
            if (string.IsNullOrEmpty(id) || ids.Contains(id))
                continue;

            ids.Add(id);
            labels.Add(id);
        }

        string current = spawnerId.stringValue ?? "";
        if (!string.IsNullOrEmpty(current) && !ids.Contains(current))
        {
            ids.Add(current);
            labels.Add(current + " (씬에 없음)");
        }

        int index = Mathf.Max(0, ids.IndexOf(current));
        int next = EditorGUI.Popup(rect, "Spawner", index, labels.ToArray());
        if (next != index && next >= 0 && next < ids.Count)
            spawnerId.stringValue = ids[next];
    }
}
