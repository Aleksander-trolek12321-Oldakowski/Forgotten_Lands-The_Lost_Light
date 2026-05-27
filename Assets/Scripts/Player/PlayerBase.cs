using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Item;
using potions;
using Inventory;
using SideQuests;
using GameSave;

namespace Player
{
    public class PlayerBase : MonoBehaviour
    {
        [Header("Base stats")]
        [SerializeField] float MaxHp = 10f;
        [SerializeField] float MaxMp = 5f;
        [SerializeField] float Strength = 1f;
        [SerializeField] float Def = 1f;
        public float DamageMultiplier = 1f;
        public float PercentDmgTaken = 1f;
        
        [Header("Runtime")]
        [SerializeField] float currentHp;
        [SerializeField] float currentMp;
        public float CurrentHp => currentHp;
        public float CurrentMp => currentMp;
        public float MaxHP => MaxHp;
        public float MaxMP => MaxMp;
        public float Damage => Strength;
        public float Defense => Def;
        public float HpPercent => MaxHp > 0 ? currentHp / MaxHp : 0f;
        public float HpRestorePercentage = 0.2f;
        public float MpRestorePercentage = 0.5f;
        public float cd = 3f;
        public int currentStack = 0;
        public int MaxStack = 10;

        [Header("Level System")]
        [SerializeField] int level = 1;
        [SerializeField] float currentExp = 0f;
        [SerializeField] float expToNextLevel = 100f;
        [SerializeField] int skillPoints = 0;
        public int Level => level;
        public float CurrentExp => currentExp;
        public float ExpToNextLevel => expToNextLevel;
        public int SkillPoints => skillPoints;

        [Header("Movement")]
        public Rigidbody rb;
        public Transform cam;
        public float speed = 3f;
        public float rotationSpeed = 10f;
        public float velocitySmoothTime = 0.12f;
        public float deadZone = 0.1f;

        [Header("Step Assist")]
        public bool enableStepAssist = true;
        public float stepCheckDistance = 0.35f;
        public float stepHeight = 0.4f;
        public float stepMinHeight = 0.08f;
        public float stepUpSpeed = 4.5f;
        public LayerMask stepMask = ~0;
        public float stepGroundProbeExtraHeight = 0.25f;

        float cachedHorizontal;
        float cachedVertical;
        Vector3 moveDir = Vector3.zero;
        Vector3 velocityRef = Vector3.zero;
        private bool IsDead = false;
        private bool IsTired = false;
        private bool CanUse = true;

        public GameObject player;
        public UnityEngine.UI.Image HpOrb;
        public UnityEngine.UI.Image MpOrb;
        public UnityEngine.UI.Image ExpBar;
        public TextMeshProUGUI levelText;

        [Header("Inventory")]
        public KeyCode inventoryKey = KeyCode.I;
        InventoryUIController inventoryUIController;

        [Header("Economy")]
        public float Money = 100f;

        [Header("Out Of Combat Regen")]
        [SerializeField] private float outOfCombatDelaySeconds = 20f;
        [SerializeField] private float outOfCombatRegenPercentPerSecond = 0.01f;

        private bool controlsEnabled = true;
        private bool zeroVelocityWhenControlsDisabled = true;
        private float lastCombatActivityTime = -999f;
        private Collider movementCollider;

        private void Awake()
        {
            Cursor.visible = false;
            if (rb == null) rb = GetComponent<Rigidbody>();
            movementCollider = GetComponent<Collider>();

            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            currentHp = MaxHp;
            currentMp = MaxMp;
            UpdateHpOrb();
            UpdateMpOrb();
            UpdateExpBar();
            UpdateLevelText();
        }

        private void OnValidate()
        {
            if (cam == null && Camera.main != null)
            {
                cam = Camera.main.transform;
            }
        }

