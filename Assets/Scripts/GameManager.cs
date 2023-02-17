using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static new void StartCoroutine(IEnumerator routine) {
        (Instance as MonoBehaviour).StartCoroutine(routine);
    }

    public static void GotoNextWorld(bool instant = false) {
        StartCoroutine(GotoNextWorld_Coro(instant));
    }

    static IEnumerator GotoNextWorld_Coro(bool instant) {
        bool foundValid = false;

        Vector3 playerPos = PlayerController.Instance.transform.position;
        Vector3 extents = PlayerController.col.bounds.extents;
        int spawnRadius = PlayerController.Instance.GetComponent<ChunkGenerator>().Radius;

        World newWorld = WorldManager.CreateWorld();

        for (int i = 0; i < 1000; i++) {
            newWorld.transform.position = playerPos;

            for (float x = -2f; x <= 2f; x += 0.1f) {
                for (float y = -2f; y <= 2f; y += 0.1f) {
                    if (newWorld.IsInsideBlock(playerPos + new Vector3(x, y, 0))) {
                        goto InvalidWorld;
                    }
                }
            }

            foundValid = true;
            break;
            
            InvalidWorld:
                newWorld.SetNextOffset();
        }

        if (!foundValid) {
            Debug.LogError("Failed to generate a world!!!");
            yield break;
        }

        newWorld.gameObject.SetActive(false);
        newWorld.CreateChunks(spawnRadius * spawnRadius, Vector2Int.zero, instant);
        newWorld.StartGeneration();

        while (newWorld.IsWaitingForChunksToGenerate) {
            yield return null;
        }

        Destroy(WorldManager.ActiveWorld?.gameObject);
        WorldManager.ActiveWorld = newWorld;
        newWorld.gameObject.SetActive(true);
    }

    void Start() {
        GotoNextWorld(true);
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            GotoNextWorld();
        }
    }
}
