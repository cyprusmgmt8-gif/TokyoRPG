using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Rebuilds the furnished bedroom from placeholder shapes.
/// Menu: Tokyo RPG / Build Bedroom Scene.
/// Each furniture group is a named empty object, so a finished 3D model can replace its child shapes later.
/// </summary>
public static class BedroomSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Bedroom/Bedroom.unity";
    const string MaterialFolder = "Assets/Materials/Bedroom";
    const string TextureFolder = "Assets/Materials/Bedroom/Textures";
    static bool batchShapes = true;

    [MenuItem("Tokyo RPG/Build Bedroom Scene")]
    public static void Build()
    {
        Material floor = Mat("WoodFloor", new Color(0.72f, 0.58f, 0.40f), "WoodFloor", 0.22f, 0f, new Vector2(3f, 3f));
        Material wall = Mat("Wall", new Color(0.93f, 0.90f, 0.85f), "Wall", 0.05f, 0f, new Vector2(2f, 2f));
        Material ceiling = Mat("Ceiling", new Color(0.96f, 0.95f, 0.93f), "Wall", 0.04f, 0f, new Vector2(2f, 2f));
        Material wood = Mat("Wood", new Color(0.62f, 0.42f, 0.24f), "Wood", 0.18f, 0f, Vector2.one);
        Material woodDark = Mat("WoodDark", new Color(0.36f, 0.22f, 0.12f), "Wood", 0.16f, 0f, Vector2.one);
        Material bedding = Mat("Bedding", new Color(0.22f, 0.28f, 0.42f), "Fabric", 0.08f, 0f, new Vector2(2f, 2f));
        Material pillow = Mat("Pillow", new Color(0.93f, 0.92f, 0.90f), "Fabric", 0.06f, 0f, Vector2.one);
        Material sheet = Mat("Sheet", new Color(0.90f, 0.90f, 0.88f), "Fabric", 0.06f, 0f, Vector2.one);
        Material metal = Mat("Metal", new Color(0.75f, 0.76f, 0.78f), "Metal", 0.55f, 0.7f, new Vector2(1f, 2f));
        Material plastic = Mat("Plastic", new Color(0.92f, 0.92f, 0.90f), null, 0.35f, 0f, Vector2.one);
        Material screen = Mat("Screen", new Color(0.05f, 0.08f, 0.12f), null, 0.65f, 0f, Vector2.one, new Color(0.03f, 0.08f, 0.12f));
        Material sky = Mat("Sky", new Color(0.55f, 0.68f, 0.82f), null, 0.02f, 0f, Vector2.one);
        Material building = Mat("Building", new Color(0.55f, 0.56f, 0.58f), null, 0.1f, 0f, Vector2.one);
        Material curtain = Mat("Curtain", new Color(0.75f, 0.72f, 0.66f), "Fabric", 0.05f, 0f, new Vector2(1f, 2f));
        Material rug = Mat("Rug", new Color(0.45f, 0.32f, 0.28f), "Rug", 0.05f, 0f, Vector2.one);
        Material plant = Mat("Plant", new Color(0.22f, 0.40f, 0.24f), null, 0.1f, 0f, Vector2.one);
        Material lamp = Mat("Lamp", new Color(1f, 0.90f, 0.70f), null, 0.2f, 0f, Vector2.one, new Color(1.4f, 1.05f, 0.55f));
        Material bookBlue = Mat("BookBlue", new Color(0.18f, 0.32f, 0.52f), null, 0.15f, 0f, Vector2.one);
        Material bookRed = Mat("BookRed", new Color(0.55f, 0.18f, 0.18f), null, 0.15f, 0f, Vector2.one);
        Material bookGreen = Mat("BookGreen", new Color(0.18f, 0.38f, 0.28f), null, 0.15f, 0f, Vector2.one);
        Material frame = Mat("Frame", new Color(0.25f, 0.18f, 0.12f), null, 0.2f, 0f, Vector2.one);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.42f, 0.38f);

        Transform bedroom = Group("Bedroom", null);
        Transform structure = Group("Structure", bedroom);
        Transform bed = Group("Bed", bedroom);
        Transform desk = Group("Desk", bedroom);
        Transform chair = Group("Chair", bedroom);
        Transform wardrobe = Group("Wardrobe", bedroom);
        Transform refrigerator = Group("Refrigerator", bedroom);
        Transform television = Group("Television", bedroom);
        Transform shelves = Group("Shelves", bedroom);
        Transform window = Group("Window", bedroom);
        Transform door = Group("Door", bedroom);
        Transform lights = Group("Lights", bedroom);
        Transform decor = Group("Decor", bedroom);

        BuildStructure(structure, floor, wall, ceiling);
        BuildWindow(window, sky, building, curtain, plastic);
        batchShapes = false;
        BuildDoor(door, wood, woodDark, metal);
        BuildBed(bed, wood, woodDark, sheet, bedding, pillow);
        BuildDesk(desk, wood, woodDark, sheet, plastic, metal, lamp);
        BuildWardrobe(wardrobe, wood, woodDark, metal);
        BuildRefrigerator(refrigerator, plastic, metal);
        BuildTelevision(television, woodDark, frame, screen);
        batchShapes = true;
        BuildChair(chair, wood);
        BuildShelves(shelves, wood, bookBlue, bookRed, bookGreen);
        BuildLightFixtures(lights, lamp, metal);
        BuildDecor(decor, rug, woodDark, plant, plastic, frame, pillow, sky);

        BoxCollider bedCollider = bed.gameObject.AddComponent<BoxCollider>();
        bedCollider.center = new Vector3(-1.48f, 0.55f, 0.45f);
        bedCollider.size = new Vector3(1.2f, 1.1f, 2.15f);
        BoxCollider lightCollider = lights.gameObject.AddComponent<BoxCollider>();
        lightCollider.center = new Vector3(0f, 2.22f, 0.15f);
        lightCollider.size = new Vector3(0.5f, 0.25f, 0.5f);

        LinkVocabulary(bed, "Bed", "bedroom.bed");
        LinkVocabulary(desk, "Desk", "bedroom.desk");
        LinkVocabulary(chair, "Chair", "bedroom.chair");
        LinkVocabulary(door, "Door", "bedroom.door");
        LinkVocabulary(window, "Window", "bedroom.window");
        LinkVocabulary(wardrobe, "Wardrobe", "bedroom.wardrobe");
        LinkVocabulary(refrigerator, "Refrigerator", "bedroom.refrigerator");
        LinkVocabulary(television, "Television", "bedroom.television");
        LinkVocabulary(shelves, "Shelf", "bedroom.shelf");
        LinkVocabulary(lights, "Light", "bedroom.light");
        LinkNamed(bedroom, "Curtain Left", "Curtain", "bedroom.curtain");
        LinkNamed(bedroom, "Rug", "Rug", "bedroom.rug");
        LinkNamed(bedroom, "Clock", "Clock", "bedroom.clock");
        LinkNamed(bedroom, "Plant Pot", "Plant", "bedroom.plant");
        LinkNamed(bedroom, "Nightstand", "Alarm Clock", "bedroom.nightstand");

        CreateLights();
        CreatePlayer();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Furnished bedroom saved to " + ScenePath);
    }

    static void BuildStructure(Transform parent, Material floor, Material wall, Material ceiling)
    {
        Cube("Floor", new Vector3(0f, -0.08f, 0f), new Vector3(4.5f, 0.16f, 4.28f), floor, parent, true);
        Cube("Ceiling", new Vector3(0f, 2.44f, 0f), new Vector3(4.5f, 0.08f, 4.28f), ceiling, parent, true);
        Cube("Wall Left", new Vector3(-2.16f, 1.2f, 0f), new Vector3(0.12f, 2.4f, 4.28f), wall, parent, true);
        Cube("Wall Right", new Vector3(2.16f, 1.2f, 0f), new Vector3(0.12f, 2.4f, 4.28f), wall, parent, true);
        Cube("Wall Back Left", new Vector3(-0.975f, 1.2f, 1.96f), new Vector3(2.25f, 2.4f, 0.12f), wall, parent, true);
        Cube("Wall Back Right", new Vector3(1.825f, 1.2f, 1.96f), new Vector3(0.55f, 2.4f, 0.12f), wall, parent, true);
        Cube("Wall Back Below Window", new Vector3(0.85f, 0.45f, 1.96f), new Vector3(1.4f, 0.9f, 0.12f), wall, parent, true);
        Cube("Wall Back Above Window", new Vector3(0.85f, 2.2f, 1.96f), new Vector3(1.4f, 0.4f, 0.12f), wall, parent, true);
        Cube("Wall Front Left", new Vector3(-1.75f, 1.2f, -1.96f), new Vector3(0.7f, 2.4f, 0.12f), wall, parent, true);
        Cube("Wall Front Right", new Vector3(0.8f, 1.2f, -1.96f), new Vector3(2.6f, 2.4f, 0.12f), wall, parent, true);
        Cube("Wall Front Above Door", new Vector3(-0.95f, 2.21f, -1.96f), new Vector3(0.9f, 0.38f, 0.12f), wall, parent, true);
    }

    static void BuildWindow(Transform parent, Material sky, Material building, Material curtain, Material frameMaterial)
    {
        Cube("Glass", new Vector3(0.85f, 1.45f, 1.90f), new Vector3(1.32f, 1.02f, 0.04f), sky, parent, true);
        Cube("Frame Left", new Vector3(0.18f, 1.45f, 1.84f), new Vector3(0.06f, 1.12f, 0.06f), frameMaterial, parent, false);
        Cube("Frame Right", new Vector3(1.52f, 1.45f, 1.84f), new Vector3(0.06f, 1.12f, 0.06f), frameMaterial, parent, false);
        Cube("Frame Top", new Vector3(0.85f, 1.98f, 1.84f), new Vector3(1.4f, 0.06f, 0.06f), frameMaterial, parent, false);
        Cube("Frame Bottom", new Vector3(0.85f, 0.92f, 1.84f), new Vector3(1.4f, 0.06f, 0.06f), frameMaterial, parent, false);
        Cube("Mullion Vertical", new Vector3(0.85f, 1.45f, 1.84f), new Vector3(0.04f, 1.02f, 0.04f), frameMaterial, parent, false);
        Cube("Mullion Horizontal", new Vector3(0.85f, 1.45f, 1.84f), new Vector3(1.28f, 0.04f, 0.04f), frameMaterial, parent, false);
        Cube("Curtain Left", new Vector3(0.28f, 1.42f, 1.78f), new Vector3(0.14f, 1.05f, 0.05f), curtain, parent, true);
        Cube("Curtain Right", new Vector3(1.42f, 1.42f, 1.78f), new Vector3(0.14f, 1.05f, 0.05f), curtain, parent, false);
        Cylinder("Curtain Rod", new Vector3(0.85f, 2.02f, 1.76f), new Vector3(0.03f, 0.72f, 0.03f), frameMaterial, parent, false, new Vector3(0f, 0f, 90f));
        Cube("Outside Sky", new Vector3(0.85f, 1.5f, 2.4f), new Vector3(1.7f, 1.4f, 0.05f), sky, parent, false);
        Cube("Outside Building Left", new Vector3(0.15f, 1.3f, 3.3f), new Vector3(1.1f, 2.6f, 0.35f), building, parent, false);
        Cube("Outside Building Right", new Vector3(1.65f, 1.7f, 3.9f), new Vector3(0.9f, 3.2f, 0.35f), building, parent, false);
    }

    static void BuildDoor(Transform parent, Material wood, Material woodDark, Material metal)
    {
        Cube("Door Slab", new Vector3(-0.95f, 1.0f, -1.88f), new Vector3(0.84f, 2.0f, 0.05f), wood, parent, true);
        Cube("Frame Left", new Vector3(-1.40f, 1.05f, -1.84f), new Vector3(0.06f, 2.1f, 0.08f), woodDark, parent, false);
        Cube("Frame Right", new Vector3(-0.50f, 1.05f, -1.84f), new Vector3(0.06f, 2.1f, 0.08f), woodDark, parent, false);
        Cube("Frame Top", new Vector3(-0.95f, 2.08f, -1.84f), new Vector3(0.96f, 0.08f, 0.08f), woodDark, parent, false);
        Cube("Handle", new Vector3(-0.58f, 1.0f, -1.84f), new Vector3(0.04f, 0.14f, 0.04f), metal, parent, false);
    }

    static void BuildBed(Transform parent, Material wood, Material woodDark, Material sheet, Material bedding, Material pillow)
    {
        Cube("Frame", new Vector3(-1.48f, 0.14f, 0.45f), new Vector3(1.15f, 0.28f, 2.05f), woodDark, parent, true);
        Cube("Headboard", new Vector3(-1.48f, 0.62f, 1.42f), new Vector3(1.15f, 0.7f, 0.06f), wood, parent, true);
        Cube("Mattress", new Vector3(-1.48f, 0.34f, 0.45f), new Vector3(1.08f, 0.14f, 1.92f), sheet, parent, false);
        Cube("Duvet", new Vector3(-1.48f, 0.42f, 0.15f), new Vector3(1.1f, 0.07f, 1.25f), bedding, parent, false);
        Cube("Pillow Left", new Vector3(-1.68f, 0.44f, 1.22f), new Vector3(0.38f, 0.1f, 0.48f), pillow, parent, false);
        Cube("Pillow Right", new Vector3(-1.28f, 0.44f, 1.22f), new Vector3(0.38f, 0.1f, 0.48f), pillow, parent, false);
    }

    static void BuildDesk(Transform parent, Material wood, Material woodDark, Material sheet, Material plastic, Material metal, Material lamp)
    {
        Cube("Desk Body", new Vector3(0.85f, 0.36f, 1.55f), new Vector3(1.3f, 0.72f, 0.52f), wood, parent, true);
        Cube("Notebook", new Vector3(0.55f, 0.74f, 1.48f), new Vector3(0.28f, 0.02f, 0.22f), sheet, parent, false);
        Cylinder("Pen Cup", new Vector3(1.15f, 0.8f, 1.62f), new Vector3(0.07f, 0.045f, 0.07f), plastic, parent, false);
        Cube("Lamp Base", new Vector3(1.25f, 0.76f, 1.68f), new Vector3(0.12f, 0.04f, 0.12f), woodDark, parent, false);
        Cube("Lamp Stem", new Vector3(1.25f, 0.9f, 1.68f), new Vector3(0.03f, 0.24f, 0.03f), metal, parent, false);
        Cylinder("Lamp Shade", new Vector3(1.25f, 1.05f, 1.68f), new Vector3(0.16f, 0.03f, 0.16f), lamp, parent, false);
    }

    static void BuildChair(Transform parent, Material wood)
    {
        Cube("Seat", new Vector3(0.85f, 0.46f, 0.95f), new Vector3(0.46f, 0.06f, 0.46f), wood, parent, true);
        Cube("Back", new Vector3(0.85f, 0.78f, 1.15f), new Vector3(0.46f, 0.5f, 0.05f), wood, parent, true);
        Cube("Leg 1", new Vector3(0.68f, 0.22f, 0.78f), new Vector3(0.04f, 0.44f, 0.04f), wood, parent, false);
        Cube("Leg 2", new Vector3(1.02f, 0.22f, 0.78f), new Vector3(0.04f, 0.44f, 0.04f), wood, parent, false);
        Cube("Leg 3", new Vector3(0.68f, 0.22f, 1.12f), new Vector3(0.04f, 0.44f, 0.04f), wood, parent, false);
        Cube("Leg 4", new Vector3(1.02f, 0.22f, 1.12f), new Vector3(0.04f, 0.44f, 0.04f), wood, parent, false);
    }

    static void BuildWardrobe(Transform parent, Material wood, Material woodDark, Material metal)
    {
        Cube("Body", new Vector3(1.82f, 0.95f, -0.55f), new Vector3(0.48f, 1.9f, 0.8f), wood, parent, true);
        Cube("Door Seam", new Vector3(1.56f, 0.95f, -0.55f), new Vector3(0.02f, 1.7f, 0.02f), woodDark, parent, false);
        Cube("Handle", new Vector3(1.55f, 0.95f, -0.35f), new Vector3(0.03f, 0.16f, 0.03f), metal, parent, false);
    }

    static void BuildRefrigerator(Transform parent, Material plastic, Material metal)
    {
        Cube("Body", new Vector3(1.82f, 0.58f, -1.48f), new Vector3(0.5f, 1.16f, 0.48f), plastic, parent, true);
        Cube("Door Line", new Vector3(1.55f, 0.72f, -1.48f), new Vector3(0.02f, 0.7f, 0.42f), metal, parent, false);
        Cube("Handle", new Vector3(1.54f, 0.85f, -1.30f), new Vector3(0.03f, 0.22f, 0.03f), metal, parent, false);
    }

    static void BuildTelevision(Transform parent, Material woodDark, Material frame, Material screen)
    {
        Cube("Stand", new Vector3(1.68f, 0.22f, 0.7f), new Vector3(0.4f, 0.44f, 0.85f), woodDark, parent, true);
        Cube("Bezel", new Vector3(1.52f, 0.75f, 0.7f), new Vector3(0.06f, 0.5f, 0.75f), frame, parent, true);
        Cube("Screen", new Vector3(1.48f, 0.75f, 0.7f), new Vector3(0.02f, 0.42f, 0.64f), screen, parent, false);
    }

    static void BuildShelves(Transform parent, Material wood, Material bookBlue, Material bookRed, Material bookGreen)
    {
        Cube("Shelf Low", new Vector3(-1.05f, 1.35f, 1.82f), new Vector3(0.85f, 0.04f, 0.22f), wood, parent, true);
        Cube("Shelf High", new Vector3(-1.05f, 1.75f, 1.82f), new Vector3(0.85f, 0.04f, 0.22f), wood, parent, true);
        Material[] books = { bookBlue, bookRed, bookGreen, bookBlue };
        for (int i = 0; i < books.Length; i++)
        {
            float x = -1.32f + (i * 0.18f);
            Cube("Book Low " + (i + 1), new Vector3(x, 1.48f, 1.82f), new Vector3(0.08f, 0.22f, 0.16f), books[i], parent, false);
            Cube("Book High " + (i + 1), new Vector3(x, 1.88f, 1.82f), new Vector3(0.08f, 0.2f, 0.16f), books[(i + 1) % books.Length], parent, false);
        }
    }

    static void BuildLightFixtures(Transform parent, Material lamp, Material metal)
    {
        Cylinder("Ceiling Shade", new Vector3(0f, 2.22f, 0.15f), new Vector3(0.42f, 0.025f, 0.42f), lamp, parent, false);
        Cube("Ceiling Cord", new Vector3(0f, 2.32f, 0.15f), new Vector3(0.02f, 0.16f, 0.02f), metal, parent, false);
    }

    static void BuildDecor(Transform parent, Material rug, Material woodDark, Material plant, Material plastic, Material frame, Material pillow, Material sky)
    {
        GameObject rugObject = Cube("Rug", new Vector3(0.05f, 0.008f, 0.15f), new Vector3(2.4f, 0.016f, 1.7f), rug, parent, true);
        rugObject.GetComponent<Collider>().isTrigger = true;
        Cylinder("Plant Pot", new Vector3(-0.15f, 0.08f, 1.58f), new Vector3(0.18f, 0.08f, 0.18f), woodDark, parent, true);
        Sphere("Plant", new Vector3(-0.15f, 0.28f, 1.58f), new Vector3(0.24f, 0.18f, 0.24f), plant, parent, false);
        Cylinder("Waste Bin", new Vector3(1.28f, 0.12f, 0.25f), new Vector3(0.18f, 0.12f, 0.18f), plastic, parent, true);
        Cube("Clock", new Vector3(-2.04f, 1.65f, 0.15f), new Vector3(0.04f, 0.28f, 0.28f), frame, parent, true);
        Cube("Clock Face", new Vector3(-2.01f, 1.65f, 0.15f), new Vector3(0.02f, 0.2f, 0.2f), pillow, parent, false);
        Cube("Air Conditioner", new Vector3(2.02f, 2.1f, 1.15f), new Vector3(0.16f, 0.28f, 0.72f), plastic, parent, false);
        Cube("Picture Frame", new Vector3(0.9f, 1.45f, -1.88f), new Vector3(0.55f, 0.4f, 0.03f), frame, parent, false);
        Cube("Picture", new Vector3(0.9f, 1.45f, -1.86f), new Vector3(0.42f, 0.28f, 0.01f), sky, parent, false);
        Cube("Light Switch", new Vector3(-0.35f, 1.2f, -1.88f), new Vector3(0.08f, 0.12f, 0.02f), plastic, parent, false);
        Cube("Nightstand", new Vector3(-0.62f, 0.22f, 1.25f), new Vector3(0.4f, 0.44f, 0.36f), woodDark, parent, true);
        Cube("Alarm Clock", new Vector3(-0.62f, 0.47f, 1.25f), new Vector3(0.12f, 0.06f, 0.08f), plastic, parent, false);
    }

    static void CreateLights()
    {
        GameObject sunObject = new GameObject("Directional Light");
        sunObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        sunObject.transform.position = new Vector3(0f, 3f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.96f, 0.9f);
        sun.intensity = 0.45f;
        sun.shadows = LightShadows.None;

        GameObject roomObject = new GameObject("Room Light");
        roomObject.transform.position = new Vector3(0f, 2.15f, 0.15f);
        Light room = roomObject.AddComponent<Light>();
        room.type = LightType.Point;
        room.color = new Color(1f, 0.93f, 0.82f);
        room.intensity = 12f;
        room.range = 7f;
        room.shadows = LightShadows.None;

        GameObject volumeObject = new GameObject("Global Volume");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
    }

    static void CreatePlayer()
    {
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0.45f, 0.05f, -0.95f);

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.28f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.2f;
        controller.skinWidth = 0.05f;
        controller.slopeLimit = 45f;

        GameObject cameraObject = new GameObject("Camera");
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.05f;
        camera.fieldOfView = 70f;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();

        FirstPersonController movement = player.AddComponent<FirstPersonController>();
        SerializedObject serializedMovement = new SerializedObject(movement);
        serializedMovement.FindProperty("eyeCamera").objectReferenceValue = cameraObject.transform;
        serializedMovement.ApplyModifiedPropertiesWithoutUndo();

        PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
        SerializedObject serializedInteractor = new SerializedObject(interactor);
        serializedInteractor.FindProperty("eye").objectReferenceValue = cameraObject.transform;
        serializedInteractor.ApplyModifiedPropertiesWithoutUndo();

        VocabularyLibrary library = player.AddComponent<VocabularyLibrary>();
        SerializedObject serializedLibrary = new SerializedObject(library);
        serializedLibrary.FindProperty("catalog").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<VocabularyCatalog>("Assets/Data/Vocabulary/BedroomVocabulary.asset");
        serializedLibrary.FindProperty("interfaceLanguage").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<InterfaceLanguage>("Assets/Data/Localization/InterfaceLanguage.asset");
        serializedLibrary.ApplyModifiedPropertiesWithoutUndo();
        player.AddComponent<PersonalDictionary>();
        player.AddComponent<PlayerProgression>();

        GrammarLibrary grammar = player.AddComponent<GrammarLibrary>();
        SerializedObject serializedGrammar = new SerializedObject(grammar);
        serializedGrammar.FindProperty("catalog").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GrammarCatalog>("Assets/Data/Grammar/BedroomGrammar.asset");
        serializedGrammar.ApplyModifiedPropertiesWithoutUndo();
        player.AddComponent<GrammarProgress>();
        player.AddComponent<RevisionProgress>();
        player.AddComponent<BedroomMapButton>();
        player.AddComponent<TouchControls>();
    }

    static void LinkNamed(Transform root, string objectName, string prompt, string vocabularyId)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == objectName)
            {
                LinkVocabulary(children[i], prompt, vocabularyId);
                return;
            }
        }
    }

    static void LinkVocabulary(Transform target, string prompt, string vocabularyId)
    {
        InteractableObject interactable = target.gameObject.AddComponent<InteractableObject>();
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("promptName").stringValue = prompt;
        serialized.FindProperty("vocabularyId").stringValue = vocabularyId;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static Transform Group(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool solid)
    {
        return Shape(PrimitiveType.Cube, name, position, scale, material, parent, solid, Vector3.zero);
    }

    static GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool solid, Vector3 eulerAngles)
    {
        return Shape(PrimitiveType.Cylinder, name, position, scale, material, parent, solid, eulerAngles);
    }

    static GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool solid)
    {
        return Cylinder(name, position, scale, material, parent, solid, Vector3.zero);
    }

    static GameObject Sphere(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool solid)
    {
        return Shape(PrimitiveType.Sphere, name, position, scale, material, parent, solid, Vector3.zero);
    }

    static GameObject Shape(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool solid, Vector3 eulerAngles)
    {
        GameObject shape = GameObject.CreatePrimitive(type);
        shape.name = name;
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = position;
        shape.transform.localRotation = Quaternion.Euler(eulerAngles);
        shape.transform.localScale = scale;
        shape.GetComponent<Renderer>().sharedMaterial = material;
        shape.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        shape.GetComponent<Renderer>().receiveShadows = false;
        if (!solid)
        {
            Collider collider = shape.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        if (batchShapes)
        {
            GameObjectUtility.SetStaticEditorFlags(shape, StaticEditorFlags.BatchingStatic);
        }
        return shape;
    }

    static Material Mat(string name, Color color, string textureName, float smoothness, float metallic, Vector2 tiling, Color? emission = null)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            material.name = name;
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        material.SetTexture("_BaseMap", null);
        material.SetTexture("_MainTex", null);

        if (!string.IsNullOrEmpty(textureName))
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/" + textureName + ".png");
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_BaseMap", tiling);
                material.SetTextureScale("_MainTex", tiling);
            }
        }

        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static void AddSceneToBuildSettings()
    {
        const string menuPath = "Assets/Scenes/MainMenu/MainMenu.unity";
        const string mapPath = "Assets/Scenes/CityMap/CityMap.unity";
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene => scene.path == ScenePath || scene.path == menuPath || scene.path == mapPath);
        scenes.Insert(0, new EditorBuildSettingsScene(menuPath, true));
        scenes.Insert(1, new EditorBuildSettingsScene(mapPath, true));
        scenes.Insert(2, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
