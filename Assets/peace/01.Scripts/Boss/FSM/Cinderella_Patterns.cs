using UnityEngine;
using System;

public class Cinderella_Patterns : MonoBehaviour, IInitializable, IBossLogics
{
    public int Priority => (int)InitOrder.Boss +1;

    [SerializeField] private Transform spawnPos;
    [SerializeField] private Transform playerPos;
    [SerializeField] private float move_idleSpeed;
    [SerializeField] private Vector3 move_idlePos;
    [SerializeField] private float move_enrangedSpeed;
    [SerializeField] private Vector3 move_enrangedPos;

    public float Move_idleSpeed => move_idleSpeed;
    public Vector3 Move_idlePos => move_idlePos;
    private Rigidbody rb;

    public void Init()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Spawn() //SpawningState
    {
        transform.position = spawnPos.position;
    }

    public void ExcuteIdleMove()
    {
        float distance = playerPos.position.x - transform.position.x;
        if (Math.Abs(distance) > move_idlePos.x+0.5)
            rb.linearVelocity = new Vector3(Math.Sign(distance) * move_idleSpeed, 0, 0);
        else if (Math.Abs(distance) < move_idlePos.x - 0.5)
            rb.linearVelocity = new Vector3(-Math.Sign(distance) * move_idleSpeed, 0, 0);
        else rb.linearVelocity = new Vector3(0, 0, 0);
    }
}
