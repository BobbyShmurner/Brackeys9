using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = System.Random;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }
    public static World ActiveWorld { get; private set; }
    
    public static List<Transform> DespawnTrackers { get; private set; } = new List<Transform>();

    [SerializeField] Material worldMat;
    [SerializeField] Vector2Int chunkSize = new Vector2Int(32, 32);
    [SerializeField] [Range(0f, 1f)] float blockThreshold = 0.5f;
    [SerializeField] [Range(1f, 1000f)] float mapSize = 250;
    [SerializeField] [Range(1f, 20f)] float scale = 15;
    [SerializeField] float blocksPerUnit = 1;
    [SerializeField] int despawnDistance = 5;
    [SerializeField] bool useRandomSeed = true;
    [SerializeField] string seed;


    public static Material WorldMat {
        get => Instance.worldMat;
    }

    public static int DespawnDistance {
        get => Instance.despawnDistance;
    }

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start() {
        CreateWorld();
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            Destroy(ActiveWorld.gameObject);
            CreateWorld();
        }
    }

    public static World CreateWorld(string seed) {
        Instance.seed = seed;
        return CreateWorld();
    }

    public static World CreateWorld() {
        Instance.GetSeedAndOffset(out int seed, out int offset);

        GameObject worldGO = new GameObject($"World ({seed})");
        worldGO.transform.parent = Instance.transform;

        World world = worldGO.AddComponent<World>();
        world.Init(Instance.chunkSize, Instance.blockThreshold, Instance.mapSize, Instance.scale, Instance.blocksPerUnit, seed, offset);

        ActiveWorld = world;
        return world;
    }

    void GetSeedAndOffset(out int seed, out int offset) {
        if (useRandomSeed) seed = (int)DateTime.Now.Ticks;
        else {
            if (int.TryParse(this.seed, out int seedInt)) {
                seed = seedInt;
            } else {
                seed = this.seed.GetHashCode();
            }
        }

        offset = GetRandomOffset(seed);
    }

    int GetRandomOffset(int seed) => new Random(seed).Next(-500000, 500000);
}
