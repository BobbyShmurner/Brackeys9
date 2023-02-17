using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    [field: SerializeField] public int Radius { get; private set; } = 5;

    void Start() {
        WorldManager.DespawnTrackers.Add(transform);
    }

    void Update() {
        WorldManager.ActiveWorld?.CreateChunks(Radius * Radius, transform.position);
    }
}
