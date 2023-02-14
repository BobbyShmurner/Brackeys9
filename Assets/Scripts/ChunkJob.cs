using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;

using Unity.Jobs;
using Unity.Mathematics;

using UnityEngine;
using Unity.Collections;
using Unity.Burst;

[BurstCompile]
struct ChunkJob : IJob {
	[ReadOnly]
	public NativeArray<float> blocks;

    public NativeList<int> tris;
    public NativeList<float3> verts;

	public float blockThreshold;
	public float unitsPerBlock;

	public int chunkWidth;
	public int chunkHeight;

	public int2 IndexToCoords(int i) {
		return new int2(Mathf.FloorToInt(i / (chunkHeight + 1)), i % (chunkHeight + 1));
	}

	public int CoordsToIndex(int x, int y) {
		return x * (chunkHeight + 1) + y;
	}

	public float GetBlock(int2 pos) {
        return GetBlock(pos.x, pos.y);
    }

    public float GetBlock(int x, int y) {
        // We use > instead of >= because we generate extra points on the right and bottom sides
        if (x < 0 || x > chunkWidth || y < 0 || y > chunkHeight) return 0;
        return blocks[CoordsToIndex(x, y)];
    }

    public bool IsBlock(int2 pos) {
        return IsBlock(pos.x, pos.y);
    }

    public bool IsBlock(int x, int y) {
        return GetBlock(x, y) < blockThreshold;
    }

    public bool IsCenterBlock(int x, int y) {
        return GetCenterAvg(x, y) < blockThreshold;
    }

    public float GetCenterAvg(int x, int y) {
        return (GetBlock(x, y) + GetBlock(x + 1, y) + GetBlock(x, y + 1) + GetBlock(x + 1, y + 1)) * 0.25f;
    }

	public float2 BlockPosToLocalPos(float x, float y) {
        return new float2(x * unitsPerBlock, y * unitsPerBlock);
    }

