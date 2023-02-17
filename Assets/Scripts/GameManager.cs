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

    public static void GotoNextWorld() {
        World newWorld = null;

        Vector3 playerPos = PlayerController.Instance.transform.position;
        Vector3 extents = PlayerController.col.bounds.extents;

        PlayerController.rb.velocity = Vector2.zero;

        for (int i = 0; i < 100; i++) {
            newWorld = WorldManager.CreateWorld();
            newWorld.transform.position = playerPos;

            if (!newWorld.IsInsideBlock(playerPos) && !newWorld.IsInsideBlock(playerPos + extents) && !newWorld.IsInsideBlock(playerPos - extents)) break;
            Destroy(newWorld.gameObject);
        }

        Destroy(WorldManager.ActiveWorld?.gameObject);
        WorldManager.ActiveWorld = newWorld;

        newWorld.CreateChunkInstant(new Vector2Int(0, 0), true);
        newWorld.CreateChunkInstant(new Vector2Int(0, -1), true);
        newWorld.CreateChunkInstant(new Vector2Int(-1, 0), true);
        newWorld.CreateChunkInstant(new Vector2Int(-1, -1), true);

        newWorld.StartGeneration();
    }

    void Start() {
        GotoNextWorld();
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            GotoNextWorld();
        }
    }
}
