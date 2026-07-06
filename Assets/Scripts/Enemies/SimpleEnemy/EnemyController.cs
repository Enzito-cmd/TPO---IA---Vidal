using UnityEngine;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
    public Transform player;
    public Pathfinding pathfinding;
    public Steering steering;
    public List<Transform> patrolPoints;

    public float walkSpeed = 2f;
    public float rotationSpeed = 10f;
    private Animator anim;
    public Vector3 spawnPosition;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (steering == null) steering = GetComponent<Steering>();
        if (pathfinding == null)
        {
            pathfinding = GetComponent<Pathfinding>();
        }
        spawnPosition = transform.position;
    }

    public void Move(Vector3 direction)
    {
        direction.y = 0;

        if (direction == Vector3.zero) return;

        transform.position += direction * walkSpeed * Time.deltaTime;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    public void UpdateAnimation(string param, bool value) => anim?.SetBool(param, value);
    public void UpdateTrigger(string param) => anim?.SetTrigger(param);
}
