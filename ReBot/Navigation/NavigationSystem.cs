using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using MapGeneration.StaticHelpers;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace ReBot.Navigation
{
    internal class NavigationSystem
    {
        public static readonly NavigationSystem Instance = new();

        public string BaseDir { get; set; }
        public bool Initialized { get; private set; } = false;
        public event Action OnMeshesConnected;
        public event Action OnMeshesReset;

        public void Init()
        {
            ServerEvents.MapGenerated += OnMapGenerated;
            ServerEvents.RoundRestarted += OnRoundRestarted;

            if (SeedSynchronizer.MapGenerated)
            {
                InitializeMeshes();
            }

            Initialized = true;
        }

        public void Teardown()
        {
            ServerEvents.MapGenerated -= OnMapGenerated;
            ServerEvents.RoundRestarted -= OnRoundRestarted;

            Initialized = false;

            ResetMeshes();
        }

        private void OnMapGenerated(MapGeneratedEventArgs args)
        {
            Timing.RunCoroutine(LoadConnectMeshesAsync());
        }

        private void OnRoundRestarted()
        {
            ResetMeshes();
        }

        private IEnumerator<float> LoadConnectMeshesAsync()
        {
            yield return Timing.WaitUntilTrue(() => SeedSynchronizer.MapGenerated);

            InitializeMeshes();
        }

        public void InitializeMeshes()
        {
            Debug.Log($"Adding NavMeshModifiers to doors for exclusion");

            var stringBuilder = new StringBuilder();
            foreach (var door in DoorVariant.AllDoors)
            {
                if (door is CheckpointDoor)
                {
                    continue;
                }

                var doorGameObject = door.gameObject;
                var navMeshModifier = doorGameObject.AddComponent<NavMeshModifier>();
                navMeshModifier.ignoreFromBuild = true;
            }

            var elevatorGroups = Enum.GetValues(typeof(ElevatorGroup));
            foreach (ElevatorGroup group in elevatorGroups)
            {
                if (!ElevatorChamber.TryGetChamber(group, out var chamber))
                {
                    Debug.LogWarning($"Could not get elevator chamber of group {group}");
                    continue;
                }

                var elevatorGameObject = chamber.gameObject;

                var elevatorNavMeshSurface = elevatorGameObject.AddComponent<NavMeshSurface>();
                elevatorNavMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                elevatorNavMeshSurface.collectObjects = CollectObjects.Children;
                elevatorNavMeshSurface.layerMask = LayerMask.GetMask("Default", "Fence");
                elevatorNavMeshSurface.BuildNavMesh();

                var navMeshModifier = elevatorGameObject.AddComponent<NavMeshModifier>();
                navMeshModifier.ignoreFromBuild = true;
            }

            var mapgenRoot = GameObject.Find("MapgenRoot");
            var mapNavMeshSurface = mapgenRoot.AddComponent<NavMeshSurface>();
            mapNavMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            mapNavMeshSurface.collectObjects = CollectObjects.All;
            mapNavMeshSurface.layerMask = LayerMask.GetMask("Default", "Fence", "Glass");

            Debug.Log($"navMeshSurface.useGeometry = {mapNavMeshSurface.useGeometry}");
            Debug.Log($"navMeshSurface.collectObjects = {mapNavMeshSurface.collectObjects}");
            Debug.Log($"navMeshSurface.layerMask = {mapNavMeshSurface.layerMask}");

            var buildSettings = mapNavMeshSurface.GetBuildSettings();
            Debug.Log($"buildSettings.agentRadius = {buildSettings.agentRadius}");
            Debug.Log($"buildSettings.agentHeight = {buildSettings.agentHeight}");
            Debug.Log($"buildSettings.agentClimb = {buildSettings.agentClimb}");
            Debug.Log($"buildSettings.agentSlope = {buildSettings.agentSlope}");
            Debug.Log($"buildSettings.ledgeDropHeight = {buildSettings.ledgeDropHeight}");
            Debug.Log($"buildSettings.maxJumpAcrossDistance = {buildSettings.maxJumpAcrossDistance}");
            Debug.Log($"buildSettings.minRegionArea = {buildSettings.minRegionArea}");

            Debug.Log($"Building navigation mesh.");
            mapNavMeshSurface.BuildNavMesh();

            Debug.Log($"Adding door obstacles.");
            foreach (var door in DoorVariant.AllDoors)
            {
                if (door is CheckpointDoor or ElevatorDoor)
                {
                    continue;
                }

                if (door.Rooms.Length == 2)
                {
                    var doorCenterPosition = door.transform.position + Vector3.up;  // assuming pivot point is located at the bottom of all doors

                    // Add to obstacles
                    var doorInstanceId = door.GetInstanceID();
                    NavigationStore.SolidObstaclesByInstancedIds.Add(doorInstanceId, (door.transform, default));

                    // Add to connections
                    NavigationStore.ConnectionsByRoomPair.Add((door.Rooms[0], door.Rooms[1]), doorCenterPosition);
                    NavigationStore.ConnectionsByRoomPair.Add((door.Rooms[1], door.Rooms[0]), doorCenterPosition);
                }
                else
                {
                    var emergencyRelease = door.Rooms[0].GetComponentsInChildren<DynamicChildController>()
                        .Where(c => c.Child)
                        .Select(c => c.Child.GetComponentInChildren<EmergencyDoorRelease>())
                        .FirstOrDefault(c => c && Vector3.Distance(c.transform.position, door.transform.position) < 5f);

                    Debug.Log($"{door.Rooms[0].gameObject.name}, {door.gameObject.name}, {door.RequiredPermissions.RequiredPermissions}, {emergencyRelease?.gameObject.name}");

                    var doorInstanceId = door.GetInstanceID();
                    NavigationStore.SolidObstaclesByInstancedIds.Add(doorInstanceId, (door.transform, emergencyRelease));
                }
            }

            Debug.Log($"Adding connections between rooms.");
            foreach (var connector in RoomConnector.AllConnectors)
            {
                var connectionPosition = connector.transform.position + Vector3.up;  // assuming pivot point is located at the bottom of all connectors

                // Add to connections
                NavigationStore.ConnectionsByRoomPair.Add((connector.Rooms[0], connector.Rooms[1]), connectionPosition);
                NavigationStore.ConnectionsByRoomPair.Add((connector.Rooms[1], connector.Rooms[0]), connectionPosition);
            }

            Debug.Log($"Adding tesla gate obstacles");
            foreach (var teslaGate in TeslaGate.AllGates)
            {
                Debug.Log($"{teslaGate.Room.gameObject.name}, {teslaGate.gameObject.name}");

                var instanceId = teslaGate.GetInstanceID();
                NavigationStore.TeslaGateObstaclesByInstanceId.Add(instanceId, teslaGate.transform);
            }

            Debug.Log($"Connecting elevator destinations and adding elevation obstacles.");
            foreach (ElevatorGroup group in elevatorGroups)
            {
                var elevatorDoors = ElevatorDoor.GetDoorsForGroup(group);
                if (elevatorDoors.Count != 2)
                {
                    Debug.LogWarning($"Irregular elevator level count ({elevatorDoors.Count}) of group {group}");
                    continue;
                }

                if (!ElevatorChamber.TryGetChamber(group, out var chamber))
                {
                    Debug.LogWarning($"Could not get elevator chamber of group {group}");
                    continue;
                }

                Span<Transform> doorTransforms = [elevatorDoors[0].transform, elevatorDoors[1].transform];
                Span<Vector3> doorPositions = [doorTransforms[0].position, doorTransforms[1].position];
                Span<Vector3> doorForwards = [doorTransforms[0].forward, doorTransforms[1].forward];

                var chamberDestDoorPosition = chamber.DestinationDoor.transform.position;
                var chamberDestDoorForward = chamber.DestinationDoor.transform.forward;

                // 
                // Connect
                //

                var link0PositionInside = chamberDestDoorPosition - chamberDestDoorForward;
                var link0PositionOutside = doorPositions[0] + doorForwards[0];

                var elevationMeshLink0 = mapgenRoot.AddComponent<NavMeshLink>();
                elevationMeshLink0.startTransform = chamber.transform;
                elevationMeshLink0.startPoint = chamber.transform.InverseTransformPoint(link0PositionInside);
                elevationMeshLink0.endPoint = mapgenRoot.transform.InverseTransformPoint(link0PositionOutside);
                elevationMeshLink0.costModifier = 5f;
                elevationMeshLink0.bidirectional = true;

                var elevationMeshLink0Valid = NavMesh.IsLinkValid(elevationMeshLink0.Instance);

                Debug.Log($"{group} elevation nav link added between {link0PositionInside} and {link0PositionOutside} which is {(elevationMeshLink0Valid ? "valid": "invalid")}");

                var link1PositionInside = chamberDestDoorPosition - chamberDestDoorForward;
                var link1PositionOutside = doorPositions[1] + doorForwards[1];

                var elevationMeshLink1 = mapgenRoot.AddComponent<NavMeshLink>();
                elevationMeshLink1.startTransform = chamber.transform;
                elevationMeshLink1.startPoint = chamber.transform.InverseTransformPoint(link1PositionInside);
                elevationMeshLink1.endPoint = mapgenRoot.transform.InverseTransformPoint(link1PositionOutside);
                elevationMeshLink1.costModifier = 5f;
                elevationMeshLink1.bidirectional = true;

                var elevationMeshLink1Valid = NavMesh.IsLinkValid(elevationMeshLink1.Instance);

                Debug.Log($"{group} elevation nav link added between {link1PositionInside} and {link1PositionOutside} which is {(elevationMeshLink1Valid ? "valid" : "invalid")}");

                //
                // Add elevation obstacles
                //

                var panelAt0 = elevatorDoors[0].GetComponentInChildren<ElevatorPanel>();
                var panelAt1 = elevatorDoors[1].GetComponentInChildren<ElevatorPanel>();

                var travelPanel = chamber.GetComponentInChildren<ElevatorPanel>();

                var inside0Position = doorPositions[0] - doorForwards[0] * 2f + Vector3.up;
                var inside1Position = doorPositions[1] - doorForwards[1] * 2f + Vector3.up;

                NavigationStore.ElevationObstacles.Add(((link0PositionOutside, inside0Position), (link1PositionOutside, inside1Position), panelAt1, travelPanel));
                NavigationStore.ElevationObstacles.Add(((link1PositionOutside, inside1Position), (link0PositionOutside, inside0Position), panelAt0, travelPanel));

                //Debug.Log($"{group} elevation obstacles added between call panels at {panelAt0.transform.position} and {panelAt1.transform.position} with travel panel at {travelPanel.transform.position}");
            }
            Debug.Log($"Adding obstacles and connecting rooms finished.");

            OnMeshesConnected?.Invoke();
        }

        public void ResetMeshes()
        {
            Debug.Log($"Resetting navigation mesh.");

            var mapgenRoot = GameObject.Find("MapgenRoot");
            var navMeshSurface = mapgenRoot.gameObject.GetComponent<NavMeshSurface>();
            if (navMeshSurface)
            {
                navMeshSurface.RemoveData();
                UnityEngine.Object.Destroy(navMeshSurface);
            }

            var navMeshLinks = mapgenRoot.gameObject.GetComponents<NavMeshLink>();
            foreach (var link in navMeshLinks)
            {
                UnityEngine.Object.Destroy(link);
            }

            NavigationStore.SolidObstaclesByInstancedIds.Clear();
            NavigationStore.ConnectionsByRoomPair.Clear();
            NavigationStore.ElevationObstacles.Clear();
            NavigationStore.TeslaGateObstaclesByInstanceId.Clear();

            OnMeshesReset?.Invoke();
        }
    }
}