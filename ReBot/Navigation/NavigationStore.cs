using Interactables.Interobjects;
using MapGeneration;
using System.Collections.Generic;
using UnityEngine;

namespace ReBot.Navigation;

internal static class NavigationStore
{
    public static readonly Dictionary<int, (Transform, EmergencyDoorRelease)> SolidObstaclesByInstancedIds = [];
    public static readonly Dictionary<(RoomIdentifier From, RoomIdentifier To), Vector3> ConnectionsByRoomPair = [];
}
