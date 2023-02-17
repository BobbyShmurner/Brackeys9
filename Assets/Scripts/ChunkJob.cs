using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;

using Unity.Jobs;
using Unity.Mathematics;

using UnityEngine;
using Unity.Collections;
using Unity.Burst;

enum SquarePoint {
	TopLeft,
	TopRight,
	BottomLeft,
	BottomRight,

	Top,
	Bottom,
	Left,
	Right,

	CenterFixed,
	CenterFixedTopLeft,
	CenterFixedTopRight,
	CenterFixedBottomLeft,
	CenterFixedBottomRight,

	CenterTopLeft,
	CenterTopRight,
	CenterBottomLeft,
	CenterBottomRight
}

[BurstCompile]
struct ChunkJob : IJob {
	[ReadOnly]
	public NativeArray<float> blocks;

    public NativeList<int> tris;
    public NativeList<float3> verts;
    public NativeHashMap<float3, int> existingVerts;

	public NativeHashMap<float2, float2> connectedVerts;

	public float blockThreshold;
	public float unitsPerBlock;

	public int chunkWidth;
	public int chunkHeight;

	public int2 IndexToCoords(int i) => new int2(Mathf.FloorToInt(i / (chunkHeight + 1)), i % (chunkHeight + 1));

	public int CoordsToIndex(int x, int y) => x * (chunkHeight + 1) + y;

	public float GetBlock(int2 pos) => GetBlock(pos.x, pos.y);
    public float GetBlock(int x, int y) {
        // We use > instead of >= because we generate extra points on the right and bottom sides
        if (x < 0 || x > chunkWidth || y < 0 || y > chunkHeight) return 0;
        return blocks[CoordsToIndex(x, y)];
    }

    public bool IsBlock(int2 pos) => IsBlock(pos.x, pos.y);
    public bool IsBlock(int x, int y) => GetBlock(x, y) < blockThreshold;

    public bool IsCenterBlock(int x, int y) => GetCenterAvg(x, y) < blockThreshold;

    public float GetCenterAvg(int x, int y) => (GetBlock(x, y) + GetBlock(x + 1, y) + GetBlock(x, y + 1) + GetBlock(x + 1, y + 1)) * 0.25f;

