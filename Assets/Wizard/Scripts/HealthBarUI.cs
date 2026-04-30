using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HealthBarUI : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerController playerController;

    [Header("Health")]
    [SerializeField] private Sprite[] healthSprites;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image healthDelayImage;
    [SerializeField] private Text healthText;
    [SerializeField] private Color highHealthColor = new Color(0.2f, 0.95f, 0.65f, 1f);
    [SerializeField] private Color midHealthColor = new Color(1f, 0.78f, 0.24f, 1f);
    [SerializeField] private Color lowHealthColor = new Color(1f, 0.22f, 0.18f, 1f);

    [Header("Magic")]
    [SerializeField] private Image magicIconImage;
    [SerializeField] private Image magicGlowImage;
    [SerializeField] private Text magicNameText;
    [SerializeField] private Sprite fallbackMagicIcon;

    private Image image;
    private float healthPercent = 1f;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += UpdateHealthBar;
            UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        if (playerController != null)
        {
            playerController.MagicChanged += UpdateMagicDisplay;
            UpdateMagicDisplay(playerController.CurrentMagicIndex, playerController.CurrentMagicName, playerController.CurrentMagicIcon);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdateHealthBar;
        }

        if (playerController != null)
        {
            playerController.MagicChanged -= UpdateMagicDisplay;
        }
    }

    private void Update()
    {
        if (healthDelayImage == null)
        {
            return;
        }

        healthDelayImage.fillAmount = Mathf.MoveTowards(healthDelayImage.fillAmount, healthPercent, Time.deltaTime * 0.65f);
    }

    public void SetPlayerHealth(PlayerHealth health)
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdateHealthBar;
        }

        playerHealth = health;

        if (isActiveAndEnabled && playerHealth != null)
        {
            playerHealth.HealthChanged += UpdateHealthBar;
            UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    public void SetPlayerController(PlayerController controller)
    {
        if (playerController != null)
        {
            playerController.MagicChanged -= UpdateMagicDisplay;
        }

        playerController = controller;

        if (isActiveAndEnabled && playerController != null)
        {
            playerController.MagicChanged += UpdateMagicDisplay;
            UpdateMagicDisplay(playerController.CurrentMagicIndex, playerController.CurrentMagicName, playerController.CurrentMagicIcon);
        }
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        healthPercent = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        Color healthColor = GetHealthColor(healthPercent);

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = healthPercent;
            healthFillImage.color = healthColor;
        }

        if (healthDelayImage != null && healthDelayImage.fillAmount < healthPercent)
        {
            healthDelayImage.fillAmount = healthPercent;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }

        if (healthFillImage != null)
        {
            return;
        }

        if (image == null || healthSprites == null || healthSprites.Length == 0)
        {
            return;
        }

        int spriteIndex = Mathf.RoundToInt((1f - healthPercent) * (healthSprites.Length - 1));
        image.sprite = healthSprites[Mathf.Clamp(spriteIndex, 0, healthSprites.Length - 1)];
    }

    private void UpdateMagicDisplay(int index, string magicName, Sprite icon)
    {
        Sprite resolvedIcon = icon != null ? icon : fallbackMagicIcon;

        if (magicIconImage != null)
        {
            magicIconImage.sprite = resolvedIcon;
            magicIconImage.enabled = resolvedIcon != null;
            magicIconImage.color = Color.white;
        }

        Color magicColor = GetMagicColor(magicName);
        if (magicGlowImage != null)
        {
            magicGlowImage.color = magicColor;
        }

        if (magicNameText != null)
        {
            magicNameText.text = string.IsNullOrWhiteSpace(magicName) ? "Nenhuma" : magicName;
            magicNameText.color = magicColor;
        }
    }

    private Color GetHealthColor(float percent)
    {
        if (percent > 0.55f)
        {
            return Color.Lerp(midHealthColor, highHealthColor, Mathf.InverseLerp(0.55f, 1f, percent));
        }

        return Color.Lerp(lowHealthColor, midHealthColor, Mathf.InverseLerp(0.2f, 0.55f, percent));
    }

    private Color GetMagicColor(string magicName)
    {
        if (string.IsNullOrWhiteSpace(magicName))
        {
            return new Color(0.65f, 0.85f, 1f, 1f);
        }

        string normalizedName = magicName.ToLowerInvariant();
        if (normalizedName.Contains("fogo"))
        {
            return new Color(1f, 0.38f, 0.18f, 1f);
        }

        if (normalizedName.Contains("gelo"))
        {
            return new Color(0.35f, 0.9f, 1f, 1f);
        }

        if (normalizedName.Contains("raio"))
        {
            return new Color(1f, 0.88f, 0.22f, 1f);
        }

        return new Color(0.72f, 0.5f, 1f, 1f);
    }
}
