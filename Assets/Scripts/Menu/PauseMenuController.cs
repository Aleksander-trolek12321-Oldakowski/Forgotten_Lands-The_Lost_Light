using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Player;
using Inventory;
using GameSave;
using SideQuests;
using shop;
using chest;

namespace Menu
{
    public class PauseMenuController : MonoBehaviour
    {
        private const string LogPrefix = "[PauseMenuController]";

        [Header("UI")]
        public GameObject pauseRoot;
        public Button resumeButton;
        public Button menuButton;
        public Button hubButton;
        public bool debugLogs = true;

        [Header("Keys")]
        public KeyCode pauseKey = KeyCode.Escape;

        [Header("Scenes")]
        public string hubSceneName = SaveService.HubSceneName;
        public string menuSceneName = SaveService.MenuSceneName;

        private static PauseMenuController activeController;

        private bool isOpen = false;
        private bool wasShopOrChestUiOpenLastFrame = false;
        private PlayerBase player;
        private bool runtimeButtonsWired = false;
        private Canvas pauseCanvas;

        private void Awake()
        {
            Log($"Awake on '{name}' in scene '{SceneManager.GetActiveScene().name}'");
            RegisterAsActiveController();
            EnsurePauseRootReference();
            EnsureButtonReferences();
            WireRuntimeButtonCallbacks();
        }

        private void OnEnable()
        {
            Log("OnEnable: subscribe sceneLoaded");
            SceneManager.sceneLoaded += OnSceneLoaded;
            UpdateHubButtonState();
        }

        private void OnDisable()
        {
            Log("OnDisable: unsubscribe sceneLoaded");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (activeController == this)
                activeController = null;
        }

        private void Start()
        {
            Log("Start");
            player = FindObjectOfType<PlayerBase>();
            if (pauseRoot != null)
                pauseRoot.SetActive(false);

            WireRuntimeButtonCallbacks();
            UpdateHubButtonState();
            Log($"Start complete: pauseRoot={(pauseRoot != null ? pauseRoot.name : "<null>")}, resumeButton={(resumeButton != null ? resumeButton.name : "<null>")}, menuButton={(menuButton != null ? menuButton.name : "<null>")}, hubButton={(hubButton != null ? hubButton.name : "<null>")}");
        }

        private void Update()
        {
            bool shopOrChestUiOpenNow = IsShopOrChestUiOpen();

            if (Input.GetKeyDown(pauseKey))
            {
                if (pauseKey == KeyCode.Escape && InputBlocker.WasEscapeHandledByUiThisFrame())
                {
                    Log("Update: pause key ignored because Escape was already handled by UI this frame");
                    wasShopOrChestUiOpenLastFrame = shopOrChestUiOpenNow;
                    return;
                }

                // Build/editor can differ in script Update order.
                // If shop/chest UI was open in previous frame and now is closed, this Escape likely just closed that UI.
                bool escapeLikelyClosedUiThisFrame =
                    pauseKey == KeyCode.Escape &&
                    !shopOrChestUiOpenNow &&
                    wasShopOrChestUiOpenLastFrame;

                if (shopOrChestUiOpenNow || escapeLikelyClosedUiThisFrame)
                {
                    Log("Update: pause key ignored because shop/chest UI is open or was just closed");
                    wasShopOrChestUiOpenLastFrame = shopOrChestUiOpenNow;
                    return;
                }

                Log($"Update: pause key pressed. isOpen={isOpen}");
                if (isOpen) ResumeGame();
                else OpenPause();
            }

            if (isOpen && pauseRoot != null && pauseRoot.activeInHierarchy && Input.GetMouseButtonDown(0))
            {
                Log($"Update: manual mouse down at {Input.mousePosition}");
                TryHandleManualPauseButtonClick();
            }

            wasShopOrChestUiOpenLastFrame = shopOrChestUiOpenNow;
        }

        public void OpenPause()
        {
            Log("OpenPause called");
            PauseMenuController controller = ResolveController(this);
            if (controller != null && controller != this)
            {
                Log($"OpenPause redirected to active controller '{controller.name}'");
                controller.OpenPause();
                return;
            }

            if (isOpen) return;
            isOpen = true;

            if (player == null) player = FindObjectOfType<PlayerBase>();
            InputBlocker.Block(player);
            if (player != null) player.SetControlsEnabled(false);

            if (pauseRoot != null) pauseRoot.SetActive(true);

            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;

            UpdateHubButtonState();
            Log("OpenPause complete: timeScale=0, cursor visible/unlocked");
        }

