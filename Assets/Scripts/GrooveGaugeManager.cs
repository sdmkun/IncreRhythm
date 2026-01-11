using UnityEngine;
using System;

/// <summary>
/// Beatmaniaスタイルのグルーブゲージを管理
/// 判定結果に応じてゲージが増減する
/// </summary>
public class GrooveGaugeManager : MonoBehaviour
{
    [Header("Gauge Settings")]
    [Tooltip("ゲージの最小値")]
    [SerializeField] private float minGauge = 0f;

    [Tooltip("ゲージの最大値")]
    [SerializeField] private float maxGauge = 100f;

    [Tooltip("ゲージの初期値")]
    [SerializeField] private float initialGauge = 20f;

    [Header("Gauge Change Values")]
    [Tooltip("Perfect判定時のゲージ増加量")]
    [SerializeField] private float perfectGain = 2.0f;

    [Tooltip("Great判定時のゲージ増加量")]
    [SerializeField] private float greatGain = 1.5f;

    [Tooltip("Good判定時のゲージ増加量")]
    [SerializeField] private float goodGain = 0.5f;

    [Tooltip("Bad判定時のゲージ減少量")]
    [SerializeField] private float badLoss = 2.0f;

    [Tooltip("Miss判定時のゲージ減少量")]
    [SerializeField] private float missLoss = 4.0f;

    [Header("Current State")]
    [Tooltip("現在のゲージ値（0-100）")]
    [SerializeField] private float currentGauge;

    // イベント：ゲージ変更時に通知
    public event Action<float> OnGaugeChanged;

    void Start()
    {
        currentGauge = initialGauge;
        OnGaugeChanged?.Invoke(currentGauge);
    }

    /// <summary>
    /// 判定結果に基づいてゲージを更新
    /// </summary>
    /// <param name="judgment">判定結果</param>
    public void UpdateGauge(JudgmentResult judgment)
    {
        float previousGauge = currentGauge;

        switch (judgment)
        {
            case JudgmentResult.Perfect:
                currentGauge += perfectGain;
                break;
            case JudgmentResult.Great:
                currentGauge += greatGain;
                break;
            case JudgmentResult.Good:
                currentGauge += goodGain;
                break;
            case JudgmentResult.Bad:
                currentGauge -= badLoss;
                break;
            case JudgmentResult.Miss:
                currentGauge -= missLoss;
                break;
        }

        // ゲージを範囲内に制限
        currentGauge = Mathf.Clamp(currentGauge, minGauge, maxGauge);

        // ゲージが変化した場合はイベントを発火
        if (Mathf.Abs(currentGauge - previousGauge) > 0.01f)
        {
            OnGaugeChanged?.Invoke(currentGauge);
            Debug.Log($"<color=green>Groove Gauge: {currentGauge:F1}%</color> ({judgment})");
        }
    }

    /// <summary>
    /// 現在のゲージ値を取得（0-100）
    /// </summary>
    public float GetGaugeValue()
    {
        return currentGauge;
    }

    /// <summary>
    /// 現在のゲージ値を正規化して取得（0.0-1.0）
    /// </summary>
    public float GetNormalizedGaugeValue()
    {
        return currentGauge / maxGauge;
    }

    /// <summary>
    /// ゲージをリセット
    /// </summary>
    public void ResetGauge()
    {
        currentGauge = initialGauge;
        OnGaugeChanged?.Invoke(currentGauge);
    }

    /// <summary>
    /// ゲージが空かどうかをチェック
    /// </summary>
    public bool IsGaugeEmpty()
    {
        return currentGauge <= minGauge;
    }

    /// <summary>
    /// ゲージが満タンかどうかをチェック
    /// </summary>
    public bool IsGaugeFull()
    {
        return currentGauge >= maxGauge;
    }
}
