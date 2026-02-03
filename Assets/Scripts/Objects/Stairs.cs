using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Player;
using Cinemachine;

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
        public Color activeColor = new Color(1f, 0.85f, 0.4f);
        public bool useMaterial = false;
        public Material inactiveMaterial;
        public Material activeMaterial;

        [Header("Transition settings")]
        public bool requireTrigger = true;
        public float transitionDuration = 1.2f;
        public ParticleSystem gateParticles;
        public ParticleSystem beamParticles;
        public ParticleSystem dustParticles;
        public AudioSource gateAudio;
        public Animator gateAnimator;

        [Header("Cinemachine")]
        public CinemachineVirtualCamera zoomCamera;
        public int zoomPriority = 60;
        public float zoomDurationRealtime = 0.8f;
        public CinemachineImpulseSource impulseSource;

        bool isActive = false;
        string currentChosen = "";
        Collider colliderRef;
        bool isTransitioning = false;

        private void Reset()
        {
            colliderRef = GetComponent<Collider>();
            if (colliderRef != null && requireTrigger)
                colliderRef.isTrigger = true;
        }

        private void Start()
        {
            colliderRef = GetComponent<Collider>();
            if (colliderRef == null)
            {
                Debug.LogError("Stairs: No Collider attached to the stairs object.");
            }
            else if (requireTrigger)
            {
                colliderRef.isTrigger = true;
            }

            if (statue == null)
            {
                statue = FindObjectOfType<Statue>();
                if (statue == null)
                    Debug.LogWarning("Stairs: No Statue found in the scene..");
            }

            currentChosen = statue != null ? statue.ChoosenLevel : "";
            UpdateActiveState();
            ApplyVisualImmediate(isActive);
        }

        private void Update()
        {
            string newChosen = statue != null ? statue.ChoosenLevel : "";
            if (newChosen != currentChosen)
            {
                currentChosen = newChosen;
                UpdateActiveState();
                ApplyVisualImmediate(isActive);
            }
        }

        void UpdateActiveState()
        {
            isActive = !string.IsNullOrEmpty(currentChosen);
        }

        void ApplyVisualImmediate(bool active)
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
                if (stairsRenderer.material != null)
                {
                    stairsRenderer.material.color = active ? activeColor : inactiveColor;
                    if (stairsRenderer.material.HasProperty("_EmissionColor"))
                    {
                        if (active)
                        {
                            stairsRenderer.material.EnableKeyword("_EMISSION");
                            stairsRenderer.material.SetColor("_EmissionColor", activeColor * 1.2f);
                        }
                        else
                        {
                            stairsRenderer.material.SetColor("_EmissionColor", Color.black);
                            stairsRenderer.material.DisableKeyword("_EMISSION");
                        }
                    }
                }
            }
        }

        IEnumerator TransitionToLevel(string levelName)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;

            if (colliderRef != null) colliderRef.enabled = false;

            if (gateAnimator != null) gateAnimator.SetTrigger("Open");
            if (gateParticles != null) gateParticles.Play();
            if (beamParticles != null) beamParticles.Play();
            if (dustParticles != null) dustParticles.Play();
            if (gateAudio != null) gateAudio.Play();

            int prevPriority = -1;
            if (zoomCamera != null)
            {
                prevPriority = zoomCamera.Priority;
                zoomCamera.Priority = zoomPriority;
            }

            if (impulseSource != null) impulseSource.GenerateImpulse();

            float oldTimeScale = Time.timeScale;
            float oldFixedDelta = Time.fixedDeltaTime;
            Time.timeScale = 0.45f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            float t = 0f;
            Material matInstance = null;
            bool hasEmission = false;
            if (stairsRenderer != null)
            {
                matInstance = stairsRenderer.material;
                hasEmission = matInstance.HasProperty("_EmissionColor");
            }

            while (t < transitionDuration)
            {
                t += Time.unscaledDeltaTime;
                float norm = Mathf.Clamp01(t / transitionDuration);
                float ease = Mathf.SmoothStep(0f, 1f, norm);

                if (matInstance != null)
                {
                    matInstance.color = Color.Lerp(inactiveColor, activeColor, ease);
                    if (hasEmission)
                    {
                        matInstance.EnableKeyword("_EMISSION");
                        matInstance.SetColor("_EmissionColor", Color.Lerp(Color.black, activeColor * 2.0f, ease));
                    }
                }

                yield return null;
            }

            if (matInstance != null)
            {
                matInstance.color = activeColor;
                if (hasEmission) matInstance.SetColor("_EmissionColor", activeColor * 2.0f);
            }

            yield return new WaitForSecondsRealtime(0.25f);
            Time.timeScale = oldTimeScale;
            Time.fixedDeltaTime = oldFixedDelta;

            if (zoomCamera != null)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                zoomCamera.Priority = prevPriority;
            }

            yield return new WaitForSecondsRealtime(0.15f);

            if (!string.IsNullOrEmpty(levelName))
            {
                SceneManager.LoadScene(levelName);
            }
            else
            {
                Debug.LogWarning("Stairs: levelName empty on transition.");
            }

            isTransitioning = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (requireTrigger && !isActive)
            {
                Debug.Log("Stairs: stairs inactive - nothing happens");
                return;
            }

            PlayerBase player = other.GetComponent<PlayerBase>();
            if (player == null) return;

            if (string.IsNullOrEmpty(currentChosen))
            {
                Debug.LogWarning("Stairs: currentChosen empty when player entered the stairs.");
                return;
            }

            StartCoroutine(TransitionToLevel(currentChosen));
        }
    }
}
