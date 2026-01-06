using System;
using System.IO;
using UnityEngine;

/// <summary>
/// ADVゲーム風のカメラコントローラー
/// キャラクター配置とカメラ位置を制御し、プリセット（クローズアップ/通常/遠景）を提供
/// クロマキー背景、カメラ角度制御、プリセット保存機能を含む
/// </summary>
[ExecuteInEditMode]
public class ADVCameraController : MonoBehaviour
{
    [Header("カメラ設定")]
    [SerializeField]
    private Camera mainCamera;

    [Header("キャラクター")]
    [SerializeField]
    private Transform leftCharacter;

    [SerializeField]
    private Transform rightCharacter;

    [Header("キャラクター配置")]
    [Range(1f, 15f)]
    [SerializeField]
    private float characterDistance = 5f; // キャラがいる位置（このGameObjectのforward方向への距離）

    [Range(0.5f, 10f)]
    [SerializeField]
    private float characterSpacing = 2.5f; // キャラ同士の間隔の半分（左右に±で配置）

    [Range(-90f, 90f)]
    [SerializeField]
    private float characterAngle = 15f; // カメラに向ける角度（-90～90度、合計180度の範囲）

    [Range(-5f, 5f)]
    [SerializeField]
    private float characterHeightOffset = 0f; // キャラクターの高さ調整（上下に移動）

    [Range(-5f, 5f)]
    [SerializeField]
    private float cameraHeightOffset = 0f; // カメラの高さ調整（上下に移動、キャラクターとは独立）

    [Header("カメラ設定（会話シーン用）")]
    [Range(0.5f, 20f)]
    [SerializeField]
    private float cameraDistance = 5f; // カメラとキャラの距離（メートル単位、大きいほど遠くから撮影）

    [Range(30f, 80f)]
    [SerializeField]
    private float cameraFOV = 60f; // 視野角（度単位、小さいほどズーム、大きいほど広角）

    [Range(-90f, 90f)]
    [SerializeField]
    private float cameraAngleX = 0f; // カメラのX軸回転（上下、-90度～90度）

    [Range(-90f, 90f)]
    [SerializeField]
    private float cameraAngleY = 0f; // カメラのY軸回転（左右、-90度～90度）

    [Header("クロマキー背景")]
    [SerializeField]
    private bool useChromaKeyBackground = true;

    [SerializeField]
    private Color chromaKeyColor = Color.green; // クロマキー色（デフォルト：緑）

    [SerializeField]
    private float backgroundSize = 100f; // 背景のサイズ（シーン全体を覆う大きさ）

    [SerializeField]
    private bool useCameraBackgroundColor = true; // カメラの背景色も設定する

    [SerializeField]
    private float backgroundDistanceFromCharacters = 5f; // キャラ中心から背景までの距離（前方）

    private GameObject chromaKeyBackground;
    private bool backgroundCreatedAtRuntime = false;

    [Header("カメラプリセット")]
    [SerializeField]
    private bool usePresets = false;

    [SerializeField]
    private CameraPreset currentPreset = CameraPreset.Normal;

    [Header("プリセット保存/読み込み")]
    [SerializeField]
    private string presetSavePath = "ADVCameraPresets";

    [Header("実行")]
    [SerializeField]
    private bool autoUpdate = true; // エディタ上で自動更新するか

    [SerializeField]
    private bool initializeOnStart = false; // ゲーム開始時にカメラを初期化するか（デフォルト：false、エディタで配置済みの場合は不要）

    // カメラの基準位置（キャラの中心を見る位置）
    private Vector3 characterCenterPosition;
    private bool isInitialized = false; // 初期化済みフラグ
    
    // 初期位置を保存（ゲーム開始時の急な移動を防ぐため）
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool hasInitialTransform = false;

    public float CameraDistance => cameraDistance;
    public float CameraFOV => cameraFOV;
    public float CharacterHeightOffset => characterHeightOffset;
    public float CameraHeightOffset => cameraHeightOffset;
    public float CameraAngleX => cameraAngleX;
    public float CameraAngleY => cameraAngleY;

