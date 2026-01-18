using UnityEngine;
using System;

/// <summary>
/// Voltageリソースを管理
/// 判定結果に応じてVoltageが増加する（減少しない）
/// </summary>
public class VoltageManager : MonoBehaviour
{
    [Header("Voltage Settings")]
    [Tooltip("Voltageの初期値")]
    [SerializeField] private float initialVoltage = 0f;

    [Header("Voltage Gain Values")]
    [Tooltip("Perfect判定時のVoltage増加量")]
    [SerializeField] private float perfectGain = 2.0f;

    [Tooltip("Great判定時のVoltage増加量")]
    [SerializeField] private float greatGain = 1.5f;

    [Tooltip("Good判定時のVoltage増加量")]
    [SerializeField] private float goodGain = 0.5f;

    [Tooltip("Bad判定時のVoltage増加量（減少しない）")]
    [SerializeField] private float badGain = 0f;

    [Tooltip("Miss判定時のVoltage増加量（減少しない）")]
    [SerializeField] private float missGain = 0f;

    [Header("Display Settings")]
    [Tooltip("Voltageを画面に表示するか")]
    [SerializeField] private bool showVoltageDisplay = true;

    [Tooltip("表示位置")]
    [SerializeField] private Vector2 displayPosition = new Vector2(50, 50);

    [Tooltip("フォントサイズ")]
    [SerializeField] private int fontSize = 24;

    [Header("Current State")]
    [Tooltip("現在のVoltage値")]
    [SerializeField] private float currentVoltage;

    // イベント：Voltage変更時に通知
    public event Action<float> OnVoltageChanged;

    void Start()
    {
        currentVoltage = initialVoltage;
        OnVoltageChanged?.Invoke(currentVoltage);
    }

    /// <summary>
    /// 判定結果に基づいてVoltageを更新
    /// </summary>
    /// <param name="judgment">判定結果</param>
    public void UpdateVoltage(JudgmentResult judgment)
    {
        float previousVoltage = currentVoltage;

        switch (judgment)
        {
            case JudgmentResult.Perfect:
                currentVoltage += perfectGain;
                break;
            case JudgmentResult.Great:
                currentVoltage += greatGain;
                break;
            case JudgmentResult.Good:
                currentVoltage += goodGain;
                break;
            case JudgmentResult.Bad:
                currentVoltage += badGain; // 減少しない
                break;
            case JudgmentResult.Miss:
                currentVoltage += missGain; // 減少しない
                break;
        }
    }

    /// <summary>
    /// 現在のVoltage値を取得（0-100）
    /// </summary>
    public float GetVoltageValue()
    {
        return currentVoltage;
    }

    /// <summary>
    /// Voltageをリセット
    /// </summary>
    public void ResetVoltage()
    {
        currentVoltage = initialVoltage;
        OnVoltageChanged?.Invoke(currentVoltage);
    }

    void OnGUI()
    {
        if (!showVoltageDisplay) return;

        // Voltageテキストのスタイル
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperLeft;

        // 影（黒）
        GUI.color = Color.black;
        Rect shadowRect = new Rect(displayPosition.x + 2, displayPosition.y + 2, 300, 50);
        GUI.Label(shadowRect, $"VOLTAGE: {currentVoltage:F1}", style);

        // メインテキスト（色は値に応じて変化）
        Color voltageColor = new Color(1f, 1f, 1f);
        GUI.color = voltageColor;
        Rect textRect = new Rect(displayPosition.x, displayPosition.y, 300, 50);
        GUI.Label(textRect, $"VOLTAGE: {currentVoltage:F1}", style);

        GUI.color = Color.white;
    }
}
