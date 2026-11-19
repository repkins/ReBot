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
        foreach (var (botRef, position) in PathfindingStore.DestinationPositions)
        {
            var bot = botRef.Value;
            var botIdx = botRef.Idx;

            var startingPos = bot.PlayerStore.Position;
            var targetPos = position;
            var previousGoalPos = PathfindingStore.GoalPositions[botIdx];

            var navMeshPath = PathfindingStore.NavMeshPaths[botIdx];

            if (!NavMesh.CalculatePath(startingPos, targetPos, NavMesh.AllAreas, navMeshPath))
            {
                outClosestPositions.Add((new(botIdx), targetPos, default));
                PathfindingStore.GoalPositions[botIdx] = targetPos;

                if (previousGoalPos != targetPos)
                {
                    Debug.LogWarning($"Failed to calculate path for bot {botIdx} from {startingPos} to {targetPos}");
                }

                DrawableLines.GenerateSphere(targetPos, .1f, Color.red);

                continue;
            }

            var needsJump = false;
            var closestPosition = navMeshPath.corners.Skip(1).First();

            outClosestPositions.Add((new(botIdx), closestPosition, needsJump));
            PathfindingStore.GoalPositions[botIdx] = targetPos;

            DrawableLines.GenerateLine(navMeshPath.corners);
        }
    }
}