        private void Update()
        {
            HandleSceneBasedRegeneration();

            if (!controlsEnabled)
            {
                if (rb != null && zeroVelocityWhenControlsDisabled)
                    rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
                return;
            }

            cachedHorizontal = Input.GetAxis("Horizontal");
            cachedVertical = Input.GetAxis("Vertical");

            if (cam != null)
            {
                Vector3 camForward = cam.forward;
                Vector3 camRight = cam.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                moveDir = (camForward * cachedVertical + camRight * cachedHorizontal).normalized;
            }
            else
            {
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                moveDir = (forward * cachedVertical + right * cachedHorizontal).normalized;
            }

            if (Input.GetKeyDown(inventoryKey))
            {
                if (inventoryUIController != null)
                {
                    inventoryUIController.ToggleInventory();
                    Debug.Log("PlayerBase: Inventory toggle key pressed.");
                }
                else
                {
                    Debug.LogWarning("PlayerBase: InventoryUIController is null - cannot toggle inventory.");
                }
            }
        }

        private void FixedUpdate()
        {
            if (!controlsEnabled)
            {
                if (rb != null && zeroVelocityWhenControlsDisabled)
                    rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
                return;
            }

            if (rb == null) return;

            Vector3 desiredVel = moveDir * speed;
            Vector3 currentVelXZ = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            Vector3 smoothedVelXZ = Vector3.SmoothDamp(currentVelXZ, new Vector3(desiredVel.x, 0f, desiredVel.z), ref velocityRef, velocitySmoothTime);

            rb.velocity = new Vector3(smoothedVelXZ.x, rb.velocity.y, smoothedVelXZ.z);
            TryStepAssist(desiredVel);

            if (moveDir.magnitude >= deadZone)
            {
                Vector3 lookDir = new Vector3(moveDir.x, 0f, moveDir.z);
                Quaternion targetRot = Quaternion.LookRotation(lookDir);

                Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);

                if (rb.constraints == 0)
                {
                    rb.MoveRotation(newRot);
                }
            }
        }

        public void PickupItem(ItemData itemData)
        {
            if (itemData == null) return;

            MaxHp += itemData.HP;
            MaxMp += itemData.Mana;

            Strength += Mathf.RoundToInt(itemData.Damage);
            Def += Mathf.RoundToInt(itemData.Defense);

            speed += itemData.Speed;

            currentHp += itemData.HP;
            currentMp += itemData.Mana;

            currentHp = Mathf.Min(currentHp, MaxHp);
            currentMp = Mathf.Min(currentMp, MaxMp);

            Debug.Log($"PlayerBase: Picked up item: +HP {itemData.HP} +MP {itemData.Mana} +STR {itemData.Damage} +DEF {itemData.Defense} +SPD {itemData.Speed}");
        }

        public void ModifyStats(float hpDelta, float manaDelta, float damageDelta, float defDelta, float speedDelta)
        {
            MaxHp += hpDelta;
            MaxMp += manaDelta;
            Strength += Mathf.RoundToInt(damageDelta);
            Def += Mathf.RoundToInt(defDelta);
            speed += speedDelta;

            currentHp = Mathf.Min(currentHp + hpDelta, MaxHp);
            currentMp = Mathf.Min(currentMp + manaDelta, MaxMp);

            Debug.Log($"PlayerBase: stats modified HP:{hpDelta} MP:{manaDelta} DMG:{damageDelta} DEF:{defDelta} SPD:{speedDelta}");
        }

        public void SetControlsEnabled(bool enabled, bool stopHorizontalMovement = true)
        {
            controlsEnabled = enabled;
            zeroVelocityWhenControlsDisabled = stopHorizontalMovement;

            if (!enabled)
            {
                if (rb != null)
                {
                    if (stopHorizontalMovement)
                    {
                        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
                    }
                    cachedHorizontal = 0f;
                    cachedVertical = 0f;
                    moveDir = Vector3.zero;
                    velocityRef = Vector3.zero;
                }
            }
        }

        public void AddSkillPoints(int amount)
        {
            if (amount <= 0) return;
            skillPoints += amount;
        }

        public bool TryConsumeSkillPoint()
        {
            if (skillPoints <= 0)
                return false;

            skillPoints--;
            return true;
        }