        public void ResumeGame()
        {
            Log("ResumeGame called");
            PauseMenuController controller = ResolveController(this);
            if (controller != null && controller != this)
            {
                Log($"ResumeGame redirected to active controller '{controller.name}'");
                controller.ResumeGame();
                return;
            }

            if (player == null) player = FindObjectOfType<PlayerBase>();

            CloseAllPausePanels();

            Time.timeScale = 1f;

            InputBlocker.Restore(player);
            if (player != null) player.SetControlsEnabled(true);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Log("ResumeGame complete: timeScale=1, cursor hidden/locked");
        }

        public void ReturnToHub()
        {
            Log($"ReturnToHub called from scene '{SceneManager.GetActiveScene().name}'");
            PauseMenuController controller = ResolveController(this);
            if (controller != null && controller != this)
            {
                Log($"ReturnToHub redirected to active controller '{controller.name}'");
                controller.ReturnToHub();
                return;
            }

            if (IsInHubScene())
            {
                Log("ReturnToHub: already in HUB, resume instead");
                ResumeGame();
                return;
            }

            TryPrepareAndSaveForExit();
            Log($"ReturnToHub: LoadScene('{hubSceneName}')");
            SceneManager.LoadScene(hubSceneName);
        }

        public void ReturnToMenu()
        {
            Log($"ReturnToMenu called from scene '{SceneManager.GetActiveScene().name}'");
            PauseMenuController controller = ResolveController(this);
            if (controller != null && controller != this)
            {
                Log($"ReturnToMenu redirected to active controller '{controller.name}'");
                controller.ReturnToMenu();
                return;
            }

            TryPrepareAndSaveForExit();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;

            Log($"ReturnToMenu: LoadScene('{menuSceneName}')");
            SceneManager.LoadScene(menuSceneName);
        }

        private void TryPrepareAndSaveForExit()
        {
            Log("TryPrepareAndSaveForExit: begin");
            try
            {
                PrepareAndSaveForExit();
                Log("TryPrepareAndSaveForExit: success");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"PauseMenuController: save before scene switch failed. {ex.Message}");
                Time.timeScale = 1f;
                CloseAllPausePanels();
                Log($"TryPrepareAndSaveForExit: exception -> {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void PrepareAndSaveForExit()
        {
            Time.timeScale = 1f;

            if (player == null) player = FindObjectOfType<PlayerBase>();
            InputBlocker.Restore(player);
            if (player != null) player.SetControlsEnabled(true);
            SideQuestManager.Instance?.ResetActiveTimedQuestTimer();

            string currentScene = SceneManager.GetActiveScene().name;
            bool inHub = string.Equals(currentScene, hubSceneName, System.StringComparison.OrdinalIgnoreCase);
            Log($"PrepareAndSaveForExit: scene='{currentScene}', inHub={inHub}, targetContinue='{hubSceneName}'");

            SaveService.CaptureAndSave(
                targetSceneName: hubSceneName,
                includeHubPosition: inHub,
                clearCurrentSceneChestState: !inHub,
                clearHubPositionWhenNotIncluded: !inHub
            );
            Log("PrepareAndSaveForExit: SaveService.CaptureAndSave done");

            CloseAllPausePanels();
            Log("PrepareAndSaveForExit: pause panels closed");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log($"OnSceneLoaded: scene='{scene.name}', mode={mode}");
            EnsureButtonReferences();
            WireRuntimeButtonCallbacks();
            UpdateHubButtonState();
        }

        private bool IsInHubScene()
        {
            return string.Equals(SceneManager.GetActiveScene().name, hubSceneName, System.StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateHubButtonState()
        {
            if (hubButton == null) return;
            hubButton.interactable = !IsInHubScene();
            Log($"UpdateHubButtonState: hubButton.interactable={hubButton.interactable} inScene='{SceneManager.GetActiveScene().name}'");
        }

        private void RegisterAsActiveController()
        {
            if (activeController == null)
            {
                activeController = this;
                return;
            }

            if (activeController == this)
                return;

            if (pauseRoot != null && activeController.pauseRoot == null)
                activeController = this;
        }

        private static PauseMenuController ResolveController(PauseMenuController caller)
        {
            if (activeController != null)
                return activeController;

            PauseMenuController[] controllers = FindObjectsOfType<PauseMenuController>();
            PauseMenuController withRoot = null;

            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] == null) continue;
                if (controllers[i].pauseRoot != null)
                {
                    withRoot = controllers[i];
                    break;
                }
            }

            activeController = withRoot != null ? withRoot : caller;
            return activeController;
        }

        private static void CloseAllPausePanels()
        {
            PauseMenuController[] controllers = FindObjectsOfType<PauseMenuController>();
            Debug.Log($"{LogPrefix} CloseAllPausePanels: found {controllers.Length} controller(s)");
            for (int i = 0; i < controllers.Length; i++)
            {
                PauseMenuController c = controllers[i];
                if (c == null) continue;

                c.isOpen = false;
                if (c.pauseRoot != null)
                    c.pauseRoot.SetActive(false);

                if (c.debugLogs)
                    Debug.Log($"{LogPrefix} CloseAllPausePanels: closed '{c.name}'");
            }
        }

        private void EnsurePauseRootReference()
        {
            if (pauseRoot != null) return;

            Transform pausePanel = transform.Find("PausePanel");
            if (pausePanel != null)
                pauseRoot = pausePanel.gameObject;

            Log($"EnsurePauseRootReference: pauseRoot={(pauseRoot != null ? pauseRoot.name : "<null>")}");
        }

        private void EnsureButtonReferences()
        {
            if (pauseRoot == null) return;
            if (pauseCanvas == null) pauseCanvas = pauseRoot.GetComponentInParent<Canvas>();

            Button[] buttons = pauseRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button b = buttons[i];
                if (b == null) continue;

                string n = b.gameObject.name;
                if (string.IsNullOrEmpty(n)) continue;

                string lower = n.ToLowerInvariant();

                if (resumeButton == null && (lower.Contains("return") || lower.Contains("resume")))
                    resumeButton = b;

                if (menuButton == null && lower.Contains("menu"))
                    menuButton = b;

                if (hubButton == null && lower.Contains("hub"))
                    hubButton = b;
            }

            Log($"EnsureButtonReferences: resume={(resumeButton != null ? resumeButton.name : "<null>")}, menu={(menuButton != null ? menuButton.name : "<null>")}, hub={(hubButton != null ? hubButton.name : "<null>")}");
        }

