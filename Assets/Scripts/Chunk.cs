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
    public int Seed { get; private set; }
    public bool HasGenerated { get; private set; } = false;
    public float BlockThreshold { get; private set; }
    public float UnitsPerBlock { get; private set; }
    public Vector2Int Pos { get; private set; }
    public Vector2Int Size { get; private set; }

    bool shouldDelete = false;
    float[] blocks;

	new MeshRenderer renderer;
    MeshFilter filter;

    void Awake() {
        renderer = GetComponent<MeshRenderer>();
        filter = GetComponent<MeshFilter>();
    }

    public void Init(int seed, float blockThreshold, float unitsPerBlock, Vector2Int pos, Vector2Int size, List<float> blocks) {
        Pos = pos;
        Seed = seed;
        this.blocks = blocks.ToArray();
        UnitsPerBlock = unitsPerBlock;
        BlockThreshold = blockThreshold;
        Size = size;

        transform.position = new Vector3(Pos.x * Size.x * UnitsPerBlock, Pos.y * Size.y * UnitsPerBlock, 0);
    }

    public void GenerateMesh() {
        NativeArray<float> blocksArray = new NativeArray<float>(blocks, Allocator.TempJob);
        NativeList<float3> verts = new NativeList<float3>(Allocator.Persistent);
        NativeList<int> tris = new NativeList<int>(Allocator.Persistent);

        ChunkJob job = new ChunkJob() {
            blocks = blocksArray,
            verts = verts,
            tris = tris,

            blockThreshold = BlockThreshold,
            unitsPerBlock = UnitsPerBlock,
            chunkWidth = Size.x,
            chunkHeight = Size.y
        };

        JobHandle jobHandle = job.Schedule();

        StartCoroutine(GenerateMeshCoro(job, jobHandle));
    }

    IEnumerator GenerateMeshCoro(ChunkJob job, JobHandle jobHandle) {
        while (!jobHandle.IsCompleted) {
            yield return null;
        }

        jobHandle.Complete();

        Mesh mesh = new Mesh();

        mesh.SetVertices<float3>(job.verts);
        mesh.SetIndices<int>(job.tris, MeshTopology.Triangles, 0);

        mesh.RecalculateNormals();
        mesh.name = $"Chunk ({Pos.x}, {Pos.y})";

        filter.mesh = mesh;
        DisposeNativeContainers(job);

        HasGenerated = true;
        if (shouldDelete) {
            Destroy(gameObject);
        }
    }

    void DisposeNativeContainers(ChunkJob job) {
        job.blocks.Dispose();
        job.verts.Dispose();
        job.tris.Dispose();
    }

    public void Delete() {
        shouldDelete = true;
        if (HasGenerated) Destroy(gameObject);
    }

    // public float GetBlock(Vector2Int pos) {
    //     return GetBlock(pos.x, pos.y);
    // }

    // public float GetBlock(int x, int y) {
    //     // We use > instead of >= because we generate extra points on the right and bottom sides
    //     if (x < 0 || x > Size.x || y < 0 || y > Size.y) return 0;
    //     return blocks[x * (Size.y + 1) + y];
    // }

    // public bool IsBlock(Vector2Int pos) {
    //     return IsBlock(pos.x, pos.y);
    // }

    // public bool IsBlock(int x, int y) {
    //     return GetBlock(x, y) < BlockThreshold;
    // }

    // public bool IsCenterBlock(int x, int y) {
    //     return GetCenterAvg(x, y) < BlockThreshold;
    // }

    // public float GetCenterAvg(int x, int y) {
    //     return (GetBlock(x, y) + GetBlock(x + 1, y) + GetBlock(x, y + 1) + GetBlock(x + 1, y + 1)) * 0.25f;
    // }

    // public Vector2 BlockPosToLocalPos(float x, float y) {
    //     return new Vector2(x * UnitsPerBlock, y * UnitsPerBlock);
    // }

	// public void GenerateMesh() {
    //     MeshData meshData = new MeshData(this);

    //     for (int x = 0; x < Size.x; x++) {
    //         for (int y = 0; y < Size.y; y++) {
    //             int squareIndex = IsBlock(x, y) ? 1 : 0;
    //             squareIndex = (squareIndex << 1) | (IsBlock(x + 1, y) ? 1 : 0);
    //             squareIndex = (squareIndex << 1) | (IsBlock(x + 1, y + 1) ? 1 : 0);
    //             squareIndex = (squareIndex << 1) | (IsBlock(x, y + 1) ? 1 : 0);

    //             float center = GetCenterAvg(x, y);
    //             bool isCenterBlock = IsCenterBlock(x, y);

    //             switch (squareIndex) {
    //                 case 0:
    //                     break;
    //                 case 1:
    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x + 1, y + 1));
    //                     meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x, y));
                        
    //                     break;
    //                 case 2:
    //                     meshData.AddVertFixed(x + 1, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x + 1, y));
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1));

    //                     break;
    //                 case 3:
    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertFixed(x + 1, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x + 1, y));

    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x + 1, y));
    //                     meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x, y));
    //                     meshData.AddVertFixed(x, y + 1);

    //                     break;
    //                 case 4:
    //                     meshData.AddVertFixed(x + 1, y);
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));

    //                     break;
    //                 case 5:
    //                     meshData.AddVertFixed(x + 1, y);
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x + 1, y));
    //                     if (isCenterBlock) meshData.AddCenterFixed(x, y); else meshData.AddCenterLerp(x, y, new Vector2Int(x + 1, y + 1));

    //                     meshData.AddVertFixed(x + 1, y);
    //                     if (isCenterBlock) meshData.AddCenterFixed(x, y); else meshData.AddCenterLerp(x, y, new Vector2Int(x + 1, y + 1));
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));

    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x + 1, y + 1));
    //                     if (isCenterBlock) meshData.AddCenterFixed(x, y); else meshData.AddCenterLerp(x, y, new Vector2Int(x, y + 1));

    //                     meshData.AddVertFixed(x, y + 1);
    //                     if (isCenterBlock) meshData.AddCenterFixed(x, y); else meshData.AddCenterLerp(x, y, new Vector2Int(x, y + 1));
    //                     meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x, y));

    //                     if (IsCenterBlock(x, y)) {
    //                         meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x + 1, y));
    //                         meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x, y + 1));
    //                         meshData.AddCenterLerp(x, y, new Vector2Int(x, y), true);

    //                         meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x + 1, y + 1));
    //                         meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));
    //                         meshData.AddCenterLerp(x, y, new Vector2Int(x + 1, y + 1), true);
    //                     }

    //                     break;
    //                 case 6:
    //                     meshData.AddVertFixed(x + 1, y + 1);
    //                     meshData.AddVertFixed(x + 1, y);
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));

    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1));
    //                     meshData.AddVertFixed(x + 1, y + 1);

    //                     break;
    //                 case 7:
    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertFixed(x + 1, y + 1);
    //                     meshData.AddVertFixed(x + 1, y);

    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertFixed(x + 1, y);
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));

    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x + 1, y));
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x, y + 1));

    //                     break;
    //                 case 8:
    //                     meshData.AddVertFixed(x, y);
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x, y + 1));
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x + 1, y));

    //                     break;
    //                 case 9:
    //                     meshData.AddVertFixed(x, y);
    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));

    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1));
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));

    //                     break;
    //                 case 10:
    //                     meshData.AddVertFixed(x, y);
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x, y + 1));
    //                     if (isCenterBlock) meshData.AddCenterFixed(x, y); else meshData.AddCenterLerp(x, y, new Vector2Int(x, y));

    //                     meshData.AddVertFixed(x, y);
    //                     if (isCenterBlock) meshData.AddCenterFixed(x, y); else meshData.AddCenterLerp(x, y, new Vector2Int(x, y));
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x + 1, y));

    //                     meshData.AddVertFixed(x + 1, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));
    //                     if (isCenterBlock) meshData.AddCenterFixed(x, y); else meshData.AddCenterLerp(x, y, new Vector2Int(x + 1, y + 1));

    //                     meshData.AddVertFixed(x + 1, y + 1);
    //                     if (isCenterBlock) meshData.AddCenterFixed(x, y); else meshData.AddCenterLerp(x, y, new Vector2Int(x + 1, y + 1));
    //                     meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x + 1, y + 1));

    //                     if (IsCenterBlock(x, y)) {
    //                         meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));
    //                         meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x + 1, y));
    //                         meshData.AddCenterLerp(x, y, new Vector2Int(x + 1, y), true);

    //                         meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x, y + 1));
    //                         meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x + 1, y + 1));
    //                         meshData.AddCenterLerp(x, y, new Vector2Int(x, y + 1), true);
    //                     }

    //                     break;
    //                 case 11:
    //                     meshData.AddVertFixed(x, y);
    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertFixed(x + 1, y + 1);

    //                     meshData.AddVertFixed(x, y);
    //                     meshData.AddVertFixed(x + 1, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x + 1, y));

    //                     meshData.AddVertFixed(x + 1, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));

    //                     break;
    //                 case 12:
    //                     meshData.AddVertFixed(x + 1, y);
    //                     meshData.AddVertFixed(x, y);
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x, y + 1));

    //                     meshData.AddVertFixed(x + 1, y);
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x, y + 1));
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));

    //                     break;
    //                 case 13:
    //                     meshData.AddVertFixed(x, y);
    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertFixed(x + 1, y);

    //                     meshData.AddVertFixed(x + 1, y);
    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x + 1, y + 1));

    //                     meshData.AddVertFixed(x + 1, y);
    //                     meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x + 1, y + 1));
    //                     meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));

    //                     break;
    //                 case 14:
    //                     meshData.AddVertFixed(x, y);
    //                     meshData.AddVertFixed(x + 1, y + 1);
    //                     meshData.AddVertFixed(x + 1, y);

    //                     meshData.AddVertFixed(x + 1, y + 1);
    //                     meshData.AddVertFixed(x, y);
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x, y + 1));

    //                     meshData.AddVertFixed(x + 1, y + 1);
    //                     meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x, y + 1));
    //                     meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x + 1, y + 1));

    //                     break;
    //                 case 15:
    //                     meshData.AddVertFixed(x, y);
    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertFixed(x + 1, y);

    //                     meshData.AddVertFixed(x + 1, y);
    //                     meshData.AddVertFixed(x, y + 1);
    //                     meshData.AddVertFixed(x + 1, y + 1);

    //                     break;
    //             }
    //         }
    //     }

    //     filter.mesh = meshData.CreateMesh();
    // }

    // void OnDrawGizmos() {
    //     for (int x = 0; x < Size.x; x++) {
    //         for (int y = 0; y < Size.y; y++) {
    //             float block = IsBlock(x, y) ? GetBlock(x, y) : 1;
    //             Gizmos.color = new Color(block, block, block, 1);

    //             Vector2 globalPos = LocalPosToGlobalPos(x, y);
    //             Gizmos.DrawCube(new Vector3(globalPos.x, globalPos.y, 0), Vector3.one * 0.2f);
    //         }
    //     }
    // }
}

