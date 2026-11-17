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
        foreach (var (botRef, position) in PathfindingStore.DestinationPositions)
        {
            var bot = botRef.Value;
            var botIdx = botRef.Idx;

            var startingPos = bot.PlayerStore.Position;
            var targetPos = position;

            if (!NavMesh.CalculatePath(startingPos, targetPos, NavMesh.AllAreas, PathfindingStore.NavMeshPaths[botIdx]))
            {
                outClosestPositions.Add((new(botIdx), targetPos, default));
                PathfindingStore.GoalPositions[botIdx] = targetPos;

                continue;
            }

            var needsJump = false;
            var closestPosition = PathfindingStore.NavMeshPaths[botIdx].corners.First();

            outClosestPositions.Add((new(botIdx), closestPosition, needsJump));
            PathfindingStore.GoalPositions[botIdx] = targetPos;
        }
    }
}
