using Interactables.Interobjects;
using System.Collections.Generic;
using UnityEngine;

namespace ReBot.Navigation;

internal static class NavigationStore
{
    public static readonly Dictionary<int, (Transform, EmergencyDoorRelease)> SolidObstaclesByInstancedIds = [];
}
