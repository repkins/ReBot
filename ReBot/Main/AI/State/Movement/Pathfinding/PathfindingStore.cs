using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ReBot.Main.AI.State.Movement.Pathfinding;

internal static class PathfindingStore
{
    public static readonly List<(BotRef, Vector3)> DestinationPositions = [];

    public static readonly List<Vector3> GoalPositions = [];

    public static readonly List<NavMeshPath> NavMeshPaths = [];

    public static void AddBot(BotRef botRef)
    {
        GoalPositions.Add(default);

        NavMeshPaths.Add(new NavMeshPath());
    }

    public static void RemoveBot(BotRef botRef)
    {
        NavMeshPaths.RemoveAt(botRef.Idx);

        GoalPositions.RemoveAt(botRef.Idx);
    }
}
