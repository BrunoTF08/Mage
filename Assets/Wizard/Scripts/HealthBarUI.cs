using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Sprite[] healthSprites;

    private Image image;

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
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdateHealthBar;
        }
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

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        if (image == null || healthSprites == null || healthSprites.Length == 0)
        {
            return;
        }

        float percent = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        int spriteIndex = Mathf.RoundToInt((1f - percent) * (healthSprites.Length - 1));
        image.sprite = healthSprites[Mathf.Clamp(spriteIndex, 0, healthSprites.Length - 1)];
    }
}
