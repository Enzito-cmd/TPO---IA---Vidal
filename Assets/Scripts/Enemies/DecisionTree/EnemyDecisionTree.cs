using UnityEngine;

public class EnemyDecisionTree : MonoBehaviour
{
    private DecisionNode rootNode;

    private void Awake()
    {
        // 1. Creamos las Hojas de Acción conectadas al EnemyControllerTree
        ActionNode attackAction = new ActionNode(c => c.DoAttack());
        ActionNode pursueAction = new ActionNode(c => c.DoPursue());
        ActionNode fleeAction = new ActionNode(c => c.DoFlee());
        ActionNode rotateAction = new ActionNode(c => c.DoIdleRotate());
        ActionNode patrolAction = new ActionNode(c => c.DoPatrol());
        ActionNode randomPatrol = new ActionNode(c => c.DoRandomPatrol());

        // 2. Rama de Combate (Cuando VE al jugador)
        QuestionNode attackOrPursue = new QuestionNode(
            ctx => ctx.player != null && Vector3.Distance(ctx.self.position, ctx.player.position) < 2.0f,
            attackAction,  // True: Ataca
            pursueAction   // False: Persigue con Steering
        );

        QuestionNode combatBranch = new QuestionNode(
            ctx => ctx.aiType == EnemyType.VIP,
            fleeAction,    // True: Si es VIP, huye
            attackOrPursue // False: Si es Patrol o Sentry, evalúa si atacar o perseguir
        );

        // 3. Rama de Calma (Cuando NO ve al jugador)
        QuestionNode calmPatrolBranch = new QuestionNode(
            ctx => ctx.aiType == EnemyType.VIP,
            randomPatrol,  // True: VIP patrulla aleatorio por nodos
            patrolAction   // False: Patrol camina por sus puntos en orden
        );

        QuestionNode calmBranch = new QuestionNode(
            ctx => ctx.aiType == EnemyType.Sentry,
            rotateAction,     // True: Sentry se queda girando en el lugar
            calmPatrolBranch  // False: Evalúa patrullas
        );

        // 4. Raíz Principal: ¿Ve al jugador?
        rootNode = new QuestionNode(
            ctx => ctx.player != null &&
                   ctx.los != null &&
                   ctx.los.CheckRange(ctx.self, ctx.player) &&
                   ctx.los.CheckAngle(ctx.self, ctx.player) &&
                   ctx.los.CheckObstacles(ctx.self, ctx.player),
            combatBranch, // True: Entra en combate
            calmBranch    // False: Entra en calma
        );
    }

    public void Evaluate(EnemyControllerTree enemy, EnemyContext context)
    {
        if (rootNode != null)
        {
            rootNode.Evaluate(enemy, context);
        }
    }
}