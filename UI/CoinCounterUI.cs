using UnityEngine;
using TMPro;

/// <summary>Shows the player's coin count from PlayerStats on a TMP label.</summary>
public class CoinCounterUI : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [SerializeField] private TMP_Text label;
    [Tooltip("Display format; {0} is the coin count. e.g. \"x{0}\" or \"{0} coins\".")]
    [SerializeField] private string format = "{0}";

    private void OnEnable()
    {
        if (stats == null || label == null) return;

        stats.EnsureInitialized();
        stats.OnCoinsChanged += HandleCoinsChanged;
        HandleCoinsChanged(stats.CurrentCoins);
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int coins)
    {
        label.text = string.Format(format, coins);
    }
}
