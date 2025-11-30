using UnityEngine;
using System.Collections;
using Player;

[System.Serializable]
public class BerserkSkill : Skill
{
    public float Duration = 25f;
    public float DmgBonus = 0.5f;            
    public float HpPerSecond = 0.01f; 

    private PlayerBase playerBase;
    private MonoBehaviour CoroutineRunner;
    private bool isActive = false;

    public void Init(PlayerBase playerBase, MonoBehaviour coroutineRunner)
    {
        this.playerBase = playerBase;
        this.CoroutineRunner = coroutineRunner;
    }

    public override void Activate()
    {
        if (!unlocked) return;
        if (playerBase == null || CoroutineRunner == null) return;
        if (isActive) return;

        CoroutineRunner.StartCoroutine(BerserkRoutine());
    }

    private IEnumerator BerserkRoutine()
    {
        isActive = true;

        playerBase.DamageMultiplier += DmgBonus;
        Debug.Log("Berserk");

        float elapsed = 0f;

        while (elapsed < Duration)
        {
            float hpLoss = playerBase.MaxHP * HpPerSecond;
            playerBase.TakeDamage(hpLoss);

            Debug.Log($"Berserk HP loss: {hpLoss}, HP = {playerBase.CurrentHp}/{playerBase.MaxHP}");

            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }

        playerBase.DamageMultiplier -= DmgBonus;
        Debug.Log("Berserk OFF");

        isActive = false;
    }
}
