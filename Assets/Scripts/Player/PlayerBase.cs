using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Item;

namespace Player
{
    public class PlayerBase : MonoBehaviour
    {
        [Header("Base stats")]
        [SerializeField] float MaxHp = 10;
        [SerializeField] float MaxMp = 5;
        [SerializeField] float Strength = 1;
        [SerializeField] float Def = 1;

        [Header("Runtime")]
        [SerializeField] float currentHp;
        [SerializeField] float currentMp;

        public GameObject player;
        public Rigidbody rb;
        public float speed = 3f;
        public Transform cam;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            if (horizontalInput == 0 && verticalInput == 0)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
                return;
            }

            //kamera
            Vector3 camForward = cam.forward;
            Vector3 camRight = cam.right;

            camForward.y = 0;
            camRight.y = 0;

            camForward.Normalize();
            camRight.Normalize();

            Vector3 ForwardRelative = horizontalInput * cam.forward;
            Vector3 RightRelative = verticalInput * cam.forward;

            Vector3 moveDir = (camForward * verticalInput + camRight * horizontalInput).normalized;

            Vector3 targetVelocity = moveDir * speed;
            rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);

            if (moveDir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
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
    }
}
