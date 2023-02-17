using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    [SerializeField] int radius = 5;

    void Start() {
        WorldManager.DespawnTrackers.Add(transform);
    }

    void Update() {
        WorldManager.ActiveWorld.CreateChunks(radius * radius, transform.position);
    }
}
