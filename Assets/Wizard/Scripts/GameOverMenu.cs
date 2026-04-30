using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private float showDelay = 1.6f;
    [SerializeField] private float buttonEnableDelay = 0.35f;
    [SerializeField] private bool pauseWhenShown = true;

    private Coroutine showRoutine;
    private bool subscribedToDeath;

    private void Awake()
    {
        Time.timeScale = 1f;

        ResolvePlayerHealth();
        HidePanel();

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(Restart);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(Exit);
        }

        SubscribeToPlayerDeath();
    }

    private void OnEnable()
    {
        ResolvePlayerHealth();
        SubscribeToPlayerDeath();
    }

    private void Start()
    {
        ResolvePlayerHealth();
        SubscribeToPlayerDeath();

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            HandlePlayerDied();
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null && subscribedToDeath)
        {
            playerHealth.Died -= HandlePlayerDied;
            subscribedToDeath = false;
        }
    }

    private void HandlePlayerDied()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }

        showRoutine = StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(showDelay);

        SetButtonsInteractable(false);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseWhenShown)
        {
            Time.timeScale = 0f;
        }

        yield return new WaitForSecondsRealtime(buttonEnableDelay);
        SetButtonsInteractable(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    public void Exit()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResolvePlayerHealth()
    {
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }
    }

    private void SubscribeToPlayerDeath()
    {
        if (playerHealth == null || subscribedToDeath)
        {
            return;
        }

        playerHealth.Died += HandlePlayerDied;
        subscribedToDeath = true;
    }

    private void HidePanel()
    {
        SetButtonsInteractable(false);

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (restartButton != null)
        {
            restartButton.interactable = interactable;
        }

        if (exitButton != null)
        {
            exitButton.interactable = interactable;
        }
    }
}
