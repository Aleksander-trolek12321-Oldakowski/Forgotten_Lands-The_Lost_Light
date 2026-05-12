using UnityEngine;
using UnityEngine.SceneManagement;
using Player;
using GameSave;

namespace Objects
{
    [RequireComponent(typeof(Collider))]
    public class ReturnToHubPortal : MonoBehaviour
    {
        [Header("Settings")]
        public string hubSceneName = "HUB";

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerBase player = other.GetComponent<PlayerBase>();
            if (player == null)
                player = other.GetComponentInParent<PlayerBase>();

            if (player == null) return;

            if (string.IsNullOrWhiteSpace(hubSceneName))
            {
                Debug.LogWarning("ReturnToHubPortal: hubSceneName is empty.");
                return;
            }

            SaveService.CaptureAndSave(
                targetSceneName: hubSceneName,
                includeHubPosition: false,
                clearCurrentSceneChestState: false,
                clearHubPositionWhenNotIncluded: true
            );

            SceneManager.LoadScene(hubSceneName);
        }
    }
}
