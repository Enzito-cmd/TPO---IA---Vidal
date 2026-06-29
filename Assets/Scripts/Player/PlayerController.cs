using UnityEngine;

public class PlayerController : MonoBehaviour
{
    PlayerModel model;
    private Transform camTransform;
    public bool CanMove { get; set; } = true;
    public GameObject winPanel;

    private void Awake()
    {
        model = GetComponent<PlayerModel>();
        if (Camera.main != null) camTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void Update()
    {
        if (CanMove)
        {
            float horizontal = Input.GetAxis("Horizontal"); 
            float vertical = Input.GetAxis("Vertical");    

            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;

            camForward.y = 0;
            camRight.y = 0;

            camForward = camForward.normalized;
            camRight = camRight.normalized;
            Vector3 dir = (camForward * vertical + camRight * horizontal).normalized;

            if (dir.magnitude >= 0.1f)
            {
                model.Walk(dir);
                model.Rotate(dir);
            }
            else
            {
                model.Walk(Vector3.zero);
            }
        }
        else
        {
            model.Walk(Vector3.zero);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Trophy"))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            winPanel.SetActive(true);
            Destroy(other.gameObject);
        }
    }
}
