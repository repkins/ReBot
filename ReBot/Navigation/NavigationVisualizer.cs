using DrawableLine;
using LabApi.Features.Wrappers;
using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ReBot.Navigation;

internal static class NavigationVisualizer
{
    public static Player Player { get; private set; }

    private static CoroutineHandle handle;

    public static void Enable(Player player)
    {
        Player = player;

        handle = Timing.RunCoroutine(RunOncePerSecond(UpdateTriangulation));
    }

    public static void Disable()
    {
        Player = null;

        Timing.KillCoroutines(handle);
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

            DrawableLines.GenerateLine(1f, vertexPositions);
        }
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
