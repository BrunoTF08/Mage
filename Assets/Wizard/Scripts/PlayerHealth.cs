using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Voice")]
    [SerializeField] private AudioClip[] damageVoiceClips;
    [SerializeField] private AudioClip deathVoiceClip;
    [SerializeField, Range(0f, 1f)] private float damageVoiceVolume = 0.85f;
    [SerializeField, Range(0f, 1f)] private float deathVoiceVolume = 1f;
    [SerializeField] private float damageVoiceCooldown = 0.35f;

    public event Action<float, float> HealthChanged;
    public event Action Died;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsAlive => currentHealth > 0f;

    private PlayerController playerController;
    private float nextDamageVoiceTime;
    private bool deathNotified;

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
        if (amount <= 0f || deathNotified)
        {
            return;
        }

        float previousHealth = currentHealth;
        SetHealth(currentHealth - amount);

        if (currentHealth >= previousHealth)
        {
            return;
        }

        if (IsAlive)
        {
            PlayDamageVoice();

            if (playerController != null)
            {
                playerController.PlayHitReaction();
            }

            return;
        }

        Die();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        if (!IsAlive && amount > 0f)
        {
            deathNotified = false;
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

    private void Die()
    {
        if (deathNotified)
        {
            return;
        }

        deathNotified = true;
        PlayDeathVoice();

        if (playerController != null)
        {
            playerController.PlayDeathReaction();
        }

        Died?.Invoke();
    }

    private void PlayDamageVoice()
    {
        if (damageVoiceClips == null || damageVoiceClips.Length == 0 || Time.time < nextDamageVoiceTime)
        {
            return;
        }

        AudioClip clip = damageVoiceClips[UnityEngine.Random.Range(0, damageVoiceClips.Length)];
        PlayVoiceClip(clip, damageVoiceVolume);
        nextDamageVoiceTime = Time.time + damageVoiceCooldown;
    }

    private void PlayDeathVoice()
    {
        PlayVoiceClip(deathVoiceClip, deathVoiceVolume);
    }

    private void PlayVoiceClip(AudioClip clip, float volume)
    {
        if (clip == null || volume <= 0f)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    }
}
