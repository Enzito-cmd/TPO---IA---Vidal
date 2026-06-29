using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private Animator anim;
    private bool isDead = false;

    public PlayerController playerMovement;
    public GameObject restartPanel;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerController>();
    }

    public void TakeDamage()
    {
        if (isDead) return;
        isDead = true;

        if (playerMovement != null) playerMovement.CanMove = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        anim.SetTrigger("GetUp");
        yield return new WaitForSeconds(1.5f);
        anim.SetTrigger("Die");

        yield return new WaitForSeconds(.0f);
        restartPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}