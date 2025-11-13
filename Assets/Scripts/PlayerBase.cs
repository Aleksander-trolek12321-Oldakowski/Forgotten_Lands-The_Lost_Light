using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.Unity.VisualStudio.Editor;

public class PlayerBase : MonoBehaviour
{
    [SerializeField] float MaxHp = 10;
    [SerializeField] float CurrentHp;
    [SerializeField] float MaxMp = 5;
    [SerializeField] float CurrentMp;
    [SerializeField] float Strengh = 1;
    [SerializeField] float Def = 1;

    public float HpRestorePercentage = 0.2f;
    public float MpRestorePercentage = 0.5f;
    public float cd = 3f;
    public int currentStack = 0;
    public int MaxStack = 10;

    public GameObject player;
    public UnityEngine.UI.Image HpOrb;
    public UnityEngine.UI.Image MpOrb;
    public Rigidbody rb;
    public float speed = 3f;
    public Transform cam;
    private bool IsDead = false;
    private bool IsTired = false;
    private bool CanUse = true;

    private void Start()
    {
        CurrentHp = MaxHp;
        CurrentMp = MaxMp;
        UpdateHpOrb();
        UpdateMpOrb();
    }
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

    public void TakeDMG(float damage)
    {
        if (IsDead) return;

        CurrentHp -= damage;
        CurrentHp = math.clamp(CurrentHp, 0, MaxHp);
        UpdateHpOrb();

        if (CurrentHp <= 0 && !IsDead)
        {
            Die();
        }
    }

    public void UseMP(float MPUsed)
    {
        CurrentMp -= MPUsed;
        CurrentMp = math.clamp(CurrentMp, 0, MaxMp);
        UpdateMpOrb();

        if (CurrentHp <= 0 && !IsDead)
        {
            Exhaust();
        }
    }
    private void UpdateHpOrb()
    {
        if (HpOrb != null)
        {
            HpOrb.fillAmount = CurrentHp / MaxHp;
        }
    }

    private void UpdateMpOrb()
    {
        if (MpOrb != null)
        {
            MpOrb.fillAmount = CurrentMp / MaxMp;
        }
    }

    public bool IsFullHp()
    {
        return CurrentHp >= MaxHp;
    }

    private bool IsFullMp()
    {
        return CurrentMp >= MaxMp;
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        CurrentHp += amount;
        CurrentHp = math.clamp(CurrentHp, 0, MaxHp);
        UpdateHpOrb();
    }

    public void Restore(float amount)
    {
        if (IsTired) return;

        CurrentMp += amount;
        CurrentMp = math.clamp(CurrentMp, 0, MaxMp);
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
}
