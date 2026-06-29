using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType { Patrol, Sentry, VIP }

public class FSM_Classes : MonoBehaviour
{
    [Header("AI Type")]
    public EnemyType AIType;

    private State currentState;
    private EnemyController controller;
    private LineOfSight lineOfSight;

    void Start()
    {
        controller = GetComponent<EnemyController>();
        lineOfSight = GetComponent<LineOfSight>();

        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1f);

        switch (AIType)
        {
            case EnemyType.Patrol:
                ChangeState(new PatrolState(this, controller));
                break;
            case EnemyType.Sentry:
                ChangeState(new IdleRotateState(this, controller));
                break;
            case EnemyType.VIP:
                ChangeState(new RandomPatrolState(this, controller, controller.patrolPoints));
                break;
        }
    }

    void Update()
    {
        bool canSee = false;

        if (controller.player != null)
        {
            bool inRange = lineOfSight.CheckRange(transform, controller.player);
            bool inAngle = lineOfSight.CheckAngle(transform, controller.player);
            bool noObstacles = lineOfSight.CheckObstacles(transform, controller.player);

            canSee = inRange && inAngle && noObstacles;
        }

        float dist = controller.player != null ? Vector3.Distance(transform.position, controller.player.position) : 999f;

        UpdateState(canSee, dist);
    }

    public void UpdateState(bool canSeePlayer, float distanceToPlayer)
    {
        if (currentState != null)
        {
            currentState.Update(canSeePlayer, distanceToPlayer);
        }
    }

    public void ChangeState(State newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;
        currentState.Enter();
    }
}

public abstract class State
{
    protected FSM_Classes fsm;
    protected EnemyController c;

    public State(FSM_Classes fsm, EnemyController c)
    {
        this.fsm = fsm;
        this.c = c;
    }

    public virtual void Enter() { }
    public virtual void Update(bool canSee, float dist) { }
    public virtual void Exit() { }
}

public class PatrolState : State
{
    private List<Node> currentPath;
    private int pathIndex = 0;
    private bool isWaiting = false;

    public PatrolState(FSM_Classes fsm, EnemyController c) : base(fsm, c) { }

    public override void Enter()
    {
        isWaiting = false;
        currentPath = null;
        c.UpdateAnimation("isRunning", false);
        c.UpdateAnimation("isWalking", true);
    }

    public override void Update(bool canSee, float dist)
    {
        if (canSee)
        {
            fsm.ChangeState(new PursuitState(fsm, c));
            return;
        }

        if (isWaiting)
        {
            return;
        }

        if (c.patrolPoints == null || c.patrolPoints.Count == 0)
        {
            return;
        }

        if (pathIndex >= c.patrolPoints.Count)
        {
            pathIndex = 0;
        }

        if (currentPath == null)
        {
            currentPath = c.pathfinding.FindPath(c.transform.position, c.patrolPoints[pathIndex].position);

            if (currentPath == null || currentPath.Count == 0)
            {
                pathIndex = (pathIndex + 1) % c.patrolPoints.Count;
                return;
            }
        }

        if (currentPath.Count > 0)
        {
            Vector3 target = currentPath[0].worldPosition;
            Vector3 dir = c.steering.Seek(target);
            c.Move(dir);

            if (Vector3.Distance(c.transform.position, target) < 0.5f)
            {
                currentPath.RemoveAt(0);
            }
        }
        else
        {
            currentPath = null;
            fsm.StartCoroutine(WaitAtPoint());
        }
    }

    private IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        c.UpdateAnimation("isWalking", false);

        yield return new WaitForSeconds(2.0f);

        pathIndex = (pathIndex + 1) % c.patrolPoints.Count;
        isWaiting = false;
        c.UpdateAnimation("isWalking", true);
    }

    public override void Exit()
    {
        c.UpdateAnimation("isWalking", false);
    }
}

public class PursuitState : State
{
    public PursuitState(FSM_Classes fsm, EnemyController c) : base(fsm, c) { }

    public override void Enter()
    {
        c.UpdateAnimation("isWalking", false);
        c.UpdateAnimation("isRunning", true);
    }

