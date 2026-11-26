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
        foreach (var (botRef, goalPos) in PathfindingStore.DestinationPositions)
        {
            var bot = botRef.Value;
            var botIdx = botRef.Idx;

            var playerPos = bot.PlayerStore.Position;

            if (bot.Hub.GetComponent<NavMeshAgent>() is not NavMeshAgent navMeshAgent)
            {
                navMeshAgent = bot.Hub.gameObject.AddComponent<NavMeshAgent>();
                navMeshAgent.baseOffset = playerPos.y - bot.Hub.transform.position.y;

                if (!navMeshAgent.isOnNavMesh)
                {
                    Object.Destroy(navMeshAgent);
                    continue;
                }
            }

            var targetPos = goalPos;
            var previousGoalPos = PathfindingStore.GoalPositions[botIdx];

            var needsNewPath = navMeshAgent.hasPath 
                && navMeshAgent.isOnOffMeshLink && !navMeshAgent.currentOffMeshLinkData.valid;

            if (targetPos != previousGoalPos || needsNewPath)
            {
                if (NavMesh.SamplePosition(targetPos, out var hit, 1f, NavMesh.AllAreas))
                {
                    targetPos = hit.position;
                }
                else
                {
                    Debug.Log($"Could not sample position {targetPos} for agent {navMeshAgent}");
                }

                if (navMeshAgent.isOnOffMeshLink)
                {
                    navMeshAgent.Warp(playerPos);
                }

                navMeshAgent.ResetPath();
                if (navMeshAgent.SetDestination(targetPos))
                {
                    navMeshAgent.isStopped = true;
                    PathfindingStore.GoalPositions[botIdx] = goalPos;
                }
                else
                {
                    Debug.Log($"Could not set destination {targetPos} for agent {navMeshAgent}");
                }
            }

            if (navMeshAgent.pathPending)
            {
                continue;
            }

            if (navMeshAgent.hasPath)
            {
                var playerPositionWithOffset = playerPos + Vector3.down * navMeshAgent.baseOffset;
                if (navMeshAgent.isOnOffMeshLink && Vector3.Distance(playerPositionWithOffset, navMeshAgent.steeringTarget) < .1f)
                {
                    Debug.Log($"Completing off mesh link for agent {navMeshAgent}");

                    navMeshAgent.CompleteOffMeshLink();
                    navMeshAgent.isStopped = true;
                }

                var needsJump = false;
                var closestPosition = navMeshAgent.steeringTarget;
                outClosestPositions.Add((new(botIdx), closestPosition, needsJump));
            }
            else
            {
                outClosestPositions.Add((new(botIdx), targetPos, default));

                DrawableLines.GenerateSphere(targetPos, .1f, Color.red);
            }
        }
    }
}
