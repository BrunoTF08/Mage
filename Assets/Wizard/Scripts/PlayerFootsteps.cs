using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    private enum FootstepMode
    {
        None,
        Walk,
        Run
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private AudioSource audioSource;

    [Header("Audio")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float runVolume = 0.65f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);
    [SerializeField] private float minHorizontalSpeed = 0.05f;

    [Header("Animation States")]
    [SerializeField] private string[] walkStateNames =
    {
        "BattleWalkForward",
        "BattleWalkBack",
        "BattleWalkRight",
        "BattleWalkLeft"
    };

    [SerializeField] private string[] runStateNames =
    {
        "BattleRunForward"
    };

    [Header("Step Timing")]
    [SerializeField] private Vector2 walkStepTimes = new Vector2(0.16f, 0.66f);
    [SerializeField] private Vector2 runStepTimes = new Vector2(0.12f, 0.58f);

    private int lastStateHash;
    private int lastLoop = -1;
    private bool playedFirstStep;
    private bool playedSecondStep;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ConfigureAudioSource();
    }

    private void LateUpdate()
    {
        if (!CanPlayFootsteps())
        {
            ResetStepCycle();
            return;
        }

        AnimatorStateInfo stateInfo = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);

        FootstepMode mode = GetFootstepMode(stateInfo);
        if (mode == FootstepMode.None)
        {
            ResetStepCycle();
            return;
        }

        int currentLoop = Mathf.FloorToInt(stateInfo.normalizedTime);
        if (lastStateHash != stateInfo.shortNameHash || lastLoop != currentLoop)
        {
            lastStateHash = stateInfo.shortNameHash;
            lastLoop = currentLoop;
            playedFirstStep = false;
            playedSecondStep = false;
        }

        float cycleTime = Mathf.Repeat(stateInfo.normalizedTime, 1f);
        Vector2 stepTimes = mode == FootstepMode.Run ? runStepTimes : walkStepTimes;

        if (!playedFirstStep && cycleTime >= stepTimes.x)
        {
            PlayFootstep(mode);
            playedFirstStep = true;
        }

        if (!playedSecondStep && cycleTime >= stepTimes.y)
        {
            PlayFootstep(mode);
            playedSecondStep = true;
        }
    }

    public void PlayFootstep()
    {
        PlayFootstep(FootstepMode.Walk);
    }

    private bool CanPlayFootsteps()
    {
        if (animator == null || audioSource == null || footstepClips == null || footstepClips.Length == 0)
        {
            return false;
        }

        if (characterController != null)
        {
            Vector3 horizontalVelocity = characterController.velocity;
            horizontalVelocity.y = 0f;

            return characterController.isGrounded && horizontalVelocity.sqrMagnitude >= minHorizontalSpeed * minHorizontalSpeed;
        }

        return true;
    }

    private FootstepMode GetFootstepMode(AnimatorStateInfo stateInfo)
    {
        if (MatchesAnyState(stateInfo, runStateNames))
        {
            return FootstepMode.Run;
        }

        if (MatchesAnyState(stateInfo, walkStateNames))
        {
            return FootstepMode.Walk;
        }

        return FootstepMode.None;
    }

    private static bool MatchesAnyState(AnimatorStateInfo stateInfo, string[] stateNames)
    {
        if (stateNames == null)
        {
            return false;
        }

        for (int i = 0; i < stateNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(stateNames[i]) && stateInfo.IsName(stateNames[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void PlayFootstep(FootstepMode mode)
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip == null)
        {
            return;
        }

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, mode == FootstepMode.Run ? runVolume : walkVolume);
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 18f;
    }

    private void ResetStepCycle()
    {
        lastStateHash = 0;
        lastLoop = -1;
        playedFirstStep = false;
        playedSecondStep = false;
    }
}
