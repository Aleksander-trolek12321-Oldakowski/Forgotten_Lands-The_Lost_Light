using System.Collections;
using UnityEngine;
using Player;

public class AttackController : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackCd = 0.6f;
    public float attackSpeedMultiplier = 2f;

    [Header("Hitbox Timing")]
    public float hitboxStart = 0.1f;
    public float hitboxEnd = 0.4f;

    [Header("References")]
    public AttackHitbox hitbox;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] attackClips;
    [Range(0f, 1f)] public float attackVolume = 0.8f;

    private bool canAttack = true;
    private Animator anim;
    private float defaultAnimatorSpeed = 1f;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
            defaultAnimatorSpeed = anim.speed;

    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1") && canAttack)
        {
            Attack();
        }
    }

    void Attack()
    {
        if (anim == null)
            return;

        canAttack = false;
        ApplyAttackAnimationSpeed(true);

        int attackIndex = Random.Range(0, 3);
        anim.SetInteger("AttackIndex", attackIndex);
        anim.SetTrigger("Attack");
        PlayAttackAudio();

        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        float safeMultiplier = Mathf.Max(0.01f, attackSpeedMultiplier);
        yield return new WaitForSeconds(hitboxStart / safeMultiplier);

        if (hitbox != null)
            hitbox.EnableHitbox();

        yield return new WaitForSeconds(Mathf.Max(0f, hitboxEnd - hitboxStart) / safeMultiplier);

        if (hitbox != null)
            hitbox.DisableHitbox();

        yield return new WaitForSeconds(attackCd / safeMultiplier);

        ApplyAttackAnimationSpeed(false);

        canAttack = true;
    }

    void OnDisable()
    {
        ApplyAttackAnimationSpeed(false);
        canAttack = true;
    }

    private void ApplyAttackAnimationSpeed(bool enabled)
    {
        if (anim == null)
            return;

        anim.speed = enabled ? defaultAnimatorSpeed * Mathf.Max(0.01f, attackSpeedMultiplier) : defaultAnimatorSpeed;
    }

    private void ResolveAudioSource()
    {
        if (audioSource == null)
        {
            PlayerBase player = GetComponentInParent<PlayerBase>();
            if (player != null)
                audioSource = player.audioSource;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void PlayAttackAudio()
    {
        if (attackClips == null || attackClips.Length == 0)
            return;

        ResolveAudioSource();

        if (audioSource == null)
            return;

        AudioClip clip = attackClips[Random.Range(0, attackClips.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip, attackVolume);
    }
}
