using UnityEngine;
using Player;

[RequireComponent(typeof(Collider))]
public class BossEncounterZone : MonoBehaviour
{
    [Header("Boss Spawn")]
    public EnemyBase bossPrefab;
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

    private EnemyBase spawnedBoss;
    private bool encounterStarted;
    private bool encounterCompleted;
    private bool portalSpawned;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Start()
    {
        if (arenaBlocker != null)
            arenaBlocker.SetActive(false);
    }

    private void OnDestroy()
    {
        if (spawnedBoss != null)
            spawnedBoss.Died -= OnBossDied;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (encounterStarted) return;

        PlayerBase player = other.GetComponent<PlayerBase>();
        if (player == null)
            player = other.GetComponentInParent<PlayerBase>();

        if (player == null) return;

        StartEncounter();
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

        Transform spawn = bossSpawnPoint != null ? bossSpawnPoint : transform;
        spawnedBoss = Instantiate(bossPrefab, spawn.position, spawn.rotation);
        spawnedBoss.Died += OnBossDied;

        if (bossHealthBar != null)
            bossHealthBar.Show(spawnedBoss, bossDisplayName);
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
}
