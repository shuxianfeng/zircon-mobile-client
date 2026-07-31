#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zircon.Mobile.Core.Assets;
using Zircon.Mobile.Game.World;
using Zircon.Mobile.UI;
using Zircon.Mobile.UI.Catalog;
using Zircon.Mobile.UI.CharacterSelect;
using Zircon.Mobile.UI.HUD;
using Zircon.Mobile.UI.Layout;
using Zircon.Mobile.UI.Login;

namespace Zircon.Mobile.Editor
{
    public static class ZirconProjectBootstrap
    {
        public const string ScenePath = "Assets/Scenes/ZirconMobile.unity";
        private static readonly Color Ink = new Color32(13, 18, 24, 255);
        private static readonly Color Panel = new Color32(28, 35, 43, 245);
        private static readonly Color Accent = new Color32(211, 163, 60, 255);
        private static readonly Color Muted = new Color32(155, 170, 181, 255);
        private static TMP_FontAsset font;

        [MenuItem("Zircon/Setup Mobile Project")]
        public static void SetupProject()
        {
            EnsureFolders();
            ConfigureSprites();
            EnsureTmpResources();
            font = EnsureFont();
            GameObject buttonPrefab = CreateButtonPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = CreateCamera();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var runtime = new GameObject("Runtime");
            ZirconWorldDebugRenderer world = runtime.AddComponent<ZirconWorldDebugRenderer>();
            ZirconMapDebugRenderer map = runtime.AddComponent<ZirconMapDebugRenderer>();
            ZirconProtocolProbeBehaviour session = runtime.AddComponent<ZirconProtocolProbeBehaviour>();
            ZirconSystemCatalogBehaviour catalog = runtime.AddComponent<ZirconSystemCatalogBehaviour>();
            runtime.AddComponent<ZirconAssetCatalogBehaviour>();
            Set(session, "worldRenderer", world);
            BindCatalog(catalog);

            GameObject canvas = CreateCanvas();
            GameObject safe = Rect("SafeArea", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.AddComponent<ZirconSafeAreaBehaviour>();

            GameObject login = CreateLogin(safe.transform, session, buttonPrefab);
            GameObject select = CreateCharacterSelect(safe.transform, session, buttonPrefab);
            GameObject hud = CreateHud(safe.transform, session, catalog, world, map, camera, buttonPrefab);

            ZirconUiFlowBehaviour flow = safe.AddComponent<ZirconUiFlowBehaviour>();
            Set(flow, "session", session);
            Set(flow, "loginRoot", login);
            Set(flow, "characterSelectRoot", select);
            Set(flow, "hudRoot", hud);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Zircon mobile scene and prefabs generated: " + ScenePath);
        }

        public static void SetupFromCommandLine() => SetupProject();

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Generated/UI"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
                AssetDatabase.CreateFolder("Assets/Generated", "UI");
            }
        }

