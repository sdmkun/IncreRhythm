using UnityEngine;
using System.Collections;

/// <summary>
/// ゲームプレイ中のUI表示を管理
/// 判定結果とグルーブゲージの表示
/// </summary>
public class GameplayUI : MonoBehaviour
{
    [Header("Judgment Display")]
    [SerializeField] private bool showJudgmentText = true;
    [SerializeField] private float judgmentDisplayDuration = 0.5f;

    [Header("Groove Gauge Display")]
    [SerializeField] private bool showGrooveGauge = true;
    [SerializeField] private float gaugeBarWidth = 400f;
    [SerializeField] private float gaugeBarHeight = 30f;
    [SerializeField] private Vector2 gaugePosition = new Vector2(50, 50);

    [Header("References")]
    [SerializeField] private GrooveGaugeManager grooveGaugeManager;

    // 内部変数
    private string currentJudgmentText = "";
    private Color currentJudgmentColor = Color.white;
    private float judgmentDisplayTimer = 0f;

    void Start()
    {
        if (grooveGaugeManager == null)
        {
            grooveGaugeManager = FindFirstObjectByType<GrooveGaugeManager>();
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

        // グルーブゲージの表示
        if (showGrooveGauge && grooveGaugeManager != null)
        {
            DrawGrooveGauge();
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

    void DrawGrooveGauge()
    {
        float gaugeValue = grooveGaugeManager.GetGaugeValue();
        float gaugePercent = grooveGaugeManager.GetNormalizedGaugeValue();

        // 背景
        Rect bgRect = new Rect(gaugePosition.x, gaugePosition.y, gaugeBarWidth, gaugeBarHeight);
        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

        // ゲージバー
        Rect gaugeRect = new Rect(gaugePosition.x + 2, gaugePosition.y + 2,
                                   (gaugeBarWidth - 4) * gaugePercent, gaugeBarHeight - 4);

        // ゲージの色（値に応じて変化）
        Color gaugeColor;
        if (gaugePercent >= 0.8f)
        {
            gaugeColor = new Color(0f, 1f, 0.5f); // 高い：明るい緑
        }
        else if (gaugePercent >= 0.5f)
        {
            gaugeColor = new Color(0.5f, 1f, 0.5f); // 中間：緑
        }
        else if (gaugePercent >= 0.3f)
        {
            gaugeColor = new Color(1f, 1f, 0f); // やや低い：黄色
        }
        else
        {
            gaugeColor = new Color(1f, 0.5f, 0f); // 低い：オレンジ～赤
        }

        GUI.color = gaugeColor;
        GUI.DrawTexture(gaugeRect, Texture2D.whiteTexture);

        // ゲージ値のテキスト
        GUI.color = Color.white;
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 18;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.alignment = TextAnchor.MiddleLeft;

        Rect textRect = new Rect(gaugePosition.x + 10, gaugePosition.y + 3, gaugeBarWidth, gaugeBarHeight);
        GUI.Label(textRect, $"GROOVE: {gaugeValue:F1}%", textStyle);

        GUI.color = Color.white;
    }
}
