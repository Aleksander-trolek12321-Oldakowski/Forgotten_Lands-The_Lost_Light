using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;
using Item;
using potions;
using Inventory;

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

        [Header("Inventory")]
        public KeyCode inventoryKey = KeyCode.I;
        InventoryUIController inventoryUIController;

        [Header("Economy")]
        public float Money = 100f;

        private bool controlsEnabled = true;

        private void Awake()
        {
            Cursor.visible = false;
            if (rb == null) rb = GetComponent<Rigidbody>();

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
            if (!controlsEnabled)
            {
                if (rb != null)
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

            //Dla testu
            if (Input.GetKeyDown(KeyCode.L))
            {
                AddExp(200);
            }
        }

        private void FixedUpdate()
        {
            if (!controlsEnabled)
            {
                if (rb != null)
                    rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
                return;
            }

            if (rb == null) return;

            Vector3 desiredVel = moveDir * speed;
            Vector3 currentVelXZ = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            Vector3 smoothedVelXZ = Vector3.SmoothDamp(currentVelXZ, new Vector3(desiredVel.x, 0f, desiredVel.z), ref velocityRef, velocitySmoothTime);

            rb.velocity = new Vector3(smoothedVelXZ.x, rb.velocity.y, smoothedVelXZ.z);

            if (moveDir.magnitude >= deadZone)
            {
                Vector3 lookDir = new Vector3(moveDir.x, 0f, moveDir.z);
                Quaternion targetRot = Quaternion.LookRotation(lookDir);

                Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);

                if ((rb.constraints & RigidbodyConstraints.FreezeRotationY) == 0)
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

        public void SetControlsEnabled(bool enabled)
        {
            controlsEnabled = enabled;

            if (!enabled)
            {
                if (rb != null)
                {
                    rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
                    cachedHorizontal = 0f;
                    cachedVertical = 0f;
                    moveDir = Vector3.zero;
                    velocityRef = Vector3.zero;
                }
            }
        }

        public void TakeDMG(float damage)
        {
            if (IsDead) return;

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
            if (amount <= 0) return;

            currentExp += amount;

            while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            LevelUp();
        }

        void LevelUp()
        {
            level++;

            // EXP rośnie x1.5
            expToNextLevel *= 1.5f;

            // Staty rosną x1.3
            MaxHp *= 1.3f;
            MaxMp *= 1.3f;
            Strength *= 1.3f;
            Def *= 1.3f;
            currentHp = MaxHp;
            currentMp = MaxMp;

            UpdateHpOrb();
            UpdateMpOrb();

            // Skill point co 5 leveli
            if (level % 5 == 0)
            {
                skillPoints++;
                Debug.Log("Skill Point Gained!");
            }

            Debug.Log($"LEVEL UP! Level: {level}");
        }
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
    }
}