// class MeshData {
//     OrderedDictionary vertMap = new OrderedDictionary();
//     List<int> tris = new List<int>();
//     Chunk chunk; 

//     public MeshData(Chunk chunk) {
//         this.chunk = chunk;
//     }

//     public void AddVertFixed(int x, int y) {
//         AddVertGlobal(chunk.BlockPosToLocalPos(x, y));
//     }

//     public void AddVertLerp(Vector2Int point1, Vector2Int point2) {
//         float block1 = chunk.GetBlock(point1);
//         float block2 = chunk.GetBlock(point2);

//         float time = Mathf.InverseLerp(block1, block2, chunk.BlockThreshold);

//         float lerpX = Mathf.Lerp(point1.x, point2.x, time);
//         float lerpY = Mathf.Lerp(point1.y, point2.y, time);

//         AddVertGlobal(chunk.BlockPosToLocalPos(lerpX, lerpY));
//     }

//     public void AddCenterFixed(int x, int y) {
//         AddVertGlobal(chunk.BlockPosToLocalPos(x + 0.5f, y + 0.5f));
//     }

//     public void AddCenterLerp(int x, int y, Vector2Int cornerPoint, bool isFixed = false) {
//         float centerBlock = chunk.GetCenterAvg(x, y);
//         float cornerBlock = chunk.GetBlock(cornerPoint);

