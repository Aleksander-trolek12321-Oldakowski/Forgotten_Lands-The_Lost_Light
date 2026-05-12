using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Player;
using Cinemachine;
using GameSave;

namespace Objects
{
    [RequireComponent(typeof(Collider))]
    public class Stairs : MonoBehaviour
    {
        [Header("Reference to Statue (level selection)")]
        public Statue statue;

        [Header("Visuals")]
        public Color inactiveColor = Color.black;
        public Color activeColor = new Color(1f, 0.85f, 0.4f);

        [Header("Transition settings")]
        public bool requireTrigger = true;
        public float transitionDuration = 1.2f;

        [Header("Particles & VFX")]
        public ParticleSystem gateParticles;
        public ParticleSystem beamParticles;
        public ParticleSystem dustParticles;
        public AudioSource gateAudio;
        public Animator gateAnimator;
        public Renderer sigilRenderer;

        [Header("Cinemachine")]
        public CinemachineVirtualCamera zoomCamera;
        public int zoomPriority = 60;
        public CinemachineImpulseSource impulseSource;

        [Header("Gate sparks orientation (flip)")]
        public bool flipGateSparks = false;
        public Vector3 flipGateSparksEuler = new Vector3(0f, 180f, 0f);

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
                Debug.LogError("Stairs: No Collider attached.");
            }
            else if (requireTrigger)
            {
                colliderRef.isTrigger = true;
            }

            if (statue == null)
                statue = FindObjectOfType<Statue>();

            currentChosen = statue != null ? statue.ChoosenLevel : "";
            UpdateActiveState();

            if (flipGateSparks && gateParticles != null)
            {
                gateParticles.transform.localEulerAngles = gateParticles.transform.localEulerAngles + flipGateSparksEuler;
            }

            ApplyGateAndDustColor(inactiveColor);
            ApplySigilColor(inactiveColor);

            if (isActive)
            {
                if (beamParticles != null && !beamParticles.isPlaying) beamParticles.Play();
                if (dustParticles != null && !dustParticles.isPlaying) dustParticles.Play();
            }
        }

        private void Update()
        {
            string newChosen = statue != null ? statue.ChoosenLevel : "";
            if (newChosen != currentChosen)
            {
                currentChosen = newChosen;
                UpdateActiveState();

                if (isActive)
                {
                    ApplyGateAndDustColor(activeColor);
                    ApplySigilColor(activeColor);

                    if (beamParticles != null && !beamParticles.isPlaying && !beamParticles.main.playOnAwake) beamParticles.Play();
                    if (dustParticles != null && !dustParticles.isPlaying) dustParticles.Play();
                }
                else
                {
                    ApplyGateAndDustColor(inactiveColor);
                    ApplySigilColor(inactiveColor);

                    if (beamParticles != null && beamParticles.isPlaying && !beamParticles.main.playOnAwake) beamParticles.Stop();
                    if (dustParticles != null && dustParticles.isPlaying) dustParticles.Stop();
                }
            }
        }

        void UpdateActiveState()
        {
            isActive = !string.IsNullOrEmpty(currentChosen);
        }

        void SetParticleStartColor(ParticleSystem ps, Color color)
        {
            if (ps == null) return;
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null && rend.material != null)
            {
                Material mat = rend.material;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                else mat.color = color;
            }
        }

        void ApplyGateAndDustColor(Color color)
        {
            SetParticleStartColor(gateParticles, color);
            SetParticleStartColor(dustParticles, color);
        }

        void ApplySigilColor(Color color)
        {
            if (sigilRenderer == null) return;
            Material mat = sigilRenderer.material;
            if (mat == null) return;

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            else mat.color = color;

            if (mat.HasProperty("_EmissionColor"))
            {
                if (color == inactiveColor) mat.SetColor("_EmissionColor", Color.black);
                else mat.SetColor("_EmissionColor", color * 2.0f);
            }
        }

        IEnumerator TransitionToLevel(string levelName)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;

            if (colliderRef != null) colliderRef.enabled = false;

            if (gateAnimator != null) gateAnimator.SetTrigger("Open");
            if (gateParticles != null) gateParticles.Play(); // burst at trigger
            if (beamParticles != null && !beamParticles.isPlaying) beamParticles.Play();
            if (dustParticles != null && !dustParticles.isPlaying) dustParticles.Play();
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
            Material sigilMatInstance = null;
            bool sigilHasEmission = false;
            if (sigilRenderer != null)
            {
                sigilMatInstance = sigilRenderer.material;
                sigilHasEmission = sigilMatInstance.HasProperty("_EmissionColor");
            }

            while (t < transitionDuration)
            {
                t += Time.unscaledDeltaTime;
                float norm = Mathf.Clamp01(t / transitionDuration);
                float ease = Mathf.SmoothStep(0f, 1f, norm);

                if (sigilMatInstance != null)
                {
                    Color c = Color.Lerp(inactiveColor, activeColor, ease);
                    if (sigilMatInstance.HasProperty("_BaseColor")) sigilMatInstance.SetColor("_BaseColor", c);
                    else sigilMatInstance.color = c;

                    if (sigilHasEmission) sigilMatInstance.SetColor("_EmissionColor", Color.Lerp(Color.black, activeColor * 2.0f, ease));
                }

                Color pcol = Color.Lerp(inactiveColor, activeColor, ease);
                SetParticleStartColor(gateParticles, pcol);
                SetParticleStartColor(dustParticles, pcol);

                yield return null;
            }

            if (sigilMatInstance != null)
            {
                if (sigilMatInstance.HasProperty("_BaseColor")) sigilMatInstance.SetColor("_BaseColor", activeColor);
                else sigilMatInstance.color = activeColor;

                if (sigilHasEmission) sigilMatInstance.SetColor("_EmissionColor", activeColor * 2.0f);
            }

            SetParticleStartColor(gateParticles, activeColor);
            SetParticleStartColor(dustParticles, activeColor);

            yield return new WaitForSecondsRealtime(0.25f);
            Time.timeScale = oldTimeScale;
            Time.fixedDeltaTime = oldFixedDelta;

            if (zoomCamera != null)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                zoomCamera.Priority = prevPriority;
            }

            if (!string.IsNullOrEmpty(levelName))
                SceneManager.LoadScene(levelName);

            isTransitioning = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (requireTrigger && !isActive)
            {
                Debug.Log("Stairs: inactive - nothing happens");
                return;
            }

            PlayerBase player = other.GetComponent<PlayerBase>();
            if (player == null) player = other.GetComponentInParent<PlayerBase>();
            if (player == null) return;

            if (string.IsNullOrEmpty(currentChosen))
            {
                Debug.LogWarning("Stairs: currentChosen empty when player entered the stairs.");
                return;
            }

            bool inHub = string.Equals(SceneManager.GetActiveScene().name, SaveService.HubSceneName);
            SaveService.CaptureAndSave(
                targetSceneName: currentChosen,
                includeHubPosition: inHub,
                clearCurrentSceneChestState: false,
                clearHubPositionWhenNotIncluded: false
            );

            StartCoroutine(TransitionToLevel(currentChosen));
        }
    }
}
