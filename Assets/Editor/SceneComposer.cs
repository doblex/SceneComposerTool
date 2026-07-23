using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class RoomCategory
{
    public string name;
    public string folderPath;
    public List<string> prefabNames = new();
    public List<GameObject> prefabAssets = new();
}

public class RoomPreview
{
    public Mesh mesh;
    public Material material;
    public DoorSpot door;
    public int index;
    public Matrix4x4 localMatrix;
}

public class DoorCouple
{
    public DoorSpot firstDoorSpot;
    public int firstDoorIndex;
    public Vector3 firstDoorPos;
    public DoorSpot secondDoorSpot;
    public float distance;
    public Vector3 direction { get { return (secondDoorSpot.transform.position - firstDoorSpot.transform.position); } }
}

public class SceneComposer : EditorWindow
{
    #region StaticToolOptions

    static string PREFAB_FOLDER = "Assets/Prefabs";
    static int COLUMN_COUNT = 2;
    static float THUMBNAIL_SIZE = 64;
    static float RANGE_THICKNESS = 0f;


    #endregion

    #region style

    GUIStyle titleOption;
    GUIStyle blackTitleOption;

    #endregion


    #region Option

    static bool snapActive = true;
    static bool showSnapRange = true;
    static float snapRange = 10f;
    static int layer;

    #endregion


    #region UIVariables

    List<RoomCategory> categories = new();
    List<string> cat = new();

    List<GameObject> prefabList = new();

    int selectedCategoryIndex = 0;
    int OldselectedCategoryIndex = 0;

    #endregion

    GameObject currentSelection;
    List<RoomPreview> roomPreviews = new();
    List<RoomPreview> doorPreviews = new();

    Vector3 previewPosition;
    Vector3 mousePos;
    Quaternion previewRotation;
    Vector3 previewScale;

    Vector2 scroll;

    bool isSnapped;
    DoorCouple couple;



    #region OpenMethods

   [MenuItem("Tools/SceneComposer")]
    public static void ShowWindow()
    {
        SceneComposer tool = GetWindow<SceneComposer>("Scene Composer");

        tool.UpdateList();
    }

    #endregion

    #region class methods

    private void OnEnable()
    {
        UpdateList();

        //creo gli stili per i testi
        titleOption = new();

        titleOption.alignment = TextAnchor.MiddleCenter;
        titleOption.fontSize = 20;
        titleOption.normal.textColor = Color.white;

        blackTitleOption = new();

        blackTitleOption.alignment = TextAnchor.MiddleCenter;
        blackTitleOption.fontSize = 20;
        blackTitleOption.normal.textColor = Color.black;

        layer = LayerMask.NameToLayer("Ground");

        //mi assicuro che sia collegata la funzione all'evento
        SceneView.duringSceneGui -= OnScenePreview;
        SceneView.duringSceneGui += OnScenePreview;
    }

    private void OnGUI()
    {
        DrawToolOptions();
        DrawRoomOptions();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnScenePreview;
    }

    public void OnScenePreview(SceneView sceneView)
    {
        //costruisco la UI in scena
        Handles.BeginGUI();
        DrawPrefabList();
        Handles.EndGUI();

        //preview della mesh in scena
        DrawPreviewMesh(sceneView);

        //repaint per sicurezza in modo che ho il tutto aggiornato
        sceneView.Repaint();
    }

    #endregion

    #region Data update

