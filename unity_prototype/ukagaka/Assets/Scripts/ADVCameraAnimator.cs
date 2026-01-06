using System.Collections;
using UnityEngine;

/// <summary>
/// ADVカメラのスムーズな移動アニメーションを制御
/// ADVCameraControllerと連携して、カメラ位置を滑らかに遷移させる
/// </summary>
public class ADVCameraAnimator : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private ADVCameraController cameraController;

    [SerializeField]
    private Camera mainCamera;

    [Header("アニメーション設定")]
    [Range(0.1f, 5f)]
    [SerializeField]
    private float transitionDuration = 1f; // 移動にかかる時間（秒）

    [SerializeField]
    private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 移動のイージングカーブ

    private Coroutine currentTransition;

    private void Start()
    {
        // 参照が未設定の場合は自動取得
        if (cameraController == null)
            cameraController = FindObjectOfType<ADVCameraController>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    /// <summary>
    /// プリセットにスムーズに移動する
    /// </summary>
    /// <param name="preset">目標プリセット</param>
    public void MoveToPreset(ADVCameraController.CameraPreset preset)
    {
        if (cameraController == null)
        {
            Debug.LogWarning("ADVCameraController is not assigned!");
            return;
        }

        if (currentTransition != null)
            StopCoroutine(currentTransition);

        currentTransition = StartCoroutine(TransitionToPreset(preset));
    }

    /// <summary>
    /// カスタム距離にスムーズに移動する
    /// </summary>
    /// <param name="distance">目標カメラ距離</param>
    /// <param name="fov">目標視野角</param>
    /// <param name="height">目標高さオフセット</param>
    public void MoveToDistance(float distance, float fov, float height)
    {
        if (cameraController == null)
        {
            Debug.LogWarning("ADVCameraController is not assigned!");
            return;
        }

        if (currentTransition != null)
            StopCoroutine(currentTransition);

        currentTransition = StartCoroutine(TransitionToCustom(distance, fov, height));
    }

    /// <summary>
    /// カスタム値にスムーズに移動する（角度含む）
    /// </summary>
    /// <param name="distance">目標カメラ距離</param>
    /// <param name="fov">目標視野角</param>
    /// <param name="height">目標高さオフセット</param>
    /// <param name="angleX">目標X軸回転</param>
    /// <param name="angleY">目標Y軸回転</param>
    public void MoveToCustom(float distance, float fov, float height, float angleX, float angleY)
    {
        if (cameraController == null)
        {
            Debug.LogWarning("ADVCameraController is not assigned!");
            return;
        }

        if (currentTransition != null)
            StopCoroutine(currentTransition);

        currentTransition = StartCoroutine(TransitionToCustomWithAngle(distance, fov, height, angleX, angleY));
    }

    /// <summary>
    /// プリセットへの遷移アニメーション
    /// </summary>
    private IEnumerator TransitionToPreset(ADVCameraController.CameraPreset preset)
    {
        // 現在の値を保存
        float startDistance = cameraController.CameraDistance;
        float startFOV = cameraController.CameraFOV;
        float startHeight = cameraController.CameraHeightOffset;

        // 目標値を取得
        float targetDistance = 0, targetFOV = 0, targetHeight = 0;
        switch (preset)
        {
            case ADVCameraController.CameraPreset.CloseUp:
                targetDistance = 2.5f;
                targetFOV = 40f;
                targetHeight = 1.2f;
                break;

            case ADVCameraController.CameraPreset.Normal:
                targetDistance = 5f;
                targetFOV = 60f;
                targetHeight = 0.5f;
                break;

            case ADVCameraController.CameraPreset.Wide:
                targetDistance = 7f;
                targetFOV = 70f;
                targetHeight = 0f;
                break;
        }

        // アニメーション実行
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / transitionDuration);

            SetCameraValues(
                Mathf.Lerp(startDistance, targetDistance, t),
                Mathf.Lerp(startFOV, targetFOV, t),
                Mathf.Lerp(startHeight, targetHeight, t)
            );

            yield return null;
        }

        // 最終値を確定
        SetCameraValues(targetDistance, targetFOV, targetHeight);
        cameraController.SetPreset(preset);

        currentTransition = null;
    }

    /// <summary>
    /// カスタム値への遷移アニメーション
    /// </summary>
    private IEnumerator TransitionToCustom(float targetDistance, float targetFOV, float targetHeight)
    {
        float startDistance = cameraController.CameraDistance;
        float startFOV = cameraController.CameraFOV;
        float startHeight = cameraController.CameraHeightOffset;
        float startAngleX = cameraController.CameraAngleX;
        float startAngleY = cameraController.CameraAngleY;

        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / transitionDuration);

            SetCameraValuesWithAngle(
                Mathf.Lerp(startDistance, targetDistance, t),
                Mathf.Lerp(startFOV, targetFOV, t),
                Mathf.Lerp(startHeight, targetHeight, t),
                Mathf.Lerp(startAngleX, startAngleX, t), // 角度は維持
                Mathf.Lerp(startAngleY, startAngleY, t)
            );

            yield return null;
        }

        // 最終値を確定
        SetCameraValuesWithAngle(targetDistance, targetFOV, targetHeight, startAngleX, startAngleY);
        cameraController.SetCustomCamera(targetDistance, targetFOV, targetHeight);

        currentTransition = null;
    }

    /// <summary>
    /// カスタム値への遷移アニメーション（角度含む）
    /// </summary>
    private IEnumerator TransitionToCustomWithAngle(float targetDistance, float targetFOV, float targetHeight, float targetAngleX, float targetAngleY)
    {
        float startDistance = cameraController.CameraDistance;
        float startFOV = cameraController.CameraFOV;
        float startHeight = cameraController.CameraHeightOffset;
        float startAngleX = cameraController.CameraAngleX;
        float startAngleY = cameraController.CameraAngleY;

        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / transitionDuration);

            SetCameraValuesWithAngle(
                Mathf.Lerp(startDistance, targetDistance, t),
                Mathf.Lerp(startFOV, targetFOV, t),
                Mathf.Lerp(startHeight, targetHeight, t),
                Mathf.Lerp(startAngleX, targetAngleX, t),
                Mathf.Lerp(startAngleY, targetAngleY, t)
            );

            yield return null;
        }

        // 最終値を確定
        SetCameraValuesWithAngle(targetDistance, targetFOV, targetHeight, targetAngleX, targetAngleY);

        currentTransition = null;
    }

    /// <summary>
    /// カメラの値を設定して更新
    /// </summary>
    private void SetCameraValues(float distance, float fov, float height)
    {
        cameraController.SetCameraValuesForAnimation(distance, fov, height);
    }

    /// <summary>
    /// カメラの値を設定して更新（角度含む）
    /// </summary>
    private void SetCameraValuesWithAngle(float distance, float fov, float height, float angleX, float angleY)
    {
        cameraController.SetCameraValuesForAnimation(distance, fov, height, angleX, angleY);
    }

    /// <summary>
    /// 現在のアニメーションを停止する
    /// </summary>
    public void StopTransition()
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
        }
    }

    // テスト用のメソッド（Inspectorから実行可能）
    [ContextMenu("クローズアップ")]
    private void TestCloseUp() => MoveToPreset(ADVCameraController.CameraPreset.CloseUp);

    [ContextMenu("通常")]
    private void TestNormal() => MoveToPreset(ADVCameraController.CameraPreset.Normal);

    [ContextMenu("遠景")]
    private void TestWide() => MoveToPreset(ADVCameraController.CameraPreset.Wide);
}
