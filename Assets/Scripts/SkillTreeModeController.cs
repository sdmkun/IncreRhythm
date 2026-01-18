using UnityEngine;
using UnityEngine.UI;
using Esper.SkillTree.UI;

/// <summary>
/// スキルツリーモードの切り替えを管理
/// スペースキーでスキルツリーUIの表示/非表示と背景のオーバーレイを制御
/// </summary>
public class SkillTreeModeController : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("画面を暗くするオーバーレイ用のCanvas")]
    [SerializeField] private Canvas overlayCanvas;

    [Tooltip("オーバーレイの暗さ（0-1）")]
    [SerializeField] private float overlayAlpha = 0.4f;

    [Header("Audio Settings")]
    [Tooltip("スキルツリーモード時のAISAC値")]
    [SerializeField] private float skillTreeModeAisacValue = 0.9f;

    [Tooltip("通常モード時のAISAC値")]
    [SerializeField] private float normalModeAisacValue = 0.5f;

    // 内部変数
    private Image overlayImage;
    private bool isSkillTreeMode = false;

    // イベント：他のスクリプトが購読できるように
    public event System.Action<bool> OnSkillTreeModeChanged;

    void Start()
    {
        // オーバーレイCanvasの初期化
        InitializeOverlay();

        // スキルツリーウィンドウのイベントをリッスン
        if (DefaultSkillTreeWindow.instance != null)
        {
            DefaultSkillTreeWindow.instance.onOpen.AddListener(OnSkillTreeOpened);
            DefaultSkillTreeWindow.instance.onClose.AddListener(OnSkillTreeClosed);
        }
    }

    void Update()
    {
        // スペースキーでスキルツリーモードを切り替え
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleSkillTreeMode();
        }
    }

    /// <summary>
    /// オーバーレイCanvasの初期化
    /// </summary>
    void InitializeOverlay()
    {
        if (overlayCanvas == null)
        {
            // Canvasが未設定の場合は新規作成
            GameObject overlayObj = new GameObject("SkillTreeOverlay");
            overlayCanvas = overlayObj.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = -1; // スキルツリーUIの後ろに表示

            CanvasScaler scaler = overlayObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            overlayObj.AddComponent<GraphicRaycaster>();

            // 暗い背景イメージを追加
            GameObject imageObj = new GameObject("Background");
            imageObj.transform.SetParent(overlayObj.transform, false);

            overlayImage = imageObj.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, overlayAlpha);
            overlayImage.raycastTarget = false; // クリックをブロックしない

            RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
        }
        else
        {
            // 既存のCanvasからImageコンポーネントを取得
            overlayImage = overlayCanvas.GetComponentInChildren<Image>();
        }

        // 初期状態は非表示
        overlayCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// スキルツリーモードを切り替え
    /// </summary>
    public void ToggleSkillTreeMode()
    {
        if (DefaultSkillTreeWindow.instance != null)
        {
            DefaultSkillTreeWindow.instance.ToggleActive();
        }
    }

    /// <summary>
    /// スキルツリーが開かれたときの処理
    /// </summary>
    void OnSkillTreeOpened()
    {
        isSkillTreeMode = true;
        overlayCanvas.gameObject.SetActive(true);
        OnSkillTreeModeChanged?.Invoke(true);
        Debug.Log("<color=cyan>Skill Tree Mode: ON</color>");
    }

    /// <summary>
    /// スキルツリーが閉じられたときの処理
    /// </summary>
    void OnSkillTreeClosed()
    {
        isSkillTreeMode = false;
        overlayCanvas.gameObject.SetActive(false);
        OnSkillTreeModeChanged?.Invoke(false);
        Debug.Log("<color=cyan>Skill Tree Mode: OFF</color>");
    }

    /// <summary>
    /// 現在スキルツリーモードかどうかを取得
    /// </summary>
    public bool IsSkillTreeMode()
    {
        return isSkillTreeMode;
    }

    /// <summary>
    /// 現在のAISAC値を取得（スキルツリーモードに応じた値）
    /// </summary>
    public float GetCurrentAisacValue()
    {
        return isSkillTreeMode ? skillTreeModeAisacValue : normalModeAisacValue;
    }

    void OnDestroy()
    {
        // イベントのクリーンアップ
        if (DefaultSkillTreeWindow.instance != null)
        {
            DefaultSkillTreeWindow.instance.onOpen.RemoveListener(OnSkillTreeOpened);
            DefaultSkillTreeWindow.instance.onClose.RemoveListener(OnSkillTreeClosed);
        }
    }
}