	public void Execute() {
		for (int x = 0; x < chunkWidth; x++) {
    		for (int y = 0; y < chunkHeight; y++) {
				int squareIndex = IsBlock(x, y) ? 1 : 0;
				squareIndex = (squareIndex << 1) | (IsBlock(x + 1, y) ? 1 : 0);
				squareIndex = (squareIndex << 1) | (IsBlock(x + 1, y + 1) ? 1 : 0);
				squareIndex = (squareIndex << 1) | (IsBlock(x, y + 1) ? 1 : 0);

				float center = GetCenterAvg(x, y);
				bool isCenterBlock = IsCenterBlock(x, y);

				switch (squareIndex) {
					case 0:
						break;
					case 1:
						AddVertFixed(x, y + 1);
						AddVertLerp(new int2(x, y + 1), new int2(x + 1, y + 1));
						AddVertLerp(new int2(x, y + 1), new int2(x, y));
						
						break;
					case 2:
						AddVertFixed(x + 1, y + 1);
						AddVertLerp(new int2(x + 1, y + 1), new int2(x + 1, y));
						AddVertLerp(new int2(x + 1, y + 1), new int2(x, y + 1));

						break;
					case 3:
						AddVertFixed(x, y + 1);
						AddVertFixed(x + 1, y + 1);
						AddVertLerp(new int2(x + 1, y + 1), new int2(x + 1, y));

						AddVertLerp(new int2(x + 1, y + 1), new int2(x + 1, y));
						AddVertLerp(new int2(x, y + 1), new int2(x, y));
						AddVertFixed(x, y + 1);

						break;
					case 4:
						AddVertFixed(x + 1, y);
						AddVertLerp(new int2(x + 1, y), new int2(x, y));
						AddVertLerp(new int2(x + 1, y), new int2(x + 1, y + 1));

						break;
					case 5:
						AddVertFixed(x + 1, y);
						AddVertLerp(new int2(x, y), new int2(x + 1, y));
						if (isCenterBlock) AddCenterFixed(x, y); else AddCenterLerp(x, y, new int2(x + 1, y + 1));

						AddVertFixed(x + 1, y);
						if (isCenterBlock) AddCenterFixed(x, y); else AddCenterLerp(x, y, new int2(x + 1, y + 1));
						AddVertLerp(new int2(x + 1, y), new int2(x + 1, y + 1));

						AddVertFixed(x, y + 1);
						AddVertLerp(new int2(x, y + 1), new int2(x + 1, y + 1));
						if (isCenterBlock) AddCenterFixed(x, y); else AddCenterLerp(x, y, new int2(x, y + 1));

						AddVertFixed(x, y + 1);
						if (isCenterBlock) AddCenterFixed(x, y); else AddCenterLerp(x, y, new int2(x, y + 1));
						AddVertLerp(new int2(x, y + 1), new int2(x, y));

						if (IsCenterBlock(x, y)) {
							AddVertLerp(new int2(x, y), new int2(x + 1, y));
							AddVertLerp(new int2(x, y), new int2(x, y + 1));
							AddCenterLerp(x, y, new int2(x, y), true);

							AddVertLerp(new int2(x, y + 1), new int2(x + 1, y + 1));
							AddVertLerp(new int2(x + 1, y), new int2(x + 1, y + 1));
							AddCenterLerp(x, y, new int2(x + 1, y + 1), true);
						}

						break;
					case 6:
						AddVertFixed(x + 1, y + 1);
						AddVertFixed(x + 1, y);
						AddVertLerp(new int2(x + 1, y), new int2(x, y));

						AddVertLerp(new int2(x + 1, y), new int2(x, y));
						AddVertLerp(new int2(x + 1, y + 1), new int2(x, y + 1));
						AddVertFixed(x + 1, y + 1);

						break;
					case 7:
						AddVertFixed(x, y + 1);
						AddVertFixed(x + 1, y + 1);
						AddVertFixed(x + 1, y);

						AddVertFixed(x, y + 1);
						AddVertFixed(x + 1, y);
						AddVertLerp(new int2(x + 1, y), new int2(x, y));

						AddVertFixed(x, y + 1);
						AddVertLerp(new int2(x, y), new int2(x + 1, y));
						AddVertLerp(new int2(x, y), new int2(x, y + 1));

						break;
					case 8:
						AddVertFixed(x, y);
						AddVertLerp(new int2(x, y), new int2(x, y + 1));
						AddVertLerp(new int2(x, y), new int2(x + 1, y));

						break;
					case 9:
						AddVertFixed(x, y);
						AddVertFixed(x, y + 1);
						AddVertLerp(new int2(x + 1, y), new int2(x, y));

						AddVertFixed(x, y + 1);
						AddVertLerp(new int2(x + 1, y + 1), new int2(x, y + 1));
						AddVertLerp(new int2(x + 1, y), new int2(x, y));

						break;
					case 10:
						AddVertFixed(x, y);
						AddVertLerp(new int2(x, y), new int2(x, y + 1));
						if (isCenterBlock) AddCenterFixed(x, y); else AddCenterLerp(x, y, new int2(x, y));

						AddVertFixed(x, y);
						if (isCenterBlock) AddCenterFixed(x, y); else AddCenterLerp(x, y, new int2(x, y));
						AddVertLerp(new int2(x, y), new int2(x + 1, y));

						AddVertFixed(x + 1, y + 1);
						AddVertLerp(new int2(x + 1, y), new int2(x + 1, y + 1));
						if (isCenterBlock) AddCenterFixed(x, y); else AddCenterLerp(x, y, new int2(x + 1, y + 1));

						AddVertFixed(x + 1, y + 1);
						if (isCenterBlock) AddCenterFixed(x, y); else AddCenterLerp(x, y, new int2(x + 1, y + 1));
						AddVertLerp(new int2(x, y + 1), new int2(x + 1, y + 1));

						if (IsCenterBlock(x, y)) {
							AddVertLerp(new int2(x + 1, y), new int2(x + 1, y + 1));
							AddVertLerp(new int2(x, y), new int2(x + 1, y));
							AddCenterLerp(x, y, new int2(x + 1, y), true);

							AddVertLerp(new int2(x, y), new int2(x, y + 1));
							AddVertLerp(new int2(x, y + 1), new int2(x + 1, y + 1));
							AddCenterLerp(x, y, new int2(x, y + 1), true);
						}

						break;
					case 11:
						AddVertFixed(x, y);
						AddVertFixed(x, y + 1);
						AddVertFixed(x + 1, y + 1);

						AddVertFixed(x, y);
						AddVertFixed(x + 1, y + 1);
						AddVertLerp(new int2(x, y), new int2(x + 1, y));

						AddVertFixed(x + 1, y + 1);
						AddVertLerp(new int2(x + 1, y), new int2(x + 1, y + 1));
						AddVertLerp(new int2(x + 1, y), new int2(x, y));

						break;
					case 12:
						AddVertFixed(x + 1, y);
						AddVertFixed(x, y);
						AddVertLerp(new int2(x, y), new int2(x, y + 1));

						AddVertFixed(x + 1, y);
						AddVertLerp(new int2(x, y), new int2(x, y + 1));
						AddVertLerp(new int2(x + 1, y), new int2(x + 1, y + 1));

						break;
					case 13:
						AddVertFixed(x, y);
						AddVertFixed(x, y + 1);
						AddVertFixed(x + 1, y);

						AddVertFixed(x + 1, y);
						AddVertFixed(x, y + 1);
						AddVertLerp(new int2(x, y + 1), new int2(x + 1, y + 1));

						AddVertFixed(x + 1, y);
						AddVertLerp(new int2(x, y + 1), new int2(x + 1, y + 1));
						AddVertLerp(new int2(x + 1, y), new int2(x + 1, y + 1));

						break;
					case 14:
						AddVertFixed(x, y);
						AddVertFixed(x + 1, y + 1);
						AddVertFixed(x + 1, y);

						AddVertFixed(x + 1, y + 1);
						AddVertFixed(x, y);
						AddVertLerp(new int2(x, y), new int2(x, y + 1));

						AddVertFixed(x + 1, y + 1);
						AddVertLerp(new int2(x, y), new int2(x, y + 1));
						AddVertLerp(new int2(x, y + 1), new int2(x + 1, y + 1));

						break;
					case 15:
						AddVertFixed(x, y);
						AddVertFixed(x, y + 1);
						AddVertFixed(x + 1, y);

						AddVertFixed(x + 1, y);
						AddVertFixed(x, y + 1);
						AddVertFixed(x + 1, y + 1);

						break;
				}
			}
		}
	}