        public void TakeDMG(float damage)
        {
            if (IsDead) return;
            RegisterCombatActivity();

            currentHp -= damage;
            currentHp = math.clamp(currentHp, 0, MaxHp);
            UpdateHpOrb();

            if (currentHp <= 0 && !IsDead)
            {
                Die();
            }
        }

        public void UseMP(float MPUsed)
        {
            currentMp -= MPUsed;
            currentMp = math.clamp(currentMp, 0, MaxMp);
            UpdateMpOrb();

            if (currentHp <= 0 && !IsDead)
            {
                Exhaust();
            }
        }
        private void UpdateHpOrb()
        {
            if (HpOrb != null)
            {
                HpOrb.fillAmount = currentHp / MaxHp;
            }
        }

        private void UpdateMpOrb()
        {
            if (MpOrb != null)
            {
                MpOrb.fillAmount = currentMp / MaxMp;
            }
        }

        private void UpdateExpBar()
        {
            if (ExpBar != null)
            {
                ExpBar.fillAmount = currentExp / expToNextLevel;
            }
        }

        private void UpdateLevelText()
        {
            if (levelText != null)
            {
                levelText.text = $"LEVEL {level}";
            }
        }

        public bool IsFullHp()
        {
            return currentHp >= MaxHp;
        }

        private bool IsFullMp()
        {
            return currentMp >= MaxMp;
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHp += amount;
            currentHp = math.clamp(currentHp, 0, MaxHp);
            UpdateHpOrb();
        }

        public void Restore(float amount)
        {
            if (IsTired) return;

            currentMp += amount;
            currentMp = math.clamp(currentMp, 0, MaxMp);
            UpdateMpOrb();
        }

        private void Die()
        {
            IsDead = true;
            SideQuestManager.Instance?.ResetActiveTimedQuestTimer();
            SaveService.DeleteSave();
            SceneManager.LoadScene("DEATH");
        }

        private void Exhaust()
        {
            IsTired = true;
        }

        public void UseHpPotion()
        {
            if (!CanUse)
                return;

            if (IsFullHp())
            {
                return;
            }

            float HealAmount = MaxHp * HpRestorePercentage;
            Heal(HealAmount);

            currentStack--;

            if (currentStack <= 0)
            {
                CanUse = false;
            }

            StartCoroutine(PotionCD());
        }
        
        public void UseMpPotion()
        {
            if (!CanUse) 
            return;    

            if (IsFullMp())
            {
                return;
            }

            float RestoreAmount = MaxMp * MpRestorePercentage;
            Restore(RestoreAmount);

            currentStack--;

            if (currentStack <= 0)
            {
                CanUse = false;
            }

            StartCoroutine(PotionCD());
        }

        private IEnumerator PotionCD()
        {
            CanUse = false;
            yield return new WaitForSeconds(cd);
            CanUse = true;
        }

        public bool AddToStack()
        {
            if (currentStack <= MaxStack)
            {
                currentStack++;
                return true;
            }
            return false; 
        }

        public void AddMoney(float amount)
        {
            Money += amount;
            Money = Mathf.Max(0f, Money);
            Debug.Log($"PlayerBase: AddMoney {amount:F1}. New balance: {Money:F1}");
        }

        public void AddExp(float amount)
        {
            if (amount <= 0f) return;

            currentExp += amount;

            while (currentExp >= expToNextLevel)
            {
                currentExp -= expToNextLevel;
                LevelUp();
            }

            UpdateExpBar();
        }

        private void LevelUp()
        {
            level++;

            // EXP grows x1.5
            expToNextLevel *= 1.5f;

            // Stats grow x1.3
            MaxHp *= 1.3f;
            MaxMp *= 1.3f;
            Strength *= 1.3f;
            Def *= 1.3f;

            currentHp = MaxHp;
            currentMp = MaxMp;

            UpdateHpOrb();
            UpdateMpOrb();

            // Skill point every 5 levels
            if (level % 5 == 0)
            {
                skillPoints++;
                Debug.Log("Skill Point Gained!");
            }

            SideQuestManager.Instance?.NotifyPlayerLevelUp();
            UpdateLevelText();

            Debug.Log($"LEVEL UP! Level: {level}");
        }

