using UnityEngine;

public class Steering : MonoBehaviour
{
    public Vector3 Seek(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
        return dir;
    }

    public Vector3 Flee(Vector3 target)
    {
        Vector3 dir = transform.position - target;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
        return dir;
    }

    public Vector3 Pursue(Transform target, float prediction = 0.0f)
    {
        Vector3 predicted = target.position + (target.forward * prediction);
        predicted.y = transform.position.y;
        return Seek(predicted);
    }
}
