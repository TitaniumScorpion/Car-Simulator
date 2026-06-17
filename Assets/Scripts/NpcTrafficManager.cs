using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NpcTrafficManager : MonoBehaviour
{
    [Header("NPC Settings")]
    public GameObject npcPrefab;
    public Transform[] spawnPoints;
    public int npcCount = 3;
    public float waypointRadius = 5f;
    public float destinationReachedThreshold = 2f;

    private readonly List<NavMeshAgent> npcAgents = new();

    private void Start()
    {
        if (npcPrefab == null) { Debug.LogError("NpcTrafficManager: npcPrefab is not assigned.", this); return; }
        if (spawnPoints == null || spawnPoints.Length == 0) { Debug.LogError("NpcTrafficManager: no spawn points assigned.", this); return; }
        SpawnNpcs();
    }

    private void Update()
    {
        foreach (NavMeshAgent agent in npcAgents)
        {
            if (!agent.pathPending && agent.hasPath && agent.remainingDistance < destinationReachedThreshold)
                AssignNewDestination(agent);
        }
    }

    private void SpawnNpcs()
    {
        int count = Mathf.Min(npcCount, spawnPoints.Length);
        if (count < npcCount)
            Debug.LogWarning($"NpcTrafficManager: npcCount ({npcCount}) exceeds spawnPoints ({spawnPoints.Length}). Only {count} NPCs will spawn.", this);

        for (int i = 0; i < count; i++)
        {
            GameObject npc = Instantiate(npcPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            if (agent == null) { Debug.LogError($"NpcTrafficManager: NPC prefab '{npc.name}' has no NavMeshAgent.", npc); Destroy(npc); continue; }
            npcAgents.Add(agent);
            AssignNewDestination(agent);
        }
    }

    public void SpawnNpc(Transform spawnPoint)
    {
        if (spawnPoint == null) { Debug.LogError("NpcTrafficManager: spawnPoint is null.", this); return; }
        GameObject npc = Instantiate(npcPrefab, spawnPoint.position, spawnPoint.rotation);
        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
        if (agent == null) { Debug.LogError($"NpcTrafficManager: NPC prefab '{npc.name}' has no NavMeshAgent.", npc); Destroy(npc); return; }
        npcAgents.Add(agent);
        AssignNewDestination(agent);
    }

    private void AssignNewDestination(NavMeshAgent agent)
    {
        agent.SetDestination(SampleRandomNavMeshPoint());
    }

    private Vector3 SampleRandomNavMeshPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 candidate = transform.position + Random.insideUnitSphere * 50f;
            candidate.y = 0f;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, waypointRadius, NavMesh.AllAreas))
                return hit.position;
        }
        Debug.LogWarning("NpcTrafficManager: no valid NavMesh point found after 10 attempts. Check waypointRadius and manager position.", this);
        return transform.position;
    }
}