	public void AddVertFixed(int x, int y) {
        AddVertGlobal(BlockPosToLocalPos(x, y));
    }

    public void AddVertLerp(int2 point1, int2 point2) {
        float block1 = GetBlock(point1);
        float block2 = GetBlock(point2);

        float time = Mathf.InverseLerp(block1, block2, blockThreshold);

        float lerpX = Mathf.Lerp(point1.x, point2.x, time);
        float lerpY = Mathf.Lerp(point1.y, point2.y, time);

        AddVertGlobal(BlockPosToLocalPos(lerpX, lerpY));
    }

    public void AddCenterFixed(int x, int y) {
        AddVertGlobal(BlockPosToLocalPos(x + 0.5f, y + 0.5f));
    }

    public void AddCenterLerp(int x, int y, int2 cornerPoint, bool isFixed = false) {
        float centerBlock = GetCenterAvg(x, y);
        float cornerBlock = GetBlock(cornerPoint);

        if (isFixed) cornerBlock -= 0.5f;

        float time = Mathf.InverseLerp(centerBlock, cornerBlock, blockThreshold);

        float lerpX = Mathf.Lerp(x + 0.5f, cornerPoint.x, time);
        float lerpY = Mathf.Lerp(y + 0.5f, cornerPoint.y, time);

        AddVertGlobal(BlockPosToLocalPos(lerpX, lerpY));
    }

    void AddVertGlobal(float2 pos) {
        float3 vert = new float3(pos.x, pos.y, 0);
        int vertIndex = verts.IndexOf(vert);

        if (vertIndex == -1) {
            vertIndex = verts.Length;
            verts.Add(vert);
        }

        tris.Add(vertIndex);
    }
}