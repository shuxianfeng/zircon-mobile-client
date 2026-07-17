#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Zircon.Mobile.Editor
{
    public static class ZirconTmpFontRepair
    {
        public static void RepairFromCommandLine()
        {
            const string sourcePath = "Assets/Generated/UI/ZirconMobile.ttf";
            const string assetPath = "Assets/Generated/UI/ZirconMobileFont.asset";
            Font source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (source == null || existing == null)
                throw new System.InvalidOperationException("Zircon mobile source font or TMP font asset is missing.");

            TMP_FontAsset generated = TMP_FontAsset.CreateFontAsset(source);
            if (generated == null || generated.atlasTexture == null || generated.material == null)
                throw new System.InvalidOperationException("TextMesh Pro could not generate the mobile font atlas.");

            Texture2D atlas = Object.Instantiate(generated.atlasTexture);
            atlas.name = "ZirconMobileFont Atlas";
            Material material = Object.Instantiate(generated.material);
            material.name = "ZirconMobileFont Material";
            material.mainTexture = atlas;

            EditorUtility.CopySerialized(generated, existing);
            SerializedObject serialized = new SerializedObject(existing);
            SerializedProperty textures = serialized.FindProperty("m_AtlasTextures");
            textures.arraySize = 1;
            textures.GetArrayElementAtIndex(0).objectReferenceValue = atlas;
            serialized.FindProperty("m_Material").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.AddObjectToAsset(atlas, existing);
            AssetDatabase.AddObjectToAsset(material, existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Object.DestroyImmediate(generated);
            Debug.Log("Zircon TextMesh Pro font atlas repaired and persisted.");
        }
    }
}
#endif
