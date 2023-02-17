using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = System.Random;

public class World : MonoBehaviour
{
    public Vector2Int ChunkSize { get; private set; }
    public float BlockThreshold { get; private set; }
    public float UnitsPerBlock { get; private set; }
    public float MapSize { get; private set; }
    public float InverseScale { get; private set; }
    public int Offset { get; private set; }
    public int Seed { get; private set; }

    List<Vector2Int> chunksToCreate = new List<Vector2Int>();
    HashSet<Vector2Int> generatedChunks = new HashSet<Vector2Int>();

    IEnumerator chunkCoro;

    public void Init(Vector2Int chunkSize, float blockThreshold, float mapSize, float scale, float blocksPerUnit, int seed, int offset) {
        ChunkSize = chunkSize;
        BlockThreshold = blockThreshold;
        MapSize = mapSize;
        InverseScale = 1 / scale;
        UnitsPerBlock = 1 / blocksPerUnit;
        Seed = seed;
        Offset = offset;
    }

    public void StartGeneration() {
        chunkCoro = CreateChunkCoro();
        StartCoroutine(chunkCoro);
    }

    public void StopGeneration() {
        StopCoroutine(chunkCoro);
    }

    void OnEnable() => StartGeneration();
    void OnDisable() => StopGeneration();

    public Vector2Int WorldPosToChunkPos(Vector3 pos) {
        return new Vector2Int(
            Mathf.FloorToInt(pos.x / (UnitsPerBlock * ChunkSize.x)),
            Mathf.FloorToInt(pos.y / (UnitsPerBlock * ChunkSize.y))
        );
    }

    public bool IsOutsideChunkDistance(Vector2Int pos) {
        foreach (Transform despawnTracker in WorldManager.DespawnTrackers) {
            Vector2Int chunkDisplacement = WorldPosToChunkPos(despawnTracker.position) - pos;

            if (chunkDisplacement.sqrMagnitude > WorldManager.DespawnDistance * WorldManager.DespawnDistance) {
                return true;
            }
        }

        return false;
    }

    public Vector2 WorldPosToLocalPos(Vector3 pos) {
        return pos / UnitsPerBlock;
    }

    public float GetWorldPerlinValue(Vector3 pos) {
        return GetLocalPerlinValue(WorldPosToLocalPos(pos));
    }

    public float GetLocalPerlinValue(Vector2 pos) => GetLocalPerlinValue(pos.x, pos.y);
    public float GetLocalPerlinValue(float x, float y) {
        float val = Mathf.Clamp01(Mathf.PerlinNoise((x + Offset) * InverseScale, (y + Offset) * InverseScale));
        val *= 1 - Mathf.InverseLerp(0, MapSize * MapSize, x * x + y * y);

        return val;
    }

    public bool IsInsideBlock(int x, int y) => IsInsideBlock(new Vector3(x, y, 0));
    public bool IsInsideBlock(Vector3 pos) {
        return GetWorldPerlinValue(pos) < BlockThreshold;
    }

    void OnDestroy() {
        StopGeneration();
        chunksToCreate.Clear();
    }

    public void ChunkDeleted(Vector2Int pos) {
        chunksToCreate.Remove(pos);
        generatedChunks.Remove(pos);
    }

    public void CreateChunks(int amount, Vector3 worldPos) => CreateChunks(amount, WorldPosToChunkPos(worldPos));
    public void CreateChunks(int amount, Vector2Int startPos) {
        bool negate = false, vertical = false;
        int step = 1, stepAmount = 0;

        int x = startPos.x;
        int y = startPos.y;

        CreateChunk(x, y);

        for (int i = 0; i < amount - 1; i++) {
            stepAmount += 1;

            if (vertical) {
                y += negate ? -1 : 1;
            } else {
                x += negate ? -1 : 1;
            }

            if (stepAmount == step) {
                vertical = !vertical;
                stepAmount = 0;

                if (!vertical) {
                    step += 1;
                    negate = !negate;
                }
            }

            CreateChunk(x, y);
        }
    }

    public void CreateChunk(int x, int y) => CreateChunk(new Vector2Int(x, y));
    public void CreateChunk(Vector2Int pos) {
        if (generatedChunks.Contains(pos) || chunksToCreate.Contains(pos)) { 
            return;
        }

        chunksToCreate.Add(pos);
    }

    IEnumerator CreateChunkCoro() {
        while (true) {
            if (chunksToCreate.Count == 0) {
                yield return null;
                continue;
            }

            Vector2Int pos = chunksToCreate[0];
            List<float> blocks = new List<float>();

            if (IsOutsideChunkDistance(pos)) {
                chunksToCreate.RemoveAt(0);
                continue;
            }

            // We want to generate an extra point on the right and bottom sides to prevent seams in the chunks
            for (int x = 0; x < ChunkSize.x + 1; x++) {
                for (int y = 0; y < ChunkSize.y + 1; y++) {
                    blocks.Add(GetLocalPerlinValue(pos.x * ChunkSize.x + x, pos.y * ChunkSize.y + y));
                }
            }

            GameObject newGO = new GameObject($"Chunk ({pos.x}, {pos.y})");
            newGO.transform.parent = transform;

            Chunk newChunk = newGO.AddComponent<Chunk>();
            newChunk.GetComponent<MeshRenderer>().sharedMaterial = WorldManager.WorldMat;
            newChunk.Init(this, pos, blocks);

            newChunk.GenerateMesh();

            chunksToCreate.RemoveAt(0);
            generatedChunks.Add(pos);
            yield return null;
        }
    }
}
