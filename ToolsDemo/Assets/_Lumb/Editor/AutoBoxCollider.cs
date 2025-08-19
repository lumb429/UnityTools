using UnityEngine;
using UnityEditor;
namespace Lumb
{
    /// <summary>
    /// 自动添加适合大小的box碰撞
    /// </summary>
    public class AutoBoxCollider : EditorWindow
    {
        [MenuItem("GameObject/Add Perfect Fit Box Collider", false, 20)]
        static void AddPerfectFitBoxCollider()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("No object selected!");
                return;
            }

            foreach (GameObject selectedObj in Selection.gameObjects)
            {
                AddBoxColliderToObject(selectedObj);
            }
        }

        [MenuItem("GameObject/Add Perfect Fit Box Collider", true)]
        static bool ValidateAddPerfectFitBoxCollider()
        {
            return Selection.activeGameObject != null;
        }

        static void AddBoxColliderToObject(GameObject targetObj)
        {
            Undo.RecordObject(targetObj, "Add Perfect Fit Box Collider");

            // 移除已有的BoxCollider
            BoxCollider existingCollider = targetObj.GetComponent<BoxCollider>();
            if (existingCollider != null)
            {
                Undo.DestroyObjectImmediate(existingCollider);
            }

            // 获取所有渲染器（包括MeshRenderer和SkinnedMeshRenderer）
            Renderer[] renderers = targetObj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"No renderers found on {targetObj.name} or its children", targetObj);
                return;
            }

            // 计算世界空间的总包围盒
            Bounds worldBounds = new Bounds();
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                // 跳过没有实际网格的渲染器
                if (renderer is MeshRenderer && renderer.GetComponent<MeshFilter>() == null)
                    continue;

                if (renderer is SkinnedMeshRenderer smr && smr.sharedMesh == null)
                    continue;

                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                Debug.LogWarning($"No valid renderers found on {targetObj.name} or its children", targetObj);
                return;
            }

            // 添加BoxCollider
            BoxCollider boxCollider = Undo.AddComponent<BoxCollider>(targetObj);

            // 计算在目标物体局部空间中的中心点和大小
            Vector3 localCenter = targetObj.transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = targetObj.transform.InverseTransformVector(worldBounds.size);

            boxCollider.center = localCenter;
            boxCollider.size = localSize;

            Debug.Log($"Added BoxCollider to {targetObj.name} with size {localSize} and center {localCenter}", targetObj);
        }
    }
}
