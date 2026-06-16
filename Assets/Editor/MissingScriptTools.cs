#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 编辑器工具:扫描/移除当前打开场景里"缺失脚本(Missing Mono Script)"的组件。
// 菜单栏 Tools > Missing Scripts。
public static class MissingScriptTools
{
    [MenuItem("Tools/Missing Scripts/扫描当前场景")]
    static void Scan()
    {
        var all = Object.FindObjectsOfType<GameObject>(true); // 含未激活
        int total = 0;
        foreach (var go in all)
        {
            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null)
                {
                    Debug.LogWarning($"[缺失脚本] {GetPath(go)} (第 {i} 个组件)", go);
                    total++;
                }
            }
        }
        Debug.Log(total == 0
            ? "扫描完成:没有发现缺失脚本。说明那组报错是 Play 切换时的选中态噪声,进 Play 前点一下 Hierarchy 空白处取消选中即可。"
            : $"扫描完成:发现 {total} 个缺失脚本组件(点上面的警告可高亮定位)。用菜单 Tools > Missing Scripts > 移除当前场景中全部 清掉。");
    }

    [MenuItem("Tools/Missing Scripts/移除当前场景中全部")]
    static void RemoveAll()
    {
        var all = Object.FindObjectsOfType<GameObject>(true);
        int removed = 0;
        foreach (var go in all)
        {
            int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (n > 0)
            {
                Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                EditorUtility.SetDirty(go);
            }
        }
        Debug.Log($"已移除 {removed} 个缺失脚本组件。记得 Ctrl+S 保存场景。");
    }

    static string GetPath(GameObject go)
    {
        string p = go.name;
        var t = go.transform;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
#endif