//         if (isFixed) cornerBlock -= 0.5f;

//         float time = Mathf.InverseLerp(centerBlock, cornerBlock, chunk.BlockThreshold);

//         float lerpX = Mathf.Lerp(x + 0.5f, cornerPoint.x, time);
//         float lerpY = Mathf.Lerp(y + 0.5f, cornerPoint.y, time);

//         AddVertGlobal(chunk.BlockPosToLocalPos(lerpX, lerpY));
//     }

//     void AddVertGlobal(Vector2 pos) {
//         Vector3 vert = (Vector3)pos;
//         int vertIndex = 0;

//         if (!vertMap.Contains(vert)) {
//             vertIndex = vertMap.Count;
//             vertMap[vert] = vertIndex;
//         }

//         vertIndex = (int)vertMap[vert];

//         tris.Add(vertIndex);
//     }

//     public Mesh CreateMesh() {
//         Mesh mesh = new Mesh();

//         Vector3[] verts = new Vector3[vertMap.Keys.Count];
//         vertMap.Keys.CopyTo(verts, 0);

//         mesh.vertices = verts;
//         mesh.triangles = tris.ToArray();

//         mesh.RecalculateNormals();
//         mesh.name = $"Chunk ({chunk.Pos.x}, {chunk.Pos.y})";

//         return mesh;
//     }
// }