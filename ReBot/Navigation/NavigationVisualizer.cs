using DrawableLine;
using LabApi.Features.Wrappers;
using MEC;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace ReBot.Navigation;

internal static class NavigationVisualizer
{
    public static Player Player { get; private set; }
    public static Vector3? CachedPosition { get; set; }
    public static NavMeshPath TracePath { get; } = new NavMeshPath();

    private static CoroutineHandle handle;
    private static CoroutineHandle handle2;

    public static void Enable(Player player)
    {
        Player = player;

        handle = Timing.RunCoroutine(RunOncePerSecond(UpdateTriangulation));
        handle2 = Timing.RunCoroutine(RunOncePerSecond(UpdatePath));
    }

    public static void Disable()
    {
        Player = null;

        Timing.KillCoroutines(handle, handle2);
    }

    private static void UpdateTriangulation()
    {
        var triangulation = NavMesh.CalculateTriangulation();
        for (int idx = 0; idx < triangulation.indices.Length; idx += 3)
        {
            Span<int> vertexIndices = [
                triangulation.indices[idx + 0],
                triangulation.indices[idx + 1],
                triangulation.indices[idx + 2],
            ];
            Vector3[] vertexPositions = [
                triangulation.vertices[vertexIndices[0]],
                triangulation.vertices[vertexIndices[1]],
                triangulation.vertices[vertexIndices[2]],
                triangulation.vertices[vertexIndices[0]]
            ];

            if (Vector3.Distance(vertexPositions[0], Player.Position) > 10f)
            {
                continue;
            }

            DrawableLines.GenerateLine(1f, Color.yellow, vertexPositions);
        }

        var mapgenRoot = GameObject.Find("MapgenRoot");
        var navMeshLinks = mapgenRoot.GetComponents<NavMeshLink>();
        foreach (var navMeshLink in navMeshLinks)
        {
            var startTransform = navMeshLink.startTransform ? navMeshLink.startTransform : navMeshLink.transform;
            var endTransform = navMeshLink.endTransform ? navMeshLink.endTransform : navMeshLink.transform;
            Vector3[] vertexPositions = [
                startTransform.TransformPoint(navMeshLink.startPoint),
                endTransform.TransformPoint(navMeshLink.endPoint)
            ];

            var lineColor = Color.yellow;

            if (!NavMesh.IsLinkValid(navMeshLink.Instance))
            {
                lineColor = Color.red;
            }

            DrawableLines.GenerateLine(1f, lineColor, vertexPositions);
        }
    }

    private static void UpdatePath()
    {
        if (TracePath.corners.Length < 2)
        {
            return;
        }

        DrawableLines.GenerateLine(1f, Color.blue, TracePath.corners);
    }

    private static IEnumerator<float> RunEachFrame(Action action)
    {
        while (true)
        {
            action.Invoke();

            yield return Timing.WaitForOneFrame;
        }
    }

    private static IEnumerator<float> RunOncePerSecond(Action action)
    {
        while (true)
        {
            action.Invoke();

            yield return Timing.WaitForSeconds(1f);
        }
    }
}
