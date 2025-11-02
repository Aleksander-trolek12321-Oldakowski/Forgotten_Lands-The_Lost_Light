using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBase : MonoBehaviour
{
    [SerializeField] int MaxHp = 10;
    [SerializeField] int MaxMp = 5;
    [SerializeField] int Strengh = 1;
    [SerializeField] int Def = 1;

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
}