    public override void Update(bool canSee, float dist)
    {
        if (!canSee)
        {
            if (fsm.AIType == EnemyType.Sentry)
            {
                c.UpdateAnimation("isRunning", false);
                c.UpdateAnimation("isWalking", true);

                Vector3 dirToSpawn = (c.spawnPosition - c.transform.position).normalized;
                dirToSpawn.y = 0;

                c.transform.position = Vector3.MoveTowards(c.transform.position, c.spawnPosition, c.walkSpeed * Time.deltaTime);

                if (dirToSpawn != Vector3.zero)
                {
                    c.transform.rotation = Quaternion.Slerp(c.transform.rotation, Quaternion.LookRotation(dirToSpawn), Time.deltaTime * 10f);
                }

                if (Vector3.Distance(c.transform.position, c.spawnPosition) < 0.2f)
                {
                    fsm.ChangeState(new IdleRotateState(fsm, c));
                }
            }
            else if (fsm.AIType == EnemyType.Patrol)
            {
                fsm.ChangeState(new PatrolState(fsm, c));
            }

            return;
        }

        if (dist < 2.0f)
        {
            fsm.ChangeState(new AttackState(fsm, c));
            return;
        }

        Vector3 dir = c.steering.Pursue(c.player, 0.0f);
        dir.y = 0;
        c.Move(dir);
    }
}

public class AttackState : State
{
    public AttackState(FSM_Classes fsm, EnemyController c) : base(fsm, c) { }

    public override void Enter()
    {
        c.StartCoroutine(AttackSeq());
    }

    private IEnumerator AttackSeq()
    {
        if (c.player != null)
        {
            PlayerHealth health = c.player.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage();
        }

        c.UpdateTrigger("Aim");
        yield return new WaitForSeconds(2.5f);

        c.UpdateTrigger("Shoot");
        yield return new WaitForSeconds(2.5f);
    }
}

public class IdleRotateState : State
{
    private float rotationSpeed = 45f;

    public IdleRotateState(FSM_Classes fsm, EnemyController c) : base(fsm, c) { }

    public override void Enter()
    {
        c.UpdateAnimation("isWalking", false);
        c.UpdateAnimation("isRunning", false);
    }

    public override void Update(bool canSee, float dist)
    {
        if (canSee)
        {
            fsm.ChangeState(new PursuitState(fsm, c));
            return;
        }

        c.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}

public class RandomPatrolState : State
{
    private List<Transform> patrolPoints;
    private List<Node> currentPath;
    private int lastIndex = -1;
    private bool isWaiting = false;

    public RandomPatrolState(FSM_Classes fsm, EnemyController c, List<Transform> points) : base(fsm, c)
    {
        this.patrolPoints = points;
    }

    public override void Enter()
    {
        c.UpdateAnimation("isRunning", false);
        c.UpdateAnimation("isWalking", true);
        isWaiting = false;
        currentPath = null;
    }

    public override void Update(bool canSee, float dist)
    {
        if (canSee)
        {
            fsm.ChangeState(new FleeState(fsm, c));
            return;
        }

        if (isWaiting) return;

        if (currentPath == null)
        {
            if (patrolPoints == null || patrolPoints.Count == 0) return;

            int nextIndex;
            do
            {
                nextIndex = Random.Range(0, patrolPoints.Count);
            } while (patrolPoints.Count > 1 && nextIndex == lastIndex);

            lastIndex = nextIndex;
            currentPath = c.pathfinding.FindPath(c.transform.position, patrolPoints[nextIndex].position);

            if (currentPath == null || currentPath.Count == 0) return;
        }

        if (currentPath != null && currentPath.Count > 0)
        {
            Vector3 target = currentPath[0].worldPosition;
            Vector3 dir = c.steering.Seek(target);
            c.Move(dir);

            if (Vector3.Distance(c.transform.position, target) < 0.5f)
            {
                currentPath.RemoveAt(0);
            }
        }
        else
        {
            currentPath = null;
            fsm.StartCoroutine(WaitAtPoint());
        }
    }

    private IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        c.UpdateAnimation("isWalking", false);

        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        isWaiting = false;
        c.UpdateAnimation("isWalking", true);
    }

    public override void Exit()
    {
        c.UpdateAnimation("isWalking", false);
    }
}

public class FleeState : State
{
    private float timer = 2.0f;

    public FleeState(FSM_Classes fsm, EnemyController c) : base(fsm, c) { }

    public override void Enter()
    {
        c.UpdateAnimation("isWalking", true);
        timer = 2f;
    }

    public override void Update(bool canSee, float dist)
    {
        timer -= Time.deltaTime;

        Vector3 fleeDir = c.steering.Flee(c.player.position);

        RaycastHit hit;
        if (Physics.Raycast(c.transform.position, fleeDir, out hit, 1.0f))
        {
            fleeDir = Vector3.RotateTowards(fleeDir, c.transform.right, 5f * Time.deltaTime, 0f);
        }

        c.Move(fleeDir);

        if (timer <= 0)
        {
            c.gameObject.SetActive(false);
            PlayerHealth healthScript = c.player.GetComponent<PlayerHealth>();

            if (healthScript != null)
            {
                healthScript.restartPanel.SetActive(true);
            }
        }
    }
}