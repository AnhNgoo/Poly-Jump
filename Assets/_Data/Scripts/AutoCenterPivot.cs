using UnityEngine;
using UnityEditor;
using System.IO;

public class AutoCenterPivot : Editor
{
    [MenuItem("Tools/Center Pivot and Save Mesh")]
    public static void CenterPivotAndSave()
    {
        // Tạo thư mục lưu Mesh nếu chưa có
        string folderPath = "Assets/GeneratedMeshes";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        foreach (GameObject obj in Selection.gameObjects)
        {
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Mesh mesh = mf.sharedMesh;
            Vector3 center = mesh.bounds.center;

            // 1. Tạo bản sao Mesh mới
            Mesh newMesh = Instantiate(mesh);
            Vector3[] vertices = newMesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] -= center;
            }

            newMesh.vertices = vertices;
            newMesh.RecalculateBounds();
            newMesh.RecalculateNormals();

            // 2. LƯU MESH THÀNH FILE ASSET (Quan trọng)
            string meshPath = $"{folderPath}/{obj.name}_{obj.GetInstanceID()}_Centered.asset";
            AssetDatabase.CreateAsset(newMesh, meshPath);
            AssetDatabase.SaveAssets();

            // 3. Gán Mesh đã lưu vào Object
            mf.sharedMesh = newMesh;
            obj.transform.position += obj.transform.TransformDirection(center);

            // Đánh dấu để Unity biết Scene đã thay đổi và cho phép Save
            EditorUtility.SetDirty(obj);

            Debug.Log($"Đã lưu Mesh mới tại: {meshPath}");
        }
    }
}