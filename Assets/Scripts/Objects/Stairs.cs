using UnityEngine.SceneManagement;
using Player;
using UnityEngine;

namespace Objects
{
[RequireComponent(typeof(Collider))]
public class Stairs : MonoBehaviour
    {
        [Header("Reference to Statue (level selection)")]
        public Statue statue;

        [Header("Visuals")]
        public Renderer stairsRenderer;
        public Color inactiveColor = Color.black;
        public Color activeColor = Color.yellow;
        public bool useMaterial = false;
        public Material inactiveMaterial;
        public Material activeMaterial;

        [Header("Trigger settings")]
        public bool requireTrigger = true;

        bool isActive = false;
        string currentChosen = "";
        Collider collider;

        private void Reset()
        {
            collider = GetComponent<Collider>();
            if (collider != null && requireTrigger)
                collider.isTrigger = true;
        }

        private void Start()
        {
            collider = GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogError("Stairs: No Collider attached to the stairs object.");
            }
            else if (requireTrigger)
            {
                collider.isTrigger = true;
            }

            // try to find a Statue instance automatically if not set
            if (statue == null)
            {
                statue = FindObjectOfType<Statue>();
                if (statue == null)
                {
                    Debug.LogWarning("Stairs: No Statue found in the scene..");
                }
            }

            currentChosen = statue != null ? statue.ChoosenLevel : "";
            UpdateActiveState();
            UpdateVisual(isActive);
        }

        private void Update()
        {
            string newChosen = statue != null ? statue.ChoosenLevel : "";
            if (newChosen != currentChosen)
            {
                currentChosen = newChosen;
                UpdateActiveState();
                UpdateVisual(isActive);
            }
        }

        void UpdateActiveState()
        {
            isActive = !string.IsNullOrEmpty(currentChosen);
        }

        void UpdateVisual(bool active)
        {
            if (stairsRenderer == null) return;

            if (useMaterial)
            {
                if (active && activeMaterial != null)
                    stairsRenderer.material = activeMaterial;
                else if (!active && inactiveMaterial != null)
                    stairsRenderer.material = inactiveMaterial;
            }
            else
            {
                // ensure material instance exists before changing color
                if (stairsRenderer.material != null)
                {
                    stairsRenderer.material.color = active ? activeColor : inactiveColor;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive)
            {
                Debug.Log("Stairs: stairs is inactive(no level selected) - nothing happens");
                return;
            }

            PlayerBase player = other.GetComponent<PlayerBase>();
            if (player == null) return;

            if (string.IsNullOrEmpty(currentChosen))
            {
                Debug.LogWarning("Stairs: currentChosen is empty when player entered the stairs.");
                return;
            }

            Debug.Log($"Stairs: player entered active stairs - loading scene '{currentChosen}'");
            SceneManager.LoadScene(currentChosen);
        }
    }
}