        public bool TrySpend(float amount)
        {
            if (amount <= 0f) return true;
            if (Money + 0.0001f >= amount)
            {
                Money -= amount;
                Debug.Log($"PlayerBase: Spent {amount:F1}. New balance: {Money:F1}");
                return true;
            }
            Debug.Log($"PlayerBase: Not enough money to spend {amount:F1}. Balance: {Money:F1}");
            return false;
        }

        public float GetMoney() => Money;

        public void RegisterCombatActivity()
        {
            lastCombatActivityTime = Time.time;
        }

        public SavedPlayerStats CreateSaveSnapshot()
        {
            return new SavedPlayerStats
            {
                maxHp = MaxHp,
                maxMp = MaxMp,
                strength = Strength,
                defense = Def,
                damageMultiplier = DamageMultiplier,
                percentDmgTaken = PercentDmgTaken,
                currentHp = currentHp,
                currentMp = currentMp,
                hpRestorePercentage = HpRestorePercentage,
                mpRestorePercentage = MpRestorePercentage,
                potionCooldown = cd,
                currentStack = currentStack,
                maxStack = MaxStack,
                level = level,
                currentExp = currentExp,
                expToNextLevel = expToNextLevel,
                skillPoints = skillPoints,
                money = Money
            };
        }

        public void ApplySaveSnapshot(SavedPlayerStats state)
        {
            if (state == null) return;

            MaxHp = Mathf.Max(1f, state.maxHp);
            MaxMp = Mathf.Max(0f, state.maxMp);
            Strength = state.strength;
            Def = state.defense;
            DamageMultiplier = state.damageMultiplier;
            PercentDmgTaken = state.percentDmgTaken;

            currentHp = Mathf.Clamp(state.currentHp, 0f, MaxHp);
            currentMp = Mathf.Clamp(state.currentMp, 0f, MaxMp);
            HpRestorePercentage = state.hpRestorePercentage;
            MpRestorePercentage = state.mpRestorePercentage;
            cd = Mathf.Max(0f, state.potionCooldown);
            currentStack = Mathf.Max(0, state.currentStack);
            MaxStack = Mathf.Max(0, state.maxStack);

            level = Mathf.Max(1, state.level);
            currentExp = Mathf.Max(0f, state.currentExp);
            expToNextLevel = Mathf.Max(1f, state.expToNextLevel);
            skillPoints = Mathf.Max(0, state.skillPoints);
            Money = Mathf.Max(0f, state.money);

            UpdateHpOrb();
            UpdateMpOrb();
            UpdateExpBar();
            UpdateLevelText();
        }

        private void HandleSceneBasedRegeneration()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            if (string.Equals(sceneName, SaveService.MenuSceneName, System.StringComparison.OrdinalIgnoreCase))
                return;

            if (string.Equals(sceneName, SaveService.HubSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                bool changed = false;

                if (currentHp < MaxHp)
                {
                    currentHp = MaxHp;
                    changed = true;
                }

                if (currentMp < MaxMp)
                {
                    currentMp = MaxMp;
                    changed = true;
                }

                if (changed)
                {
                    UpdateHpOrb();
                    UpdateMpOrb();
                }

                return;
            }

            if (Time.time < lastCombatActivityTime + outOfCombatDelaySeconds)
                return;

            float hpRegenPerSecond = MaxHp * outOfCombatRegenPercentPerSecond;
            float mpRegenPerSecond = MaxMp * outOfCombatRegenPercentPerSecond;
            float hpAmount = hpRegenPerSecond * Time.deltaTime;
            float mpAmount = mpRegenPerSecond * Time.deltaTime;

            bool hpChanged = false;
            bool mpChanged = false;

            if (currentHp < MaxHp && hpAmount > 0f)
            {
                currentHp = Mathf.Min(MaxHp, currentHp + hpAmount);
                hpChanged = true;
            }

            if (currentMp < MaxMp && mpAmount > 0f)
            {
                currentMp = Mathf.Min(MaxMp, currentMp + mpAmount);
                mpChanged = true;
            }

            if (hpChanged) UpdateHpOrb();
            if (mpChanged) UpdateMpOrb();
        }

