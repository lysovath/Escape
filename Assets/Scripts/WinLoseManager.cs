using UnityEngine;
using UnityEngine.SceneManagement;

public class WinLoseManager : MonoBehaviour
{
    public static WinLoseManager Instance { get; private set; }

    [Header("UI Banners")]
    [SerializeField] private GameObject winBanner;
    [SerializeField] private GameObject loseBanner;

    [Header("Scenes")]
    [SerializeField] private string lobbySceneName = "Lobby";

    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (winBanner != null) winBanner.SetActive(false);
        if (loseBanner != null) loseBanner.SetActive(false);
        Time.timeScale = 1f;
    }

    public void WinGame()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (winBanner != null) winBanner.SetActive(true);
        PauseAndUnlockCursor();
    }

    public void LoseGame()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (loseBanner != null) loseBanner.SetActive(true);
        PauseAndUnlockCursor();
    }

    private void PauseAndUnlockCursor()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void PlayAgain() => RestartCurrentScene();
    public void TryAgain() => RestartCurrentScene();

    private void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(lobbySceneName);
    }

}
