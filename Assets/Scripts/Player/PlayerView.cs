using UnityEngine;

public class PlayerView : MonoBehaviour
{
    Animator animator;
    Rigidbody rb;
    void Awake()
    {
        animator = GetComponent<Animator>();  
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        animator.SetFloat("Speed", rb.linearVelocity.magnitude);
    }
}
