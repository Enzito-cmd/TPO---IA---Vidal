using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyControllerTree : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Pathfinding pathfinding;
    public Steering steering;
    public LineOfSight los;
    private EnemyDecisionTree decisionTree;
    private Animator anim;

    [Header("AI Config")]
    public EnemyType AIType;
    public List<Transform> patrolPoints;
    public float walkSpeed = 2f;
    public float rotationSpeed = 10f;
    public Vector3 spawnPosition;

    private EnemyContext context;
    private List<Node> currentPath;
    private int pathIndex = 0;
    private int lastRandomIndex = -1;
    private bool isWaiting = false;
    private bool isAttacking = false;
    private float fleeTimer = 2.0f;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (steering == null) steering = GetComponent<Steering>();
        if (pathfinding == null) pathfinding = GetComponent<Pathfinding>();
        if (los == null) los = GetComponent<LineOfSight>();
        decisionTree = GetComponent<EnemyDecisionTree>();

        spawnPosition = transform.position;

        context = new EnemyContext { self = transform, player = player, los = los, aiType = AIType };
    }

    void Update()
    {
        if (player != null) context.player = player;
        context.aiType = AIType;

        if (decisionTree != null)
        {
            decisionTree.Evaluate(this, context);
        }
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



    public void DoIdleRotate()
    {
        UpdateAnimation("isRunning", false);
        UpdateAnimation("isWalking", false);
        transform.Rotate(0, 45f * Time.deltaTime, 0);
    }

    public void DoPursue()
    {
        UpdateAnimation("isWalking", false);
        UpdateAnimation("isRunning", true);
        currentPath = null; 

        Vector3 dir = steering.Pursue(player, 0.0f);
        Move(dir);
    }

    public void DoAttack()
    {
        UpdateAnimation("isRunning", false);
        UpdateAnimation("isWalking", false);

        if (!isAttacking)
        {
            StartCoroutine(AttackSeq());
        }
    }

    private IEnumerator AttackSeq()
    {
        isAttacking = true;
        if (player != null)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage();
        }

        UpdateTrigger("Aim");
        yield return new WaitForSeconds(2.5f);

        UpdateTrigger("Shoot");
        yield return new WaitForSeconds(2.5f);
        isAttacking = false;
    }

    public void DoFlee()
    {
        UpdateAnimation("isWalking", true);
        fleeTimer -= Time.deltaTime;

        Vector3 fleeDir = steering.Flee(player.position);
        RaycastHit hit;
        if (Physics.Raycast(transform.position, fleeDir, out hit, 1.0f))
        {
            fleeDir = Vector3.RotateTowards(fleeDir, transform.right, 5f * Time.deltaTime, 0f);
        }

        Move(fleeDir);

        if (fleeTimer <= 0)
        {
            gameObject.SetActive(false);
            PlayerHealth healthScript = player.GetComponent<PlayerHealth>();
            if (healthScript != null && healthScript.restartPanel != null)
            {
                healthScript.restartPanel.SetActive(true);
            }
        }
    }

    public void DoPatrol()
    {
        if (isWaiting || patrolPoints == null || patrolPoints.Count == 0) return;
        UpdateAnimation("isRunning", false);
        UpdateAnimation("isWalking", true);

        if (isWaiting || patrolPoints == null || patrolPoints.Count == 0) return;

        if (pathIndex >= patrolPoints.Count) pathIndex = 0;

        if (currentPath == null)
        {
            currentPath = pathfinding.FindPath(transform.position, patrolPoints[pathIndex].position);
            if (currentPath == null || currentPath.Count == 0)
            {
                pathIndex = (pathIndex + 1) % patrolPoints.Count;
                return;
            }
        }

        FollowPath(() => StartCoroutine(WaitAtPoint(() => pathIndex = (pathIndex + 1) % patrolPoints.Count)));
    }

    public void DoRandomPatrol()
    {
        if (isWaiting || patrolPoints == null || patrolPoints.Count == 0) return;
        UpdateAnimation("isRunning", false);
        UpdateAnimation("isWalking", true);

        if (isWaiting || patrolPoints == null || patrolPoints.Count == 0) return;

        if (currentPath == null)
        {
            int nextIndex;
            do { nextIndex = Random.Range(0, patrolPoints.Count); }
            while (patrolPoints.Count > 1 && nextIndex == lastRandomIndex);

            lastRandomIndex = nextIndex;
            currentPath = pathfinding.FindPath(transform.position, patrolPoints[nextIndex].position);
            if (currentPath == null || currentPath.Count == 0) return;
        }

        FollowPath(() => StartCoroutine(WaitAtPoint(() => { })));
    }

    private void FollowPath(System.Action onReachedDestination)
    {
        if (currentPath != null && currentPath.Count > 0)
        {
            Vector3 target = currentPath[0].worldPosition;
            target.y = transform.position.y;

            Vector3 dir = steering.Seek(target);
            Move(dir);

            if (Vector3.Distance(transform.position, target) < 0.5f)
            {
                currentPath.RemoveAt(0);
            }
        }
        else
        {
            currentPath = null;
            onReachedDestination?.Invoke();
        }
    }

    private IEnumerator WaitAtPoint(System.Action onComplete)
    {
        isWaiting = true;

        // APAGAMOS TODO PARA QUE QUEDE EN IDLE PURO
        UpdateAnimation("isWalking", false);
        UpdateAnimation("isRunning", false);

        yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));

        onComplete?.Invoke();
        isWaiting = false;
    }
}