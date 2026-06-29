using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    LineOfSight LOS;
    [SerializeField] GameObject player;
    [SerializeField] GameObject indicator;

  

    private void Awake()
    {
        LOS = new LineOfSight();
    }
    void Update()
    {
        if (LOS.CheckRange(transform, player.transform) && 
            LOS.CheckAngle(transform, player.transform) && 
            LOS.CheckObstacles(transform, player.transform))
        {
            indicator.SetActive(true);
        }
        else
        {
            indicator.SetActive(false);
        }
    }
}
