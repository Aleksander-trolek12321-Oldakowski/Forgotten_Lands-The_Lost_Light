using UnityEngine;
using Player;

[RequireComponent(typeof(Collider))]
public class BossEncounterZone : MonoBehaviour
{
    [Header("Boss Spawn")]
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public string bossDisplayName = "Boss";

    [Header("Arena")]
    public GameObject arenaBlocker;
    public bool disableBlockerAfterBossDeath = true;

    [Header("Boss UI")]
    public BossHealthBarUI bossHealthBar;

    [Header("Portal")]
    public GameObject portalPrefab;
    public Transform portalSpawnPoint;
    public Transform encounterCenterPoint;
    public float portalHeightOffset = 0f;

    [Header("Enemy Gatekeeping")]
    public bool blockNonBossEnemiesFromZone = true;
    [Tooltip("How far outside the encounter boundary non-boss enemies should be moved.")]
    public float nonBossEjectOffset = 1.5f;

    [Header("Boss Music")]
    public AudioClip bossMusicClip;
    [Range(0f, 1f)] public float bossMusicVolume = 1f;
    public bool restoreSceneMusicOnBossDeath = true;

    public EnemyBase spawnedBoss;
    private bool encounterStarted;
    private bool encounterCompleted;
    private bool portalSpawned;
    private Collider zoneCollider;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Start()
    {
        zoneCollider = GetComponent<Collider>();
        if (arenaBlocker != null)
            arenaBlocker.SetActive(false);
    }

    private void OnDestroy()
    {
        if (spawnedBoss != null)
            spawnedBoss.Died -= OnBossDied;

        if (restoreSceneMusicOnBossDeath && encounterStarted && !encounterCompleted && SceneMusicManager.Instance != null)
            SceneMusicManager.Instance.StopBossMusic();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryEjectNonBoss(other);

        if (encounterStarted) return;

        PlayerBase player = other.GetComponent<PlayerBase>();
        if (player == null)
            player = other.GetComponentInParent<PlayerBase>();

        if (player == null) return;

        StartEncounter();
    }

    private void OnTriggerStay(Collider other)
    {
        TryEjectNonBoss(other);
    }

    private void StartEncounter()
    {
        encounterStarted = true;

        if (arenaBlocker != null)
            arenaBlocker.SetActive(true);

        if (bossPrefab == null)
        {
            Debug.LogWarning("BossEncounterZone: Missing bossPrefab.");
            return;
        }

        bossPrefab.SetActive(true);
        spawnedBoss.Died += OnBossDied;

        if (bossHealthBar != null)
            bossHealthBar.Show(spawnedBoss, bossDisplayName);

        if (bossMusicClip != null && SceneMusicManager.Instance != null)
            SceneMusicManager.Instance.PlayBossMusic(bossMusicClip, bossMusicVolume);
    }

    private void OnBossDied(EnemyBase enemy)
    {
        if (encounterCompleted) return;
        encounterCompleted = true;

        if (spawnedBoss != null)
            spawnedBoss.Died -= OnBossDied;

        if (bossHealthBar != null)
            bossHealthBar.Hide();

        if (disableBlockerAfterBossDeath && arenaBlocker != null)
            arenaBlocker.SetActive(false);

        if (restoreSceneMusicOnBossDeath && SceneMusicManager.Instance != null)
            SceneMusicManager.Instance.StopBossMusic();

        SpawnPortal();
        portalPrefab.SetActive(true);
    }

    private void SpawnPortal()
    {
        if (portalSpawned) return;
        portalSpawned = true;

        if (portalPrefab == null)
        {
            Debug.LogWarning("BossEncounterZone: Missing portalPrefab.");
            return;
        }

        Transform basePoint = portalSpawnPoint != null
            ? portalSpawnPoint
            : (encounterCenterPoint != null ? encounterCenterPoint : transform);

        Vector3 spawnPos = basePoint.position + Vector3.up * portalHeightOffset;
        Instantiate(portalPrefab, spawnPos, Quaternion.identity);
    }

    private void TryEjectNonBoss(Collider other)
    {
        if (!blockNonBossEnemiesFromZone || other == null)
            return;

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy == null || enemy.isBoss || enemy.IsDead)
            return;

        Vector3 center = encounterCenterPoint != null ? encounterCenterPoint.position : transform.position;
        Vector3 enemyPos = enemy.transform.position;

        Vector3 dir = enemyPos - center;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;
        dir.Normalize();

        Vector3 boundaryPoint = center + dir;
        if (zoneCollider != null)
            boundaryPoint = zoneCollider.ClosestPoint(center + dir * 1000f);

        Vector3 ejectTarget = boundaryPoint + dir * Mathf.Max(0.1f, nonBossEjectOffset);
        enemy.ForceRelocate(ejectTarget, keepCurrentY: true);
    }
}
