using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class Chunk : MonoBehaviour {
    public int Seed { get; private set; }
    public float BlockThreshold { get; private set; }
    public float UnitsPerBlock { get; private set; }
    public Vector2Int Pos { get; private set; }
    public Vector2Int Size { get; private set; }
    public bool Lerp { get; private set; }

    List<List<float>> blocks;

	new MeshRenderer renderer;
    MeshFilter filter;

    void Awake() {
        renderer = GetComponent<MeshRenderer>();
        filter = GetComponent<MeshFilter>();
    }

    public void Init(int seed, float blockThreshold, float unitsPerBlock, bool lerp, Vector2Int pos, List<List<float>> blocks) {
        Pos = pos;
        Seed = seed;
        Lerp = lerp;
        this.blocks = blocks;
        UnitsPerBlock = unitsPerBlock;
        BlockThreshold = blockThreshold;

        Size = new Vector2Int(blocks.Count, blocks[0].Count);
    }

    public float GetBlock(Vector2Int pos) {
        return GetBlock(pos.x, pos.y);
    }

    public float GetBlock(int x, int y) {
        if (x < 0 || x >= Size.x || y < 0 || y >= Size.y) return 0;
        return blocks[x][y];
    }

    public bool IsBlock(Vector2Int pos) {
        return IsBlock(pos.x, pos.y);
    }

    public bool IsBlock(int x, int y) {
        return GetBlock(x, y) < BlockThreshold;
    }

    public Vector2 LocalPosToGlobalPos(float x, float y) {
        return new Vector2(x + Pos.x * Size.x * UnitsPerBlock, y + Pos.y * Size.y * UnitsPerBlock);
    }

	public void GenerateMesh() {
        MeshData meshData = new MeshData(this);

        for (int x = 0; x < Size.x - 1; x++) {
            for (int y = 0; y < Size.y - 1; y++) {
                int squareIndex = IsBlock(x, y) ? 1 : 0;
                squareIndex = (squareIndex << 1) | (IsBlock(x + 1, y) ? 1 : 0);
                squareIndex = (squareIndex << 1) | (IsBlock(x + 1, y + 1) ? 1 : 0);
                squareIndex = (squareIndex << 1) | (IsBlock(x, y + 1) ? 1 : 0);

                switch (squareIndex) {
                    case 0:
                        break;
                    case 1:
                        meshData.AddVertFixed(x, y + 1);
                        meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x + 1, y + 1));
                        meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x, y));
                        
                        break;
                    case 2:
                        meshData.AddVertFixed(x + 1, y + 1);
                        meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x + 1, y));
                        meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1));

                        break;
                    case 3:
                        meshData.AddVertFixed(x, y + 1);
                        meshData.AddVertFixed(x + 1, y + 1);
                        meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x + 1, y));

                        meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x + 1, y));
                        meshData.AddVertLerp(new Vector2Int(x, y + 1), new Vector2Int(x, y));
                        meshData.AddVertFixed(x, y + 1);

                        break;
                    case 4:
                        meshData.AddVertFixed(x + 1, y);
                        meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));
                        meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));

                        break;
                    case 5:
                        

                        break;
                    case 6:
                        meshData.AddVertFixed(x + 1, y + 1);
                        meshData.AddVertFixed(x + 1, y);
                        meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));

                        meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));
                        meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1));
                        meshData.AddVertFixed(x + 1, y + 1);

                        break;
                    case 7:
                        meshData.AddVertFixed(x, y + 1);
                        meshData.AddVertFixed(x + 1, y + 1);
                        meshData.AddVertFixed(x + 1, y);

                        meshData.AddVertFixed(x, y + 1);
                        meshData.AddVertFixed(x + 1, y);
                        meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));

                        meshData.AddVertFixed(x, y + 1);
                        meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x + 1, y));
                        meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x, y + 1));

                        break;
                    case 8:
                        meshData.AddVertFixed(x, y);
                        meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x, y + 1));
                        meshData.AddVertLerp(new Vector2Int(x, y), new Vector2Int(x + 1, y));

                        break;
                    case 9:
                        meshData.AddVertFixed(x, y);
                        meshData.AddVertFixed(x, y + 1);
                        meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));

                        meshData.AddVertFixed(x, y + 1);
                        meshData.AddVertLerp(new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1));
                        meshData.AddVertLerp(new Vector2Int(x + 1, y), new Vector2Int(x, y));

                        break;
                    case 15:
                        meshData.AddVertFixed(x, y);
                        meshData.AddVertFixed(x, y + 1);
                        meshData.AddVertFixed(x + 1, y);

                        meshData.AddVertFixed(x + 1, y);
                        meshData.AddVertFixed(x, y + 1);
                        meshData.AddVertFixed(x + 1, y + 1);

                        break;
                }
            }
        }

        filter.mesh = meshData.CreateMesh();
    }

    // void OnDrawGizmosSelected() {
    //     for (int x = 0; x < Size.x; x++) {
    //         for (int y = 0; y < Size.y; y++) {
    //             float block = IsBlock(x, y) ? GetBlock(x, y) : 1;
    //             Gizmos.color = new Color(block, block, block, 1);

    //             Vector2 globalPos = LocalPosToGlobalPos(x, y);
    //             Gizmos.DrawCube(new Vector3(globalPos.x, globalPos.y, 0), Vector3.one);
    //         }
    //     }
    // }
}

class MeshData {
    List<Vector3> verts = new List<Vector3>();
    List<int> tris = new List<int>();
    Chunk chunk; 

    public MeshData(Chunk chunk) {
        this.chunk = chunk;
    }

    public void AddVertFixed(int x, int y) {
        AddVertGlobal(chunk.LocalPosToGlobalPos(x, y));
    }

    public void AddVertLerp(Vector2Int point1, Vector2Int point2) {
        float x, y;

        if (chunk.Lerp) {
            float block1 = chunk.GetBlock(point1);
            float block2 = chunk.GetBlock(point2);

            float time = Mathf.InverseLerp(block1, block2, chunk.BlockThreshold);

            x = Mathf.Lerp(point1.x, point2.x, time);
            y = Mathf.Lerp(point1.y, point2.y, time);
        } else {
            x = Mathf.Lerp(point1.x, point2.x, 0.5f);
            y = Mathf.Lerp(point1.y, point2.y, 0.5f);
        }

        AddVertGlobal(chunk.LocalPosToGlobalPos(x, y));
    }

    void AddVertGlobal(Vector2 pos) {
        Vector3 vert = (Vector3)pos;
        int vertIndex = verts.IndexOf(vert);

        if (vertIndex == -1) {
            vertIndex = verts.Count;
            verts.Add(vert);
        }

        tris.Add(vertIndex);
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();

        mesh.RecalculateNormals();
        mesh.name = $"Chunk ({chunk.Pos.x}, {chunk.Pos.y})";

        return mesh;
    }
}