    /// <summary>
    /// カメラプリセットの種類
    /// </summary>
    public enum CameraPreset
    {
        CloseUp,    // 顔アップ
        Normal,     // 通常
        Wide        // 全身・遠景
    }

    private void Awake()
    {
        // エディタモードで既に配置されている場合は、初期位置を保存
        if (!Application.isPlaying)
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            hasInitialTransform = true;
        }
    }

    private void Start()
    {
        // ゲーム開始時に、このGameObjectの位置が変わっていないか確認
        if (Application.isPlaying && hasInitialTransform)
        {
            // エディタモードで保存した位置と現在の位置が異なる場合
            if (Vector3.Distance(transform.position, initialPosition) > 0.01f ||
                Quaternion.Angle(transform.rotation, initialRotation) > 0.1f)
            {
                Debug.LogWarning($"ADVCameraController: GameObjectの位置がゲーム開始時に変更されました。");
                Debug.LogWarning($"エディタ位置: {initialPosition}, 現在位置: {transform.position}");
                // 位置が変わっている場合は、エディタモードの位置に戻す
                transform.position = initialPosition;
                transform.rotation = initialRotation;
            }
        }
        else if (Application.isPlaying && !hasInitialTransform)
        {
            // エディタモードで実行されなかった場合は、現在位置を保存
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            hasInitialTransform = true;
        }

        // クロマキー背景を生成
        if (useChromaKeyBackground)
        {
            CreateChromaKeyBackground();
        }

        // ゲーム開始時にカメラ位置を初期化（オプション）
        // エディタモードで既に配置されている場合は、急な移動を避けるためスキップ
        if (Application.isPlaying && initializeOnStart && !isInitialized)
        {
            if (mainCamera != null)
            {
                // エディタモードで既にカメラが配置されているか確認
                // カメラが初期位置（0,0,0）でない場合、既に配置済みと判断
                Vector3 currentCameraPos = mainCamera.transform.position;
                bool isCameraAlreadyPositioned = currentCameraPos != Vector3.zero && 
                    (Mathf.Abs(currentCameraPos.x) > 0.01f || 
                     Mathf.Abs(currentCameraPos.y) > 0.01f || 
                     Mathf.Abs(currentCameraPos.z) > 0.01f);

                if (isCameraAlreadyPositioned)
                {
                    // カメラが既に配置されている場合は、再配置しない
                    Debug.Log($"[ADVCameraController] カメラは既に配置済みのため、初期化をスキップします。現在位置: {currentCameraPos}");
                }
                else
                {
                    // カメラが初期位置の場合は、設定に基づいて配置
                    Debug.Log($"[ADVCameraController] カメラが初期位置のため、設定に基づいて配置します。");
                    UpdateCamera();
                }
            }
            else
            {
                // カメラが未設定の場合は、設定に基づいて配置
                UpdateCamera();
            }

            isInitialized = true;
        }
    }

    private void Update()
    {
        // エディタモードで、かつ自動更新ONの時だけ更新
        if (!Application.isPlaying && autoUpdate)
        {
            UpdateCamera();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (autoUpdate)
                UpdateCamera();
            return;
        }

        UpdateCamera();
    }

    private void OnDestroy()
    {
        // クロマキー背景を削除
        if (chromaKeyBackground != null)
        {
            if (Application.isPlaying && backgroundCreatedAtRuntime)
                Destroy(chromaKeyBackground);
            else if (!Application.isPlaying && backgroundCreatedAtRuntime)
                DestroyImmediate(chromaKeyBackground);
        }
    }

    /// <summary>
    /// カメラとキャラクターの配置を更新
    /// </summary>
    public void UpdateCamera()
    {
        if (mainCamera == null)
            return;

        // プリセット使用時は値を上書き
        if (usePresets)
        {
            float manualCharacterHeight = characterHeightOffset;
            float manualCameraHeight = cameraHeightOffset;
            ApplyPreset(currentPreset);
            // エディタで手動調整した高さオフセットを保持
            if (!Application.isPlaying)
            {
                characterHeightOffset = manualCharacterHeight;
                cameraHeightOffset = manualCameraHeight;
            }
        }

        UpdateCharacterCenterPosition();

        // キャラクター配置（両方設定されているときのみ）
        if (leftCharacter != null && rightCharacter != null)
            SetupCharacters();

        // カメラ配置
        SetupCamera();
    }

    /// <summary>
    /// キャラクターを配置する
    /// </summary>
    private void SetupCharacters()
    {
        // 左キャラ配置
        leftCharacter.position = characterCenterPosition + transform.right * -characterSpacing;
        leftCharacter.LookAt(new Vector3(
            characterCenterPosition.x,
            leftCharacter.position.y,
            characterCenterPosition.z
        ));
        leftCharacter.Rotate(0, -characterAngle, 0);

        // 右キャラ配置
        rightCharacter.position = characterCenterPosition + transform.right * characterSpacing;
        rightCharacter.LookAt(new Vector3(
            characterCenterPosition.x,
            rightCharacter.position.y,
            characterCenterPosition.z
        ));
        rightCharacter.Rotate(0, characterAngle, 0);
    }

    private void UpdateCharacterCenterPosition()
    {
        // キャラの基準位置を計算（このGameObjectのforward方向に配置）
        characterCenterPosition = transform.position + transform.forward * characterDistance;
        // キャラクターの高さオフセットを適用（カメラとは独立）
        characterCenterPosition.y += characterHeightOffset;
    }

    /// <summary>
    /// カメラを配置する
    /// </summary>
    private void SetupCamera()
    {
        // カメラ位置を計算（キャラの中心から、指定距離だけ後ろに配置）
        Vector3 baseDirection = -transform.forward;
        Vector3 cameraPos = characterCenterPosition + baseDirection * cameraDistance;

        // カメラの高さを独立して調整
        cameraPos.y += cameraHeightOffset;

        mainCamera.transform.position = cameraPos;

        // LookAtは使わず、手動の回転値で向きを決める
        mainCamera.transform.rotation = transform.rotation * Quaternion.Euler(cameraAngleX, cameraAngleY, 0f);

        // FOV（視野角）を設定
        mainCamera.fieldOfView = cameraFOV;

        // カメラの背景色を設定（クロマキー用）
        if (useChromaKeyBackground && useCameraBackgroundColor)
        {
            mainCamera.backgroundColor = chromaKeyColor;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        // クロマキー背景を更新
        if (useChromaKeyBackground && chromaKeyBackground != null)
        {
            UpdateChromaKeyBackground();
        }
    }

    /// <summary>
    /// クロマキー背景を作成する（シーン全体を覆う）
    /// </summary>
    private void CreateChromaKeyBackground()
    {
        // 既存の背景がシーンにあれば再利用
        if (chromaKeyBackground == null)
        {
            chromaKeyBackground = GameObject.Find("ChromaKeyBackground_Scene");
            if (chromaKeyBackground != null)
            {
                backgroundCreatedAtRuntime = false;
            }
        }

        if (chromaKeyBackground == null)
        {
            // シーン全体を覆う大きな背景オブジェクトを作成
            // 球体（Sphere）を使用して、シーン全体を囲む
            chromaKeyBackground = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chromaKeyBackground.name = "ChromaKeyBackground_Scene";
            backgroundCreatedAtRuntime = true;
        }
        
        // 背景はシーンのルートに配置（親を設定しない）
        chromaKeyBackground.transform.position = transform.position;

        // マテリアルを作成して色を設定
        var renderer = chromaKeyBackground.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = chromaKeyColor;
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0f);
            // 両面描画のために、シェーダーを変更（Unlit/Colorなど）
            // または、裏面も描画されるように設定
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // 両面描画
            renderer.material = mat;
        }

        // コライダーは不要なので削除
        Collider col = chromaKeyBackground.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying)
                Destroy(col);
            else
                DestroyImmediate(col);
        }

        // 背景のサイズを設定（シーン全体を覆う大きさ）
        UpdateChromaKeyBackground();
    }

    /// <summary>
    /// クロマキー背景を更新する（シーン全体を覆う）
    /// </summary>
    private void UpdateChromaKeyBackground()
    {
        if (chromaKeyBackground == null)
            return;

        Vector3 targetPos;
        if (mainCamera != null)
        {
            // キャラ中心より前方（カメラから見て奥）に配置
            targetPos = characterCenterPosition + transform.forward * backgroundDistanceFromCharacters;

            // カメラ前方に必ず来るように補正
            Vector3 toTarget = targetPos - mainCamera.transform.position;
            if (Vector3.Dot(mainCamera.transform.forward, toTarget) <= 0f)
            {
                targetPos = mainCamera.transform.position + mainCamera.transform.forward * (cameraDistance + backgroundDistanceFromCharacters);
            }
        }
        else
        {
            targetPos = transform.position + transform.forward * backgroundDistanceFromCharacters;
        }

        chromaKeyBackground.transform.position = targetPos;

        // サイズを設定（シーン全体を覆う大きさ）
        // Sphereのデフォルト半径は0.5なので、backgroundSizeをそのままスケールに使用
        chromaKeyBackground.transform.localScale = Vector3.one * backgroundSize;

        // 色を更新
        Renderer renderer = chromaKeyBackground.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = chromaKeyColor;
        }
    }

    /// <summary>
    /// プリセットを適用する
    /// </summary>
    /// <param name="preset">適用するプリセット</param>
    private void ApplyPreset(CameraPreset preset)
    {
        switch (preset)
        {
            case CameraPreset.CloseUp:
                cameraDistance = 2.5f;
                cameraFOV = 40f;
                characterHeightOffset = 1.2f; // 顔の高さに
                cameraHeightOffset = 1.2f; // カメラも同じ高さ
                break;

            case CameraPreset.Normal:
                cameraDistance = 5f;
                cameraFOV = 60f;
                characterHeightOffset = 0.5f; // 上半身
                cameraHeightOffset = 0.5f; // カメラも同じ高さ
                break;

            case CameraPreset.Wide:
                cameraDistance = 7f;
                cameraFOV = 70f;
                characterHeightOffset = 0f; // 全身
                cameraHeightOffset = 0f; // カメラも同じ高さ
                break;
        }
    }

    /// <summary>
    /// Inspectorからボタンで実行できるように
    /// </summary>
    [ContextMenu("配置を更新")]
    private void UpdateSetup()
    {
        UpdateCamera();
    }

    /// <summary>
    /// クロマキー背景を再生成
    /// </summary>
    [ContextMenu("クロマキー背景を再生成")]
    private void RecreateChromaKeyBackground()
    {
        CreateChromaKeyBackground();
    }

    /// <summary>
    /// 現在の設定をプリセットとして保存
    /// </summary>
    [ContextMenu("現在の設定を保存")]
    public void SaveCurrentPreset()
    {
        SaveCurrentPreset("CustomPreset_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
    }

    /// <summary>
    /// 現在の設定をプリセットとして保存（名前指定）
    /// </summary>
    /// <param name="presetName">プリセット名</param>
    public void SaveCurrentPreset(string presetName)
    {
        var preset = new CameraPresetData
        {
            presetName = presetName,
            characterDistance = characterDistance,
            characterSpacing = characterSpacing,
            characterAngle = characterAngle,
            characterHeightOffset = characterHeightOffset,
            cameraHeightOffset = cameraHeightOffset,
            cameraDistance = cameraDistance,
            cameraFOV = cameraFOV,
            cameraAngleX = cameraAngleX,
            cameraAngleY = cameraAngleY,
            chromaKeyColor = chromaKeyColor,
            backgroundSize = backgroundSize
        };

        string json = JsonUtility.ToJson(preset, true);
        string path = Path.Combine(Application.persistentDataPath, presetSavePath);
        Directory.CreateDirectory(path);
        string filePath = Path.Combine(path, presetName + ".json");
        File.WriteAllText(filePath, json);

        Debug.Log($"プリセットを保存しました: {filePath}");
    }

    /// <summary>
    /// 保存されたプリセットを読み込む
    /// </summary>
    /// <param name="presetName">プリセット名</param>
    public void LoadPreset(string presetName)
    {
        string path = Path.Combine(Application.persistentDataPath, presetSavePath, presetName + ".json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"プリセットが見つかりません: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        var preset = JsonUtility.FromJson<CameraPresetData>(json);

        // 値を適用
        characterDistance = preset.characterDistance;
        characterSpacing = preset.characterSpacing;
        characterAngle = preset.characterAngle;
        characterHeightOffset = preset.characterHeightOffset;
        cameraHeightOffset = preset.cameraHeightOffset;
        cameraDistance = preset.cameraDistance;
        cameraFOV = preset.cameraFOV;
        cameraAngleX = preset.cameraAngleX;
        cameraAngleY = preset.cameraAngleY;
        chromaKeyColor = preset.chromaKeyColor;
        backgroundSize = preset.backgroundSize;

        usePresets = false;
        UpdateCamera();

        Debug.Log($"プリセットを読み込みました: {presetName}");
    }

    /// <summary>
    /// プリセットデータクラス
    /// </summary>
    [Serializable]
    public class CameraPresetData
    {
        public string presetName;
        public float characterDistance;
        public float characterSpacing;
        public float characterAngle;
        public float characterHeightOffset;
        public float cameraHeightOffset;
        public float cameraDistance;
        public float cameraFOV;
        public float cameraAngleX;
        public float cameraAngleY;
        public Color chromaKeyColor;
        public float backgroundSize;
    }

    /// <summary>
    /// 現在のプリセットを設定する（外部から呼び出し可能）
    /// </summary>
    /// <param name="preset">設定するプリセット</param>
    public void SetPreset(CameraPreset preset)
    {
        currentPreset = preset;
        usePresets = true;
        UpdateCamera();
    }

    /// <summary>
    /// カスタムカメラ設定を適用する（外部から呼び出し可能）
    /// </summary>
    /// <param name="distance">カメラ距離</param>
    /// <param name="fov">視野角</param>
    /// <param name="cameraHeight">カメラの高さオフセット</param>
    public void SetCustomCamera(float distance, float fov, float cameraHeight)
    {
        cameraDistance = distance;
        cameraFOV = fov;
        cameraHeightOffset = cameraHeight;
        usePresets = false;
        UpdateCamera();
    }

    /// <summary>
    /// アニメーション用：カメラ値を設定して更新（usePresetsは変更しない）
    /// </summary>
    /// <param name="distance">カメラ距離</param>
    /// <param name="fov">視野角</param>
    /// <param name="cameraHeight">カメラの高さオフセット</param>
    public void SetCameraValuesForAnimation(float distance, float fov, float cameraHeight)
    {
        cameraDistance = distance;
        cameraFOV = fov;
        cameraHeightOffset = cameraHeight;
        UpdateCamera();
    }

    /// <summary>
    /// アニメーション用：カメラ値を設定して更新（角度含む）
    /// </summary>
    /// <param name="distance">カメラ距離</param>
    /// <param name="fov">視野角</param>
    /// <param name="cameraHeight">カメラの高さオフセット</param>
    /// <param name="angleX">X軸回転</param>
    /// <param name="angleY">Y軸回転</param>
    public void SetCameraValuesForAnimation(float distance, float fov, float cameraHeight, float angleX, float angleY)
    {
        cameraDistance = distance;
        cameraFOV = fov;
        cameraHeightOffset = cameraHeight;
        cameraAngleX = angleX;
        cameraAngleY = angleY;
        UpdateCamera();
    }
}