    private void UpdateList()
    {
        //reset delle liste
        categories.Clear();
        prefabList.Clear();
        cat.Clear();

        selectedCategoryIndex = OldselectedCategoryIndex =  0;

        //controllo di avere la cartella nel progetto
        if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER)) return;

        //ottengo le sottocartelle
        string fullRootPath = Path.GetFullPath(PREFAB_FOLDER);
        string[] subDirs = Directory.GetDirectories(fullRootPath, "*", SearchOption.AllDirectories);

        for (int i = 0; i < subDirs.Length; i++)
        {
            string subDir = subDirs[i];

            string folderName = Path.GetFileName(subDir);

            string assetFolderPath = PREFAB_FOLDER + "/" + folderName;

            //ottengo tutti i GUIDs presenti per ogni sottocartella
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { assetFolderPath });

            // se nessun elemento skippo la cartella
            if (guids.Length == 0) continue;

            // creo una categoria  che conterrà le informazioni da mostrare poi
            RoomCategory Category = new();

            Category.name = folderName;
            Category.folderPath = assetFolderPath;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                Category.prefabNames.Add(Path.GetFileNameWithoutExtension(assetPath));
                Category.prefabAssets.Add(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
            }

            cat.Add(Category.name);

            categories.Add(Category);
        }

        if (categories.Count > 0)
        { 
            SelectPrefab(categories[selectedCategoryIndex].prefabAssets[0]);
        }

    }

    #endregion

    #region DrawOnScreen methods

    private void DrawRoomOptions()
    {
        GUILayout.Space(10);
        GUILayout.Label("Room options", titleOption, GUILayout.ExpandWidth(true));
        GUILayout.Space(10);

        // bottone di refresh
        if (GUILayout.Button("Refresh"))
        {
            UpdateList();
        }

        // se non trovo asset lascio un messaggio
        if (categories.Count == 0)
        {
            GUILayout.Label("No room categories found in " + PREFAB_FOLDER);
            return;
        }

        // creo un popup generico, così il menù è pulito
        selectedCategoryIndex = EditorGUILayout.Popup("Room Category", OldselectedCategoryIndex, cat.ToArray());

        // se non scelgo lo stesso allora cambio la categoria e seleziono il primo prefab
        if (selectedCategoryIndex != OldselectedCategoryIndex)
        { 
            OldselectedCategoryIndex = selectedCategoryIndex;

            SelectPrefab(categories[selectedCategoryIndex].prefabAssets[0]);
        }

    }

    private void DrawToolOptions()
    {
        GUILayout.Space(10);
        GUILayout.Label("Tool options", titleOption, GUILayout.ExpandWidth(true));
        GUILayout.Space(10);

        // Campo layer
        layer = EditorGUILayout.LayerField("Layer",layer);

        //toggle per l'attivazione dello snap, se disattivato disattivo anche il resto delle opzioni di snap
        snapActive = EditorGUILayout.Toggle("Snap Active", snapActive);

        if(!snapActive) GUI.enabled = false;

        // Toggle per mostrare l'handle del range
        showSnapRange = EditorGUILayout.Toggle("Show Snap Range", showSnapRange);

        // slider per decidere il range
        snapRange = EditorGUILayout.Slider("Snap Range", snapRange, 1f, 100f);

        if (!snapActive) GUI.enabled = true;
    }

    private void DrawPrefabList()
    {
        scroll = GUILayout.BeginScrollView(scroll);

        GUILayout.Label("Use shift + Q or E to rotate the preview, use left click to place the prefab", blackTitleOption);

        // Bottone per l'undo, si allarga in base a quanto grande è la UI
        if (GUILayout.Button("Undo", GUILayout.Width(THUMBNAIL_SIZE * COLUMN_COUNT)))
        {
            Undo.PerformUndo();
        }

        if (categories.Count <= 0)
        { 
            GUILayout.EndScrollView();
            return;
        }

        float thumbnailSize = THUMBNAIL_SIZE;
        int columns = COLUMN_COUNT;
        int itemCount = categories[selectedCategoryIndex].prefabAssets.Count;

        // mi tiro fuori la quantità di righe
        int rows = Mathf.CeilToInt((float)itemCount / columns);

        for (int row = 0; row < rows; row++)
        {
            // Scope che mi apre e chiude in automatico  un horizontal
            using (new GUILayout.HorizontalScope())
            {
                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;

                    if (index >= itemCount)
                    {
                        // gli elementi in più sono delle label senza nulla
                        GUILayout.Label("", GUILayout.Width(thumbnailSize), GUILayout.Height(thumbnailSize));
                        continue;
                    }

                    GameObject prefab = categories[selectedCategoryIndex].prefabAssets[index];

                    // se è quello selezionato allora lo coloro per comodità
                    if (prefab == currentSelection)
                    {
                        GUI.backgroundColor = Color.blue;
                    }
                    else
                    {
                        GUI.backgroundColor = Color.white;
                    }

                    //Ottengo la preview
                    Texture2D preview = AssetPreview.GetAssetPreview(prefab);

                    GUIContent content;

                    if (preview != null)
                    {
                        // se caricata la mostro
                        content = new GUIContent(preview, categories[selectedCategoryIndex].prefabNames[index]);
                    }
                    else
                    {
                        // altrimenti mostro il nome
                        content = new GUIContent(categories[selectedCategoryIndex].prefabNames[index]);
                    }

                    // creo il bottone che contiene la preview
                    if (GUILayout.Button(content, GUILayout.Width(thumbnailSize), GUILayout.Height(thumbnailSize)))
                    {
                        SelectPrefab(prefab);
                    }
                }
            }
        }

        GUILayout.EndScrollView();
        GUI.backgroundColor = Color.white;
    }

    private void DrawPreviewMesh(SceneView sceneView)
    {
        if (currentSelection == null || roomPreviews.Count <= 0) return;

        // dice alla sceneview di non gestire i click
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        // mi salvo l'evento corrente
        Event e = Event.current;

        //Creo il raggio che mi darà la posizione sul piano
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        //mi creo la maschera per gestire il layer in cui usare il raycast
        LayerMask mask = 1 << layer;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000, mask))
        {
            //alzo la posizione per evitare il sovrapporsi di preview e pavimento
            mousePos = hit.point + Vector3.up * 0.01f;

            //ottengo la totalità delle porte nel range di snap
            List<DoorSpot> doors = Lib.GetObjectInRange<DoorSpot>(hit.point, snapRange);

            previewPosition = mousePos;

            // se lo snap è attivo e visualizzabile allora disegna gli handle
            if (snapActive)
            {
                if (showSnapRange)
                {
                    Handles.color = Color.yellow;
                    Handles.DrawWireDisc(mousePos, Vector3.up, snapRange, RANGE_THICKNESS);

                    // creo una label che segue il disco per indicare il raggio
                    Vector3 forward = SceneView.lastActiveSceneView.camera.transform.forward;
                    forward.y = 0;
                    forward.Normalize();

                    Vector3 labelPos = mousePos - forward * (snapRange - 1f);

                    Handles.Label(labelPos, snapRange.ToString());
                }

                //controllo e ottengo il primo snap valido più vicino della lista
                isSnapped = GetClosestDoorCouple(hit.point, doors, out couple);
                if (isSnapped)
                {
                    //calcolo la traslazione e il punto dello snap da fare
                    // couple.firstDoorSpot.transform.position viene moltiplicato per la rotazione della preview perché è la posizione del prefab
                    Vector3 direction = couple.secondDoorSpot.transform.position - (previewRotation * couple.firstDoorSpot.transform.position);

                    Matrix4x4 snapMatrix = Matrix4x4.TRS(direction, Quaternion.identity, Vector3.one);
                    previewPosition = snapMatrix.MultiplyPoint3x4(Vector3.zero);
                }
            }

            // creo la matrice per disegnare la preview nel punto corretto
            Matrix4x4 previewRootMatrix = Matrix4x4.TRS(previewPosition, previewRotation, previewScale);

            //disegno le mesh tramite la GPU per risparmiare risorse
            foreach (var preview in roomPreviews)
            {
                Graphics.DrawMesh(preview.mesh, previewRootMatrix * preview.localMatrix, preview.material, 0, sceneView.camera, 0);
            }
        }


        //Tolgo la preview con esc
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            ClearSelection();
            e.Use();
        }

        // shift + Q/E ruoto
        if (e.type == EventType.KeyDown && e.shift  && e.keyCode == KeyCode.Q)
        {
            previewRotation *= Quaternion.Euler(0, -90, 0);
            e.Use();
        }

        if (e.type == EventType.KeyDown && e.shift && e.keyCode == KeyCode.E)
        {
            previewRotation *= Quaternion.Euler(0, 90, 0);
            e.Use();
        }

        // instanzio la preview con tasto sinistro
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            GameObject spawned = PrefabUtility.InstantiatePrefab(currentSelection) as GameObject;
            spawned.transform.position = previewPosition;
            spawned.transform.rotation = previewRotation;

            // creo un gruppo per l'undo in quanto quando istanzio blocco le porte
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(currentSelection.name);

            // se snappo allora blocco le porte
            if (isSnapped && couple != null)
            {
                Transform[] children = spawned.transform.GetAllChildren().ToArray();

                children[couple.firstDoorIndex].GetComponent<DoorSpot>().Occupied = true;

                Undo.RecordObject(couple.secondDoorSpot, $"{couple.secondDoorSpot.name} occupied");
                couple.secondDoorSpot.Occupied = true;
            }

            Undo.RegisterCreatedObjectUndo(spawned, $"Spawn {currentSelection.name}");
            e.Use();
        }
    }

    private bool GetClosestDoorCouple(Vector3 centerPos, List<DoorSpot> doors, out DoorCouple couple)
    {
        DoorCouple closerDoors = null;
        // mi creo la matrice di trasformazione per le posizioni corrette rispetto alla preview
        Matrix4x4 matrix = Matrix4x4.TRS(previewPosition, previewRotation, previewScale);

        if (doors.Count <= 0)
        {
            couple = null;
            return false;
        }

        // per ogni porta in range
        foreach (DoorSpot door in doors)
        {
            // se occupata skippo
            if (door.Occupied) continue;

            // ottengo il forward per controllare che le porte siano opposte
            Vector3 dirB = door.transform.forward;

            foreach (RoomPreview currentRoomDoor in doorPreviews)
            {
                //Controllo che le porte abbiano il forward opposto
                Vector3 dirA = previewRotation * currentRoomDoor.door.transform.forward;

                // se il DotProduct è ≈ -1 allora sono opposte
                if (Vector3.Dot(dirA, dirB) > -0.95f)
                    continue;

                //calcolo l'origine del raggio
                Vector3 origin = matrix.MultiplyPoint3x4(currentRoomDoor.localMatrix.MultiplyPoint3x4(Vector3.zero));

                float distance = Vector3.Distance(door.transform.position, origin);

                //controllo che sia il primo elemento o l'elemento più vicino
                if (closerDoors == null || distance < closerDoors.distance)
                {
                    DoorCouple candidate = new DoorCouple();
                    candidate.distance = distance;
                    candidate.firstDoorSpot = currentRoomDoor.door;
                    candidate.firstDoorPos = origin;
                    candidate.firstDoorIndex = currentRoomDoor.index;
                    candidate.secondDoorSpot = door;

                    closerDoors = candidate;
                }
            }
        }

        couple = closerDoors;

        return couple != null;
    }

    #endregion

    #region Selection

    private void ClearSelection()
    {
        // cancello la selezione
        currentSelection = null;
        roomPreviews.Clear();
        doorPreviews.Clear();
    }

    private void SelectPrefab(GameObject prefab)
    {
        currentSelection = prefab;

        if (prefab != null)
        {
            // ottengo ogni figlio in modo ricorsivo
            Transform[] children = prefab.transform.GetAllChildren().ToArray();

            roomPreviews.Clear();
            doorPreviews.Clear();

            if (children.Length > 0)
            {
                // matrice del prefab
                Matrix4x4 prefabLocalMatrix = prefab.transform.worldToLocalMatrix;

                for (int i = 0; i < children.Length; i++)
                {
                    Transform child = children[i];
                    RoomPreview preview;

                    // se il figlio ha una mesh allora me lo salvo assieme al materiale
                    if (child.TryGetComponent(out MeshFilter meshFilter) 
                        && child.TryGetComponent(out MeshRenderer meshRenderer))
                    {
                        preview = new();

                        preview.mesh = meshFilter.sharedMesh;
                        preview.material = meshRenderer.sharedMaterial;

                        preview.localMatrix = prefabLocalMatrix * child.localToWorldMatrix;

                        roomPreviews.Add(preview);
                    }

                    // se il figlio ha uno script porta allora lo salvo in ul altra lista
                    if (child.TryGetComponent(out DoorSpot doorSpot))
                    {
                        preview = new();

                        preview.door = doorSpot;
                        preview.localMatrix = prefabLocalMatrix * child.localToWorldMatrix;
                        preview.index = i;

                        doorPreviews.Add(preview);
                    }
                }

                // salvo scala e rotazione del prefab
                previewScale = prefab.transform.localScale;
                previewRotation = prefab.transform.localRotation;
            }
            else
            {
                ClearSelection();
            }
        }

    }

    #endregion
}