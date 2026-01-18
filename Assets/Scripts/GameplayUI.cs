using UnityEngine;
using System.Collections;

/// <summary>
/// ゲームプレイ中のUI表示を管理
/// 判定結果とVoltageの表示
/// </summary>
public class GameplayUI : MonoBehaviour
{
    [Header("Judgment Display")]
    [SerializeField] private bool showJudgmentText = true;
    [SerializeField] private float judgmentDisplayDuration = 0.5f;

    [Header("Voltage Display")]
    [SerializeField] private bool showVoltage = true;
    [SerializeField] private float voltageBarWidth = 400f;
    [SerializeField] private float voltageBarHeight = 30f;
    [SerializeField] private Vector2 voltagePosition = new Vector2(50, 50);

    [Header("References")]
    [SerializeField] private VoltageManager voltageManager;

    // 内部変数
    private string currentJudgmentText = "";
    private Color currentJudgmentColor = Color.white;
    private float judgmentDisplayTimer = 0f;

    void Start()
    {
        if (voltageManager == null)
        {
            voltageManager = FindFirstObjectByType<VoltageManager>();
        }
    }

    void Update()
    {
        // 判定テキストのタイマーを減らす
        if (judgmentDisplayTimer > 0)
        {
            judgmentDisplayTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 判定結果を表示
    /// </summary>
    public void ShowJudgment(JudgmentResult judgment)
    {
        if (!showJudgmentText) return;

        switch (judgment)
        {
            case JudgmentResult.Perfect:
                currentJudgmentText = "PERFECT!";
                currentJudgmentColor = new Color(1f, 0.84f, 0f); // ゴールド
                break;
            case JudgmentResult.Great:
                currentJudgmentText = "GREAT!";
                currentJudgmentColor = new Color(0f, 1f, 0.5f); // 明るい緑
                break;
            case JudgmentResult.Good:
                currentJudgmentText = "GOOD";
                currentJudgmentColor = new Color(0.5f, 1f, 0.5f); // 緑
                break;
            case JudgmentResult.Bad:
                currentJudgmentText = "BAD";
                currentJudgmentColor = new Color(1f, 0.5f, 0f); // オレンジ
                break;
            case JudgmentResult.Miss:
                currentJudgmentText = "MISS";
                currentJudgmentColor = new Color(1f, 0f, 0f); // 赤
                break;
        }

        judgmentDisplayTimer = judgmentDisplayDuration;
    }

    void OnGUI()
    {
        // 判定テキストの表示
        if (showJudgmentText && judgmentDisplayTimer > 0)
        {
            DrawJudgmentText();
        }

        // Voltageの表示
        if (showVoltage && voltageManager != null)
        {
            DrawVoltage();
        }
    }

    void DrawJudgmentText()
    {
        // 画面中央に大きく表示
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 48;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        // 影をつける
        GUI.color = Color.black;
        Rect shadowRect = new Rect(Screen.width / 2 - 152, Screen.height / 2 - 152, 304, 100);
        GUI.Label(shadowRect, currentJudgmentText, style);

        // メインテキスト
        GUI.color = currentJudgmentColor;
        Rect textRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 150, 300, 100);
        GUI.Label(textRect, currentJudgmentText, style);

        GUI.color = Color.white;
    }

    void DrawVoltage()
    {
        // float voltageValue = voltageManager.GetVoltageValue();
        // // float voltagePercent = voltageManager.GetNormalizedVoltageValue();

        // // 背景
        // Rect bgRect = new Rect(voltagePosition.x, voltagePosition.y, voltageBarWidth, voltageBarHeight);
        // GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        // GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

        // // Voltageバー
        // Rect voltageRect = new Rect(voltagePosition.x + 2, voltagePosition.y + 2,
        //                            (voltageBarWidth - 4) * voltagePercent, voltageBarHeight - 4);

        // // Voltageの色（値に応じて変化）
        // Color voltageColor = new Color(1f, 1f, 1f);

        // GUI.color = voltageColor;
        // GUI.DrawTexture(voltageRect, Texture2D.whiteTexture);

        // // Voltage値のテキスト
        // GUI.color = Color.white;
        // GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        // textStyle.fontSize = 18;
        // textStyle.fontStyle = FontStyle.Bold;
        // textStyle.alignment = TextAnchor.MiddleLeft;

        // Rect textRect = new Rect(voltagePosition.x + 10, voltagePosition.y + 3, voltageBarWidth, voltageBarHeight);
        // GUI.Label(textRect, $"VOLTAGE: {voltageValue:F1}%", textStyle);

        // GUI.color = Color.white;
    }
}
