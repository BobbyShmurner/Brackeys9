using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;

using Unity.Jobs;

using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class Chunk : MonoBehaviour {
    public bool HasGenerated { get; private set; } = false;
    public World World { get; private set; }
    public Vector2Int Pos { get; private set; }

    float[] blocks;

	new MeshRenderer renderer;
    IEnumerator generateCoro;
    MeshFilter filter;
    ChunkJob job;

    void Awake() {
        renderer = GetComponent<MeshRenderer>();
        filter = GetComponent<MeshFilter>();
    }

    public bool IsOutsideChunkDistance() => World.IsOutsideChunkDistance(Pos);

    void Update() {
        if (IsOutsideChunkDistance()) Destroy(gameObject);
    }

    public void Init(World world, Vector2Int pos, List<float> blocks) {
        World = world;
        Pos = pos;
        this.blocks = blocks.ToArray();

        transform.position = World.ChunkPosToWorldPos(pos);
    }

    public void GenerateMesh(bool instant) {
        NativeHashMap<float3, int> existingVerts = new NativeHashMap<float3, int>(128, Allocator.TempJob);
        NativeArray<float> blocksArray = new NativeArray<float>(blocks, Allocator.TempJob);

        NativeHashMap<float2, float2> connectedVerts = new NativeHashMap<float2, float2>(32, Allocator.Persistent);
        NativeList<float3> verts = new NativeList<float3>(Allocator.Persistent);
        NativeList<int> tris = new NativeList<int>(Allocator.Persistent);

        job = new ChunkJob() {
            connectedVerts = connectedVerts,
            existingVerts = existingVerts,
            blocks = blocksArray,
            verts = verts,
            tris = tris,

            blockThreshold = World.BlockThreshold,
            unitsPerBlock = World.UnitsPerBlock,
            chunkWidth = World.ChunkSize.x,
            chunkHeight = World.ChunkSize.y
        };

        JobHandle jobHandle = job.Schedule();
        if (instant) jobHandle.Complete();

        generateCoro = GenerateMeshCoro(jobHandle, instant);
        GameManager.StartCoroutine(generateCoro);
    }

    IEnumerator GenerateMeshCoro(JobHandle jobHandle, bool instant) {
        while (!jobHandle.IsCompleted) {
            yield return null;
        }

        jobHandle.Complete();

        job.existingVerts.Dispose();
		job.blocks.Dispose();

        // Mesh Stuff

        Mesh mesh = new Mesh();

        mesh.SetVertices<float3>(job.verts);
        mesh.SetIndices<int>(job.tris, MeshTopology.Triangles, 0);

        mesh.RecalculateNormals();
        mesh.name = $"Chunk ({Pos.x}, {Pos.y})";

        GetComponent<MeshFilter>().mesh = mesh;

        // Collider Stuff

        HashSet<float2> vertsChecked = new HashSet<float2>();
        NativeArray<float2> nativeVertKeys = job.connectedVerts.GetKeyArray(Allocator.Temp);

        float2[] vertKeys = new float2[nativeVertKeys.Length];
        nativeVertKeys.CopyTo(vertKeys);
        nativeVertKeys.Dispose();

        foreach (var keyVert in vertKeys) {
            if (vertsChecked.Contains(keyVert)) continue;
            vertsChecked.Add(keyVert);

            EdgeCollider2D col = gameObject.AddComponent<EdgeCollider2D>();
            List<Vector2> points = new List<Vector2>();
            float2 currentVert = keyVert;

            points.Add(currentVert);

            while (vertKeys.Contains(currentVert)) {
                currentVert = job.connectedVerts[currentVert];
                points.Add(currentVert);

                if (vertsChecked.Contains(currentVert)) break;
                vertsChecked.Add(currentVert);
            }
            
            col.SetPoints(points);
            if (!instant) yield return null;
        }

        HasGenerated = true;
        DisposeNativeContainers();
    }

    void DisposeNativeContainers() {
        if (job.connectedVerts.IsCreated) job.connectedVerts.Dispose();
        if (job.existingVerts.IsCreated) job.existingVerts.Dispose();
		if (job.blocks.IsCreated) job.blocks.Dispose();
        if (job.verts.IsCreated) job.verts.Dispose();
        if (job.tris.IsCreated) job.tris.Dispose();
    }

    public void OnDestroy() {
        DisposeNativeContainers();
        World.ChunkDeleted(Pos);
    }
}