using System;
using System.Collections.Generic;
using UnityEngine;

using Random = System.Random;

public class ChunkGenerator : MonoBehaviour
{
    [SerializeField] Vector2Int chunkSize = new Vector2Int(32, 32);
    [SerializeField] [Range(0f, 1f)] float blockThreshold = 0.5f;
    [SerializeField] float scale = 15;
    [SerializeField] float blocksPerUnit = 1;
    [SerializeField] bool lerp = true;
    [SerializeField] bool useRandomSeed = true;
    [SerializeField] string m_Seed;

    int seed;
    int offset;

    void Start() {
        CreateChunks();
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            CreateChunks();
        }
    }

    void SetSeedAndOffset() {
        if (useRandomSeed) seed = Time.time.GetHashCode();
        else seed = m_Seed.GetHashCode();

        offset = GetRandomOffset(seed);
    }

    int GetRandomOffset(int seed) {
        return new Random(seed).Next(-500000, 500000);
    }

    void CreateChunks() {
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        SetSeedAndOffset();

        for (int x = 0; x < 3; x++) {
            for (int y = 0; y < 3; y++) {
                Chunk chunk = CreateChunk(new Vector2Int(x, y));
                chunk.GenerateMesh();
            }
        }
    }

    public Chunk CreateChunk(Vector2Int pos) {
        List<List<float>> blocks = new List<List<float>>();

        for (int x = 0; x < chunkSize.x; x++) {
            List<float> column = new List<float>();

            for (int y = 0; y < chunkSize.y; y++) {
                column.Add(Mathf.Clamp01(Mathf.PerlinNoise((x + (pos.x * chunkSize.x) + offset) / scale, (y + (pos.y * chunkSize.y) + offset) / scale)));
            }

            blocks.Add(column);
        }

        GameObject newGO = new GameObject($"Chunk ({pos.x}, {pos.y})");
        newGO.transform.parent = transform;

        Chunk newChunk = newGO.AddComponent<Chunk>();
        newChunk.Init(seed, blockThreshold, 1 / blocksPerUnit, lerp, pos, blocks);

        return newChunk;
    }
}
