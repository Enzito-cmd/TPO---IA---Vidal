using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [SerializeField] private int speed = 5;
    [SerializeField] private int rotationSpeed = 10;

    [Header("Sprint & Stamina")]
    [SerializeField] private float sprintMultiplier = 1.7f;
    [SerializeField] private float maxStamina = 2.0f;
    [SerializeField] private float staminaRegenRate = 1.0f;

    private float currentStamina;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentStamina = maxStamina;
    }

    public void Walk(Vector3 dir)
    {
        if (Input.GetKey(KeyCode.LeftShift) && currentStamina > 0 && dir.magnitude > 0)
        {
            currentStamina -= Time.deltaTime;
            rb.linearVelocity = dir * (speed * sprintMultiplier);
        }
        else
        {
            rb.linearVelocity = dir * speed;

            if (currentStamina < maxStamina)
            {
                currentStamina += Time.deltaTime * staminaRegenRate;
            }
        }
    }

    public void Rotate(Vector3 dir)
    {
        if (dir == Vector3.zero) return;
        transform.forward = Vector3.Lerp(transform.forward, dir, rotationSpeed * Time.deltaTime);
    }
}