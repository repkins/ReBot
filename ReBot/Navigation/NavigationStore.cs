using Interactables.Interobjects;
using MapGeneration;
using System.Collections.Generic;
using UnityEngine;

namespace ReBot.Navigation;

using DestinationTuple = (Vector3 Link, Vector3 Enter);

internal static class NavigationStore
{
    public static readonly Dictionary<int, (Transform, EmergencyDoorRelease)> SolidObstaclesByInstancedIds = [];
    public static readonly Dictionary<(RoomIdentifier From, RoomIdentifier To), Vector3> ConnectionsByRoomPair = [];
    public static readonly List<(DestinationTuple ToDestination, DestinationTuple FromDestination, ElevatorPanel ToCallPanel, ElevatorPanel TravelPanel)> ElevationObstacles = [];
    public static readonly Dictionary<int, Transform> TeslaGateObstaclesByInstanceId = [];
}
