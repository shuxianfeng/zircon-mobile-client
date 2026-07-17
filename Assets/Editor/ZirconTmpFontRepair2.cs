#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Zircon.Mobile.Editor
{
    public static class ZirconTmpFontRepair2
    {
        public static void RepairFromCommandLine()
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Generated/UI/ZirconMobile.ttf");
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Generated/UI/ZirconMobileFont.asset");
            TMP_FontAsset generated = source == null ? null : TMP_FontAsset.CreateFontAsset(source);
            if (existing == null || generated == null || generated.atlasTexture == null || generated.material == null)
                throw new System.InvalidOperationException("Unable to rebuild the mobile TMP font.");

            Texture2D atlas = Object.Instantiate(generated.atlasTexture);
            atlas.name = "ZirconMobileFont Atlas";
            Material material = Object.Instantiate(generated.material);
            material.name = "ZirconMobileFont Material";
            material.mainTexture = atlas;
            AssetDatabase.AddObjectToAsset(atlas, existing);
            AssetDatabase.AddObjectToAsset(material, existing);

            EditorUtility.CopySerialized(generated, existing);
            SerializedObject serialized = new SerializedObject(existing);
            SerializedProperty textures = serialized.FindProperty("m_AtlasTextures");
            textures.arraySize = 1;
            textures.GetArrayElementAtIndex(0).objectReferenceValue = atlas;
            serialized.FindProperty("material").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Object.DestroyImmediate(generated);
            Debug.Log("Zircon TextMesh Pro font atlas repaired and persisted.");
        }
    }
}
#endif
