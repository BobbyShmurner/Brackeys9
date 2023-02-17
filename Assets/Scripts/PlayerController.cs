using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour {
    public static PlayerController Instance { get; private set; }

    public static Rigidbody2D rb { get; private set; }
    public static BoxCollider2D col { get; private set; }

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
    }
}
