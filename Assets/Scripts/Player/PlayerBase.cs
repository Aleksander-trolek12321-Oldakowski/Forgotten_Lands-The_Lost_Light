using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Item; // Upewnij się, że namespace Item/ item pasuje do Twojego ItemData

namespace Player
{
    public class PlayerBase : MonoBehaviour
    {
        [Header("Base stats")]
        [SerializeField] float MaxHp = 10f;
        [SerializeField] float MaxMp = 5f;
        [SerializeField] float Strength = 1f;
        [SerializeField] float Def = 1f;

        [Header("Runtime")]
        [SerializeField] float currentHp;
        [SerializeField] float currentMp;

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

            Strength += itemData.Damage;
            Def += itemData.Defense;

            speed += itemData.Speed;

            currentHp += itemData.HP;
            currentMp += itemData.Mana;

            currentHp = Mathf.Min(currentHp, MaxHp);
            currentMp = Mathf.Min(currentMp, MaxMp);

            Debug.Log($"Picked up item: +HP {itemData.HP} +MP {itemData.Mana} +STR {itemData.Damage} +DEF {itemData.Defense} +SPD {itemData.Speed}");
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

        public void ModifyStats(float hpDelta, float manaDelta, float damageDelta, float defDelta, float speedDelta)
        {
            MaxHp += hpDelta;
            MaxMp += manaDelta;
            Strength += Mathf.RoundToInt(damageDelta);
            Def += Mathf.RoundToInt(defDelta);
            speed += speedDelta;

            // adjust current values as well
            currentHp = Mathf.Min(currentHp + hpDelta, MaxHp);
            currentMp = Mathf.Min(currentMp + manaDelta, MaxMp);

            Debug.Log($"PlayerBase: stats modified HP:{hpDelta} MP:{manaDelta} DMG:{damageDelta} DEF:{defDelta} SPD:{speedDelta}");
        }
    }
}
