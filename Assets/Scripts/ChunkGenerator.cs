using System;
using System.Collections.Generic;
using UnityEngine;

using Random = System.Random;

public class ChunkGenerator : MonoBehaviour
{
    [SerializeField] Material worldMat;
    [SerializeField] Vector2Int chunkSize = new Vector2Int(32, 32);
    [SerializeField] Vector2Int worldSize = new Vector2Int(2, 2);
    [SerializeField] [Range(0f, 1f)] float blockThreshold = 0.5f;
    [SerializeField] [Range(1f, 500f)] float mapSize = 250;
    [SerializeField] [Range(1f, 20f)] float scale = 15;
    [SerializeField] float blocksPerUnit = 1;
    [SerializeField] bool useRandomSeed = true;
    [SerializeField] string m_Seed;

    int seed;
    int offset;

    void Start() {
        CreateChunks(worldSize.x, worldSize.y);
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            CreateChunks(worldSize.x, worldSize.y);
        }
    }

    void SetSeedAndOffset() {
        if (useRandomSeed) seed = Time.time.GetHashCode();
        else {
            if (int.TryParse(m_Seed, out int seedInt)) {
                seed = seedInt;
            } else {
                seed = m_Seed.GetHashCode();
            }
        }

        offset = GetRandomOffset(seed);
    }

    int GetRandomOffset(int seed) {
        return new Random(seed).Next(-500000, 500000);
    }

    void CreateChunks(int xAmount, int yAmount) {
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }

        SetSeedAndOffset();

        for (int x = -xAmount + Mathf.CeilToInt(xAmount / 2f); x < xAmount - Mathf.FloorToInt(xAmount / 2f); x++) {
            for (int y = -yAmount + Mathf.CeilToInt(yAmount / 2f); y < yAmount - Mathf.FloorToInt(yAmount / 2f); y++) {
                CreateChunk(new Vector2Int(x, y));
            }
        }
    }

    public Chunk CreateChunk(Vector2Int pos) {
        List<List<float>> blocks = new List<List<float>>();

        // We want to generate an extra point on the right and bottom sides to prevent seams in the chunks
        for (int x = 0; x < chunkSize.x + 1; x++) {
            List<float> column = new List<float>();

            for (int y = 0; y < chunkSize.y + 1; y++) {
                float noise = Mathf.Clamp01(Mathf.PerlinNoise((x + (pos.x * chunkSize.x) + offset) / scale, (y + (pos.y * chunkSize.y) + offset) / scale));
                noise *= 1 - Mathf.InverseLerp(0, mapSize, Mathf.Abs(pos.x * chunkSize.x + x) + Mathf.Abs(pos.y * chunkSize.y + y));
                
                column.Add(noise);
            }

            blocks.Add(column);
        }

        GameObject newGO = new GameObject($"Chunk ({pos.x}, {pos.y})");
        newGO.transform.parent = transform;

        Chunk newChunk = newGO.AddComponent<Chunk>();
        newChunk.Init(seed, blockThreshold, 1 / blocksPerUnit, pos, blocks);
        newChunk.GetComponent<MeshRenderer>().sharedMaterial = worldMat;

        newChunk.GenerateMesh();
        return newChunk;
    }
}