        private static void ConfigureSprites()
        {
            string[] paths = {
                "Assets/Generated/Textures/Inventory/Inventory_00000_image.png",
                "Assets/Generated/Textures/MIcon/MIcon_00000_image.png",
                "Assets/Generated/Textures/MapData/Tiles30c/Tiles30c_00720_image.png"
            };
            foreach (string path in paths)
            {
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureTmpResources()
        {
            const string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath) != null) return;
            string package = Path.GetFullPath("Library/PackageCache/com.unity.textmeshpro@3.0.6/Package Resources/TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh();
        }

        private static TMP_FontAsset EnsureFont()
        {
            const string path = "Assets/Generated/UI/ZirconMobileFont.asset";
            TMP_FontAsset result = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (result != null) return result;
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Generated/UI/ZirconMobile.ttf");
            result = TMP_FontAsset.CreateFontAsset(source);
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        private static GameObject CreateButtonPrefab()
        {
            const string path = "Assets/Prefabs/ZirconButton.prefab";
            GameObject root = ButtonObject("ZirconButton", null, "Action", out _);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("WorldCamera", typeof(Camera), typeof(AudioListener));
            Camera camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2.4f;
            camera.backgroundColor = Ink;
            camera.transform.position = new Vector3(0, 0, -10);
            return camera;
        }

        private static GameObject CreateCanvas()
        {
            var canvas = new GameObject("MobileCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static GameObject CreateLogin(Transform parent, ZirconProtocolProbeBehaviour session, GameObject prefab)
        {
            GameObject root = Rect("Login", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bg = root.AddComponent<Image>(); bg.color = Ink;
            Text("Brand", root.transform, "ZIRCON LEGEND", 54, Accent, new Vector2(.5f,.78f), new Vector2(.5f,.78f), new Vector2(700,90));
            Text("Subtitle", root.transform, "MOBILE CLIENT", 18, Muted, new Vector2(.5f,.70f), new Vector2(.5f,.70f), new Vector2(500,50));
            TMP_InputField email = Input("Account", root.transform, "Account", new Vector2(.5f,.55f));
            TMP_InputField password = Input("Password", root.transform, "Password", new Vector2(.5f,.45f)); password.contentType = TMP_InputField.ContentType.Password;
            Button login = InstantiateButton(prefab, root.transform, "LOGIN", new Vector2(.5f,.33f), new Vector2(380,72));
            TMP_Text status = Text("Status", root.transform, "Disconnected", 18, Muted, new Vector2(.5f,.23f), new Vector2(.5f,.23f), new Vector2(700,50));
            ZirconLoginPanelBehaviour panel = root.AddComponent<ZirconLoginPanelBehaviour>();
            Set(panel,"session",session); Set(panel,"emailInput",email); Set(panel,"passwordInput",password); Set(panel,"loginButton",login); Set(panel,"statusText",status);
            return root;
        }

        private static GameObject CreateCharacterSelect(Transform parent, ZirconProtocolProbeBehaviour session, GameObject prefab)
        {
            GameObject root = Rect("CharacterSelect", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.AddComponent<Image>().color = Ink;
            Text("Title",root.transform,"SELECT CHARACTER",38,Accent,new Vector2(.5f,.82f),new Vector2(.5f,.82f),new Vector2(700,70));
            GameObject content = Rect("Characters",root.transform,new Vector2(.25f,.25f),new Vector2(.75f,.72f),Vector2.zero,Vector2.zero);
            var layout=content.AddComponent<VerticalLayoutGroup>(); layout.spacing=16; layout.childAlignment=TextAnchor.UpperCenter; layout.childControlHeight=false; layout.childControlWidth=true;
            Button template=InstantiateButton(prefab,content.transform,"Character",new Vector2(.5f,.5f),new Vector2(0,82)); template.gameObject.SetActive(false);
            TMP_Text empty=Text("Empty",root.transform,"No character",20,Muted,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(500,60));
            var panel=root.AddComponent<ZirconCharacterSelectPanelBehaviour>(); Set(panel,"session",session); Set(panel,"content",content.GetComponent<RectTransform>()); Set(panel,"characterButtonTemplate",template); Set(panel,"emptyText",empty);
            root.SetActive(false); return root;
        }

        private static GameObject CreateHud(Transform parent, ZirconProtocolProbeBehaviour session, ZirconSystemCatalogBehaviour catalog, ZirconWorldDebugRenderer world, ZirconMapDebugRenderer map, Camera camera, GameObject prefab)
        {
            GameObject root=Rect("HUD",parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            GameObject top=Rect("Status",root.transform,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-116),Vector2.zero); top.AddComponent<Image>().color=Panel;
            TMP_Text name=Text("Name",top.transform,"Player",24,Color.white,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(360,50),new Vector2(32,0));
            Slider hp=SliderObject("HP",top.transform,new Vector2(.22f,.62f),new Color32(190,55,55,255));
            Slider mp=SliderObject("MP",top.transform,new Vector2(.22f,.27f),new Color32(55,120,205,255));
            TMP_Text hpText=Text("HPText",top.transform,"0/0",16,Color.white,new Vector2(.22f,.62f),new Vector2(.22f,.62f),new Vector2(260,32));
            TMP_Text mpText=Text("MPText",top.transform,"0/0",16,Color.white,new Vector2(.22f,.27f),new Vector2(.22f,.27f),new Vector2(260,32));
            TMP_Text gold=Text("Gold",top.transform,"0",20,Accent,new Vector2(.67f,.5f),new Vector2(.67f,.5f),new Vector2(280,48));
            TMP_Text location=Text("Location",top.transform,"0,0",18,Muted,new Vector2(.86f,.5f),new Vector2(.86f,.5f),new Vector2(300,48));
            var main=root.AddComponent<ZirconMainHudBehaviour>(); Set(main,"session",session); Set(main,"nameLevelText",name); Set(main,"healthText",hpText); Set(main,"manaText",mpText); Set(main,"currencyText",gold); Set(main,"locationText",location); Set(main,"healthBar",hp); Set(main,"manaBar",mp); Set(main,"inGameRoot",root);

            string[] names={"Inventory","Skills","Quests","Map","NPC","Storage","Social","Mail","Market"};
            Type[] types={typeof(UI.Inventory.ZirconInventoryEquipmentPanelBehaviour),typeof(UI.Skills.ZirconSkillBookPanelBehaviour),typeof(UI.Quests.ZirconQuestPanelBehaviour),typeof(UI.Map.ZirconMiniMapPanelBehaviour),typeof(UI.Npc.ZirconNpcServicePanelBehaviour),typeof(UI.Storage.ZirconStoragePanelBehaviour),typeof(UI.Social.ZirconSocialTradePanelBehaviour),typeof(UI.Mail.ZirconMailPanelBehaviour),typeof(UI.Market.ZirconMarketPanelBehaviour)};
            var opens=new List<Button>(); var panels=new List<GameObject>(); var closes=new List<Button>();
            for(int i=0;i<names.Length;i++)
            {
                Button open=InstantiateButton(prefab,root.transform,names[i],new Vector2(.965f,.82f-i*.075f),new Vector2(170,54)); opens.Add(open);
                GameObject panel=Rect(names[i]+"Panel",root.transform,new Vector2(.16f,.12f),new Vector2(.84f,.88f),Vector2.zero,Vector2.zero); panel.AddComponent<Image>().color=Panel;
                Text("Title",panel.transform,names[i].ToUpperInvariant(),30,Accent,new Vector2(.08f,.92f),new Vector2(.08f,.92f),new Vector2(500,60));
                Button close=InstantiateButton(prefab,panel.transform,"CLOSE",new Vector2(.9f,.92f),new Vector2(150,52)); closes.Add(close);
                Component feature=panel.AddComponent(types[i]); Set(feature,"session",session); Set(feature,"catalog",catalog);
                panel.SetActive(false); panels.Add(panel);
            }
            var nav=root.AddComponent<ZirconPanelNavigationBehaviour>(); SetArray(nav,"openButtons",opens.ToArray()); SetArray(nav,"panels",panels.ToArray()); SetArray(nav,"closeButtons",closes.ToArray());
            var follow=camera.gameObject.AddComponent<ZirconWorldCameraFollow>(); Set(follow,"worldRenderer",world); Set(follow,"targetCamera",camera);
            root.SetActive(false); return root;
        }

        private static void BindCatalog(ZirconSystemCatalogBehaviour catalog)
        {
            Set(catalog,"itemManifest",AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Generated/Data/System/items.manifest.json"));
            Set(catalog,"magicManifest",AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Generated/Data/System/magics.manifest.json"));
            Set(catalog,"npcPageManifest",AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Generated/Data/System/npc-pages.manifest.json"));
            Set(catalog,"questManifest",AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Generated/Data/System/quests.manifest.json"));
            Set(catalog,"mapManifest",AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Generated/Data/System/maps.manifest.json"));
        }

        private static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        { var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); var r=(RectTransform)go.transform; r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax; return go; }
        private static TMP_Text Text(string name,Transform parent,string value,float size,Color color,Vector2 min,Vector2 max,Vector2 dimensions,Vector2? position=null)
        { GameObject go=Rect(name,parent,min,max,Vector2.zero,Vector2.zero); var r=(RectTransform)go.transform;r.sizeDelta=dimensions;r.anchoredPosition=position??Vector2.zero; var t=go.AddComponent<TextMeshProUGUI>();t.text=value;t.font=font;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;t.enableWordWrapping=false;return t; }
        private static GameObject ButtonObject(string name,Transform parent,string label,out Button button)
        { GameObject go=Rect(name,parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,Vector2.zero); var image=go.AddComponent<Image>();image.color=Accent;button=go.AddComponent<Button>(); Text("Label",go.transform,label,18,Ink,Vector2.zero,Vector2.one,Vector2.zero);return go; }
        private static Button InstantiateButton(GameObject prefab,Transform parent,string label,Vector2 anchor,Vector2 size)
        { GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.name=label;var r=(RectTransform)go.transform;r.anchorMin=anchor;r.anchorMax=anchor;r.sizeDelta=size;r.anchoredPosition=Vector2.zero;go.GetComponentInChildren<TMP_Text>().text=label;return go.GetComponent<Button>(); }
        private static TMP_InputField Input(string name,Transform parent,string placeholder,Vector2 anchor)
        { GameObject go=Rect(name,parent,anchor,anchor,Vector2.zero,Vector2.zero);var r=(RectTransform)go.transform;r.sizeDelta=new Vector2(560,70);go.AddComponent<Image>().color=Panel;var input=go.AddComponent<TMP_InputField>();GameObject area=Rect("TextArea",go.transform,Vector2.zero,Vector2.one,new Vector2(20,8),new Vector2(-20,-8));area.AddComponent<RectMask2D>();TMP_Text hint=Text("Placeholder",area.transform,placeholder,20,Muted,Vector2.zero,Vector2.one,Vector2.zero);TMP_Text value=Text("Text",area.transform,"",20,Color.white,Vector2.zero,Vector2.one,Vector2.zero);hint.alignment=value.alignment=TextAlignmentOptions.MidlineLeft;input.textViewport=(RectTransform)area.transform;input.textComponent=(TextMeshProUGUI)value;input.placeholder=hint;return input; }
        private static Slider SliderObject(string name,Transform parent,Vector2 anchor,Color fillColor)
        { GameObject go=Rect(name,parent,anchor,anchor,Vector2.zero,Vector2.zero);var r=(RectTransform)go.transform;r.sizeDelta=new Vector2(300,22);go.AddComponent<Image>().color=new Color32(62,70,78,255);var slider=go.AddComponent<Slider>();GameObject fill=Rect("Fill",go.transform,Vector2.zero,Vector2.one,new Vector2(2,2),new Vector2(-2,-2));fill.AddComponent<Image>().color=fillColor;slider.fillRect=(RectTransform)fill.transform;slider.transition=Selectable.Transition.None;return slider; }
        private static void Set(UnityEngine.Object target,string property,UnityEngine.Object value){var so=new SerializedObject(target);var p=so.FindProperty(property);if(p!=null){p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}}
        private static void SetArray(UnityEngine.Object target,string property,UnityEngine.Object[] values){var so=new SerializedObject(target);var p=so.FindProperty(property);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    }
}
#endif
