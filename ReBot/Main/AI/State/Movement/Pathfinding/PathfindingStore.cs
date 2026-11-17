using ReBot.Navigation.Mesh;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace ReBot.Main.AI.State.Movement.Pathfinding;

internal static class PathfindingStore
{
    public static readonly List<(BotRef, Vector3)> DestinationPositions = [];
    public static readonly List<(BotRef, Vector3)> ClosestPositions = [];

    public static readonly List<Vector3> GoalPositions = [];

    public static readonly List<List<TransformCell>> CellsPath = [];
    public static readonly List<IEnumerable<(TransformCell Cell, TransformCell NextCell)>> CellPathSegments = [];

    public static readonly List<TransformCell?> CellWithins = [];
    public static readonly List<TransformCell?> PrevCellWithins = [];

    public static readonly List<NavMeshPath> NavMeshPaths = [];

    public static void AddBot(BotRef botRef)
    {
        GoalPositions.Add(default);

        NavMeshPaths.Add(new NavMeshPath());
        CellsPath.Add([]);
        CellPathSegments.Add(CellsPath[botRef.Idx].Zip(CellsPath[botRef.Idx].Skip(1), (cell, nextCell) => (cell, nextCell)));

        CellWithins.Add(default);
        PrevCellWithins.Add(default);
    }

    public static void RemoveBot(BotRef botRef)
    {
        CellWithins.RemoveAt(botRef.Idx);
        PrevCellWithins.RemoveAt(botRef.Idx);

        CellPathSegments.RemoveAt(botRef.Idx);
        CellsPath.RemoveAt(botRef.Idx);
        NavMeshPaths.RemoveAt(botRef.Idx);

        GoalPositions.RemoveAt(botRef.Idx);
    }
}