        private void TryStepAssist(Vector3 desiredVelocity)
        {
            if (movementCollider == null)
                movementCollider = GetComponent<Collider>();

            if (!enableStepAssist || rb == null || movementCollider == null)
                return;

            Vector3 planarMove = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
            if (planarMove.sqrMagnitude < 0.0001f)
                return;

            if (rb.velocity.y > 1f)
                return;

            Vector3 moveDirNormalized = planarMove.normalized;
            Bounds bounds = movementCollider.bounds;
            float radiusFromBounds = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.8f;
            float probeRadius = Mathf.Clamp(radiusFromBounds, 0.05f, 0.5f);

            float bottomY = bounds.min.y + 0.02f;
            float lowProbeY = bottomY + 0.05f;
            float highProbeY = bottomY + Mathf.Max(0.1f, stepHeight);
            float checkDistance = Mathf.Max(0.05f, stepCheckDistance);

            Vector3 lowOrigin = new Vector3(bounds.center.x, lowProbeY, bounds.center.z);
            Vector3 highOrigin = new Vector3(bounds.center.x, highProbeY, bounds.center.z);

            if (!HasObstacleAtHeight(lowOrigin, moveDirNormalized, probeRadius, checkDistance))
                return;

            if (HasObstacleAtHeight(highOrigin, moveDirNormalized, probeRadius, checkDistance))
                return;

            Vector3 forwardEdge = lowOrigin + moveDirNormalized * (checkDistance + probeRadius + 0.02f);
            Vector3 groundProbeStart = new Vector3(forwardEdge.x, highProbeY + stepGroundProbeExtraHeight, forwardEdge.z);
            float groundProbeDistance = Mathf.Max(stepHeight + stepGroundProbeExtraHeight + 0.1f, 0.2f);

            if (!Physics.Raycast(groundProbeStart, Vector3.down, out RaycastHit groundHit, groundProbeDistance, stepMask, QueryTriggerInteraction.Ignore))
                return;

            if (groundHit.collider == null)
                return;

            if (groundHit.collider.transform == transform || groundHit.collider.transform.IsChildOf(transform))
                return;

            if (!TryGetGroundY(bounds.center, out float currentGroundY))
                return;

            float stepRise = groundHit.point.y - currentGroundY;
            if (stepRise < Mathf.Max(0.001f, stepMinHeight))
                return;

            if (stepRise > stepHeight + 0.05f)
                return;

            Vector3 stepOffset = Vector3.up * (stepUpSpeed * Time.fixedDeltaTime);
            rb.MovePosition(rb.position + stepOffset);
        }

        private bool HasObstacleAtHeight(Vector3 origin, Vector3 dir, float radius, float distance)
        {
            RaycastHit[] hits = Physics.SphereCastAll(origin, radius, dir, distance, stepMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i].collider;
                if (c == null)
                    continue;

                if (c.transform == transform || c.transform.IsChildOf(transform))
                    continue;

                return true;
            }

            return false;
        }

        private bool TryGetGroundY(Vector3 worldCenter, out float groundY)
        {
            groundY = 0f;
            float probeStartY = worldCenter.y + 0.25f;
            float probeDistance = Mathf.Max(stepHeight + 0.75f, 1.0f);
            Vector3 probeStart = new Vector3(worldCenter.x, probeStartY, worldCenter.z);

            RaycastHit[] hits = Physics.RaycastAll(probeStart, Vector3.down, probeDistance, stepMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return false;

            float bestDistance = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i].collider;
                if (c == null)
                    continue;

                if (c.transform == transform || c.transform.IsChildOf(transform))
                    continue;

                if (hits[i].distance < bestDistance)
                {
                    bestDistance = hits[i].distance;
                    groundY = hits[i].point.y;
                    found = true;
                }
            }

            return found;
        }
    }
}