	public float2 BlockPosToLocalPos(float x, float y) =>new float2(x * unitsPerBlock, y * unitsPerBlock);

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
						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.Bottom, x, y);
						AddVert(SquarePoint.Left, x, y);

						AddOutline(SquarePoint.Bottom, SquarePoint.Left, x, y);
						
						break;
					case 2:
						AddVert(SquarePoint.BottomRight, x, y);
						AddVert(SquarePoint.Right, x, y);
						AddVert(SquarePoint.Bottom, x, y);

						AddOutline(SquarePoint.Right, SquarePoint.Bottom, x, y);

						break;
					case 3:
						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.BottomRight, x, y);
						AddVert(SquarePoint.Right, x, y);

						AddVert(SquarePoint.Right, x, y);
						AddVert(SquarePoint.Left, x, y);
						AddVert(SquarePoint.BottomLeft, x, y);

						AddOutline(SquarePoint.Right, SquarePoint.Left, x, y);

						break;
					case 4:
						AddVert(SquarePoint.TopRight, x, y);;
						AddVert(SquarePoint.Top, x, y);
						AddVert(SquarePoint.Right, x, y);

						AddOutline(SquarePoint.Top, SquarePoint.Right, x, y);

						break;
					case 5:
						AddVert(SquarePoint.TopRight, x, y);;
						AddVert(SquarePoint.Top, x, y);
						if (isCenterBlock) AddVert(SquarePoint.CenterFixed, x, y); else AddVert(SquarePoint.CenterBottomRight, x, y);

						AddVert(SquarePoint.TopRight, x, y);;
						if (isCenterBlock) AddVert(SquarePoint.CenterFixed, x, y); else AddVert(SquarePoint.CenterBottomRight, x, y);
						AddVert(SquarePoint.Right, x, y);

						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.Bottom, x, y);
						if (isCenterBlock) AddVert(SquarePoint.CenterFixed, x, y); else AddVert(SquarePoint.CenterBottomLeft, x, y);

						AddVert(SquarePoint.BottomLeft, x, y);
						if (isCenterBlock) AddVert(SquarePoint.CenterFixed, x, y); else AddVert(SquarePoint.CenterBottomLeft, x, y);
						AddVert(SquarePoint.Left, x, y);

						if (IsCenterBlock(x, y)) {
							AddVert(SquarePoint.Top, x, y);
							AddVert(SquarePoint.Left, x, y);
							AddVert(SquarePoint.CenterFixedTopLeft, x, y);

							AddVert(SquarePoint.Bottom, x, y);
							AddVert(SquarePoint.Right, x, y);
							AddVert(SquarePoint.CenterFixedBottomRight, x, y);

							AddOutline(SquarePoint.Top, SquarePoint.CenterFixedTopLeft, x, y);
							AddOutline(SquarePoint.CenterFixedTopLeft, SquarePoint.Left, x, y);

							AddOutline(SquarePoint.Bottom, SquarePoint.CenterFixedBottomRight, x, y);
							AddOutline(SquarePoint.CenterFixedBottomRight, SquarePoint.Right, x, y);
						} else {
							AddOutline(SquarePoint.Bottom, SquarePoint.CenterBottomLeft, x, y);
							AddOutline(SquarePoint.CenterBottomLeft, SquarePoint.Left, x, y);

							AddOutline(SquarePoint.Top, SquarePoint.CenterTopRight, x, y);
							AddOutline(SquarePoint.CenterTopRight, SquarePoint.Right, x, y);
						}

						break;
					case 6:
						AddVert(SquarePoint.BottomRight, x, y);
						AddVert(SquarePoint.TopRight, x, y);;
						AddVert(SquarePoint.Top, x, y);

						AddVert(SquarePoint.Top, x, y);
						AddVert(SquarePoint.Bottom, x, y);
						AddVert(SquarePoint.BottomRight, x, y);

						AddOutline(SquarePoint.Top, SquarePoint.Bottom, x, y);

						break;
					case 7:
						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.BottomRight, x, y);
						AddVert(SquarePoint.TopRight, x, y);;

						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.TopRight, x, y);;
						AddVert(SquarePoint.Top, x, y);

						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.Top, x, y);
						AddVert(SquarePoint.Left, x, y);

						AddOutline(SquarePoint.Top, SquarePoint.Left, x, y);

						break;
					case 8:
						AddVert(SquarePoint.TopLeft, x, y);
						AddVert(SquarePoint.Left, x, y);
						AddVert(SquarePoint.Top, x, y);

						AddOutline(SquarePoint.Left, SquarePoint.Top, x, y);

						break;
					case 9:
						AddVert(SquarePoint.TopLeft, x, y);
						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.Top, x, y);

						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.Bottom, x, y);
						AddVert(SquarePoint.Top, x, y);

						AddOutline(SquarePoint.Bottom, SquarePoint.Top, x, y);

						break;
					case 10:
						AddVert(SquarePoint.TopLeft, x, y);
						AddVert(SquarePoint.Left, x, y);
						if (isCenterBlock) AddVert(SquarePoint.CenterFixed, x, y); else AddVert(SquarePoint.CenterTopLeft, x, y);

						AddVert(SquarePoint.TopLeft, x, y);
						if (isCenterBlock) AddVert(SquarePoint.CenterFixed, x, y); else AddVert(SquarePoint.CenterTopLeft, x, y);
						AddVert(SquarePoint.Top, x, y);

						AddVert(SquarePoint.BottomRight, x, y);
						AddVert(SquarePoint.Right, x, y);
						if (isCenterBlock) AddVert(SquarePoint.CenterFixed, x, y); else AddVert(SquarePoint.CenterBottomRight, x, y);

						AddVert(SquarePoint.BottomRight, x, y);
						if (isCenterBlock) AddVert(SquarePoint.CenterFixed, x, y); else AddVert(SquarePoint.CenterBottomRight, x, y);
						AddVert(SquarePoint.Bottom, x, y);

						if (IsCenterBlock(x, y)) {
							AddVert(SquarePoint.Right, x, y);
							AddVert(SquarePoint.Top, x, y);
							AddVert(SquarePoint.CenterFixedTopRight, x, y);

							AddVert(SquarePoint.Left, x, y);
							AddVert(SquarePoint.Bottom, x, y);
							AddVert(SquarePoint.CenterFixedBottomLeft, x, y);

							AddOutline(SquarePoint.Right, SquarePoint.CenterFixedTopRight, x, y);
							AddOutline(SquarePoint.CenterFixedTopRight, SquarePoint.Top, x, y);

							AddOutline(SquarePoint.Left, SquarePoint.CenterFixedBottomLeft, x, y);
							AddOutline(SquarePoint.CenterFixedBottomLeft, SquarePoint.Bottom, x, y);
						} else {
							AddOutline(SquarePoint.Right, SquarePoint.CenterBottomRight, x, y);
							AddOutline(SquarePoint.CenterBottomRight, SquarePoint.Bottom, x, y);

							AddOutline(SquarePoint.Left, SquarePoint.CenterTopLeft, x, y);
							AddOutline(SquarePoint.CenterTopLeft, SquarePoint.Top, x, y);
						}

						break;
					case 11:
						AddVert(SquarePoint.TopLeft, x, y);
						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.BottomRight, x, y);

						AddVert(SquarePoint.TopLeft, x, y);
						AddVert(SquarePoint.BottomRight, x, y);
						AddVert(SquarePoint.Top, x, y);

						AddVert(SquarePoint.BottomRight, x, y);
						AddVert(SquarePoint.Right, x, y);
						AddVert(SquarePoint.Top, x, y);

						AddOutline(SquarePoint.Right, SquarePoint.Top, x, y);

						break;
					case 12:
						AddVert(SquarePoint.TopRight, x, y);
						AddVert(SquarePoint.TopLeft, x, y);
						AddVert(SquarePoint.Left, x, y);

						AddVert(SquarePoint.TopRight, x, y);
						AddVert(SquarePoint.Left, x, y);
						AddVert(SquarePoint.Right, x, y);

						AddOutline(SquarePoint.Left, SquarePoint.Right, x, y);

						break;
					case 13:
						AddVert(SquarePoint.TopLeft, x, y);
						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.TopRight, x, y);;

						AddVert(SquarePoint.TopRight, x, y);;
						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.Bottom, x, y);

						AddVert(SquarePoint.TopRight, x, y);;
						AddVert(SquarePoint.Bottom, x, y);
						AddVert(SquarePoint.Right, x, y);

						AddOutline(SquarePoint.Bottom, SquarePoint.Right, x, y);

						break;
					case 14:
						AddVert(SquarePoint.TopLeft, x, y);
						AddVert(SquarePoint.BottomRight, x, y);
						AddVert(SquarePoint.TopRight, x, y);;

						AddVert(SquarePoint.BottomRight, x, y);
						AddVert(SquarePoint.TopLeft, x, y);
						AddVert(SquarePoint.Left, x, y);

						AddVert(SquarePoint.BottomRight, x, y);
						AddVert(SquarePoint.Left, x, y);
						AddVert(SquarePoint.Bottom, x, y);

						AddOutline(SquarePoint.Left, SquarePoint.Bottom, x, y);

						break;
					case 15:
						AddVert(SquarePoint.TopLeft, x, y);
						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.TopRight, x, y);;

						AddVert(SquarePoint.TopRight, x, y);;
						AddVert(SquarePoint.BottomLeft, x, y);
						AddVert(SquarePoint.BottomRight, x, y);

						break;
				}
			}
		}
	}

	public void AddOutline(SquarePoint point1, SquarePoint point2, int x, int y) => AddOutline(GetPoint(point1, x, y), GetPoint(point2, x, y));
	public void AddOutline(float2 point1, float2 point2) => connectedVerts.Add(point1, point2);

	public float2 GetFixedPoint(int x, int y) => BlockPosToLocalPos(x, y);
	public float2 GetCenterFixed(int x, int y) => BlockPosToLocalPos(x + 0.5f, y + 0.5f);

	public float2 GetPoint(SquarePoint point, int x, int y) {
		switch (point) {
			case SquarePoint.TopLeft:
				return GetFixedPoint(x, y);
			case SquarePoint.TopRight:
				return GetFixedPoint(x + 1, y);
			case SquarePoint.BottomLeft:
				return GetFixedPoint(x, y + 1);
			case SquarePoint.BottomRight:
				return GetFixedPoint(x + 1, y + 1);
			
			case SquarePoint.Top:
				return GetPointLerp(new int2(x, y), new int2(x + 1, y));
			case SquarePoint.Bottom:
				return GetPointLerp(new int2(x, y + 1), new int2(x + 1, y + 1));
			case SquarePoint.Left:
				return GetPointLerp(new int2(x, y), new int2(x, y + 1));
			case SquarePoint.Right:
				return GetPointLerp(new int2(x + 1, y), new int2(x + 1, y + 1));

			case SquarePoint.CenterFixed:
				return GetCenterFixed(x, y);
			case SquarePoint.CenterFixedTopLeft:
				return GetCenterLerp(x, y, new int2(x, y), true);
			case SquarePoint.CenterFixedTopRight:
				return GetCenterLerp(x, y, new int2(x + 1, y), true);
			case SquarePoint.CenterFixedBottomLeft:
				return GetCenterLerp(x, y, new int2(x, y + 1), true);
			case SquarePoint.CenterFixedBottomRight:
				return GetCenterLerp(x, y, new int2(x + 1, y + 1), true);

			case SquarePoint.CenterTopLeft:
				return GetCenterLerp(x, y, new int2(x, y));
			case SquarePoint.CenterTopRight:
				return GetCenterLerp(x, y, new int2(x + 1, y));
			case SquarePoint.CenterBottomLeft:
				return GetCenterLerp(x, y, new int2(x, y + 1));
			case SquarePoint.CenterBottomRight:
				return GetCenterLerp(x, y, new int2(x + 1, y + 1));

			default:
				Debug.LogError($"Invalid Square Point \"{point}\"!");
				return float2.zero;
		}
	}

    public float2 GetPointLerp(int2 point1, int2 point2) {
        float block1 = GetBlock(point1);
        float block2 = GetBlock(point2);

        float time = Mathf.InverseLerp(block1, block2, blockThreshold);

        float lerpX = Mathf.Lerp(point1.x, point2.x, time);
        float lerpY = Mathf.Lerp(point1.y, point2.y, time);

        return BlockPosToLocalPos(lerpX, lerpY);
    }

    public float2 GetCenterLerp(int x, int y, int2 cornerPoint, bool isFixed = false) {
        float centerBlock = GetCenterAvg(x, y);
        float cornerBlock = GetBlock(cornerPoint);

        if (isFixed) cornerBlock -= 0.5f;

        float time = Mathf.InverseLerp(centerBlock, cornerBlock, blockThreshold);

        float lerpX = Mathf.Lerp(x + 0.5f, cornerPoint.x, time);
        float lerpY = Mathf.Lerp(y + 0.5f, cornerPoint.y, time);

        return BlockPosToLocalPos(lerpX, lerpY);
    }

	void AddVert(SquarePoint point, int x, int y) => AddVert(GetPoint(point, x, y));
    void AddVert(float2 pos) {
        float3 vert = new float3(pos.x, pos.y, 0);
		int vertIndex;

		if (existingVerts.ContainsKey(vert)) {
			vertIndex = existingVerts[vert];
		} else {
			vertIndex = verts.Length;
			verts.Add(vert);

			existingVerts.Add(vert, vertIndex);
		}

        tris.Add(vertIndex);
    }
}