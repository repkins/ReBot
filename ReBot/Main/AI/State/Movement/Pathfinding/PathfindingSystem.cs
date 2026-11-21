using DrawableLine;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace ReBot.Main.AI.State.Movement.Pathfinding;

internal static class PathfindingSystem
{
    const float SineOfJumpThresholdAngles = 0.5f;    // 30 degrees

    public static void Process(NativeList<(BotRef, Vector3, bool)> outClosestPositions)
    {
        foreach (var (botRef, targetPos) in PathfindingStore.DestinationPositions)
        {
            var bot = botRef.Value;
            var botIdx = botRef.Idx;

            var startingPos = bot.PlayerStore.Position;
            var previousGoalPos = PathfindingStore.GoalPositions[botIdx];

            if (bot.Hub.GetComponent<NavMeshAgent>() is not NavMeshAgent navMeshAgent)
            {
                navMeshAgent = bot.Hub.gameObject.AddComponent<NavMeshAgent>();
                navMeshAgent.baseOffset = startingPos.y - bot.Hub.transform.position.y;

                if (!navMeshAgent.isOnNavMesh)
                {
                    Object.Destroy(navMeshAgent);
                    continue;
                }
            }

            if (navMeshAgent.SetDestination(targetPos))
            {
                navMeshAgent.isStopped = true;
            }

            if (navMeshAgent.pathPending)
            {
                continue;
            }

            if (navMeshAgent.hasPath)
            {
                var needsJump = false;
                var closestPosition = navMeshAgent.steeringTarget;
                outClosestPositions.Add((new(botIdx), closestPosition, needsJump));

                PathfindingStore.NavMeshPaths[botIdx] = navMeshAgent.path;
            }
            else
            {
                outClosestPositions.Add((new(botIdx), targetPos, default));

                DrawableLines.GenerateSphere(targetPos, .1f, Color.red);
            }

            PathfindingStore.GoalPositions[botIdx] = targetPos;
        }
    }
}
