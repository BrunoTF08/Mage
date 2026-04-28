using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    public event Action<float, float> HealthChanged;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsAlive => currentHealth > 0f;

    private PlayerController playerController;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        NotifyHealthChanged();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        float previousHealth = currentHealth;
        SetHealth(currentHealth - amount);

        if (currentHealth < previousHealth && IsAlive && playerController != null)
        {
            playerController.PlayHitReaction();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetHealth(currentHealth + amount);
    }

    public void SetHealth(float value)
    {
        float newHealth = Mathf.Clamp(value, 0f, maxHealth);
        if (Mathf.Approximately(currentHealth, newHealth))
        {
            return;
        }

        currentHealth = newHealth;
        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
