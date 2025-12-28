using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameplayManager : StaticMonoBehaviour<GameplayManager>
{
    public UnityEvent OnGameOver;
    private bool isGameOver = false;
    private bool gameRestarted = false;

    private InputSystemActions input;

    [SerializeField] GameObject resumeButton;
    [SerializeField] GameObject gameMenuUI;

    protected override void Awake()
    {
        base.Awake();
        input = new InputSystemActions();
        input.Enable();
        input.Player.Pause.performed += ToggleMenu;
    }

    /// <summary>
    /// 게임을 게임오버상태로 만듭니다.
    /// </summary>
    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        OnGameOver?.Invoke();
    }

    /// <summary>
    /// 게임 애플리케이션을 즉시 종료합니다.
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// 게임의 시간을 멈추고 일시정지 메뉴를 엽니다.
    /// </summary>
    public void PauseGame()
    {
        PlayerController.Instance.Input.Disable();
        gameMenuUI.SetActive(true);
        EventSystem.current.SetSelectedGameObject(resumeButton);
        TimeManager.Instance.PauseTime();
    }

    /// <summary>
    /// 일시정지 메뉴를 닫고 게임의 시간을 되돌립니다.
    /// </summary>
    public void ResumeGame()
    {
        PlayerController.Instance.Input.Enable();
        gameMenuUI.SetActive(false);
        TimeManager.Instance.UnpauseTime();
    }

    const float restartKeyHold = 0.5f;
    float restartKeyHoldTimer = 0f;


    private void OnDisable()
    {
        input.Player.Pause.performed -= ToggleMenu;
    }

    public void Update()
    {
        if (isGameOver)
        {
            if (gameRestarted) return;

            if (input.Player.Restart.IsPressed())
                restartKeyHoldTimer += Time.deltaTime;

            if (restartKeyHoldTimer >= restartKeyHold)
            {
                gameRestarted = true;
                SceneLoader.Instance.LoadNewScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }

        }
    }

    public void ToggleMenu(InputAction.CallbackContext context)
    {
        if (isGameOver) return;

        if (gameMenuUI.activeInHierarchy)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }

    }

    private void OnDestroy()
    {
        input.Player.Pause.performed -= ToggleMenu;
    }
}