        private void WireRuntimeButtonCallbacks()
        {
            if (runtimeButtonsWired) return;

            bool wiredAny = false;

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeGame);
                AddPointerDownFallback(resumeButton, ResumeGame);
                wiredAny = true;
            }

            if (menuButton != null)
            {
                menuButton.onClick.AddListener(ReturnToMenu);
                AddPointerDownFallback(menuButton, ReturnToMenu);
                wiredAny = true;
            }

            if (hubButton != null)
            {
                hubButton.onClick.AddListener(ReturnToHub);
                AddPointerDownFallback(hubButton, ReturnToHub);
                wiredAny = true;
            }

            runtimeButtonsWired = wiredAny;
            Log($"WireRuntimeButtonCallbacks: wiredAny={wiredAny}");
        }

        private static void AddPointerDownFallback(Button button, UnityAction action)
        {
            if (button == null || action == null) return;

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            entry.callback.AddListener(_ =>
            {
                if (button != null && button.IsInteractable())
                    action.Invoke();
            });
            trigger.triggers.Add(entry);
        }

        private void TryHandleManualPauseButtonClick()
        {
            Camera eventCamera = null;
            if (pauseCanvas != null && pauseCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = pauseCanvas.worldCamera;

            Vector2 mousePos = Input.mousePosition;
            Log($"TryHandleManualPauseButtonClick: mouse={mousePos}");

            if (IsButtonHit(resumeButton, mousePos, eventCamera))
            {
                Log("Manual hit: resumeButton");
                ResumeGame();
                return;
            }

            if (IsButtonHit(menuButton, mousePos, eventCamera))
            {
                Log("Manual hit: menuButton");
                ReturnToMenu();
                return;
            }

            if (IsButtonHit(hubButton, mousePos, eventCamera))
            {
                Log("Manual hit: hubButton");
                ReturnToHub();
                return;
            }

            Log("Manual hit: no pause button");
        }

        private static bool IsButtonHit(Button button, Vector2 screenPos, Camera eventCamera)
        {
            if (button == null || !button.IsInteractable() || !button.gameObject.activeInHierarchy)
                return false;

            RectTransform rt = button.transform as RectTransform;
            if (rt == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, eventCamera);
        }

        private bool IsShopOrChestUiOpen()
        {
            ShopUIController shopUi = FindObjectOfType<ShopUIController>();
            if (shopUi != null && shopUi.inventoryRoot != null && shopUi.inventoryRoot.activeInHierarchy)
                return true;

            ChestUIController chestUi = FindObjectOfType<ChestUIController>();
            if (chestUi != null && chestUi.inventoryRoot != null && chestUi.inventoryRoot.activeInHierarchy)
                return true;

            return false;
        }

        private void Log(string message)
        {
            if (!debugLogs) return;
            Debug.Log($"{LogPrefix} {message}");
        }
    }
}
