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

            var mapgenRoot = GameObject.Find("MapgenRoot");
            var navMeshSurface = mapgenRoot.AddComponent<NavMeshSurface>();
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurface.collectObjects = CollectObjects.All;
            navMeshSurface.layerMask = LayerMask.GetMask("Default", "Fence", "Glass");

            Debug.Log($"navMeshSurface.useGeometry = {navMeshSurface.useGeometry}");
            Debug.Log($"navMeshSurface.collectObjects = {navMeshSurface.collectObjects}");
            Debug.Log($"navMeshSurface.layerMask = {navMeshSurface.layerMask}");

            var buildSettings = navMeshSurface.GetBuildSettings();
            Debug.Log($"buildSettings.agentRadius = {buildSettings.agentRadius}");
            Debug.Log($"buildSettings.agentHeight = {buildSettings.agentHeight}");
            Debug.Log($"buildSettings.agentClimb = {buildSettings.agentClimb}");
            Debug.Log($"buildSettings.agentSlope = {buildSettings.agentSlope}");
            Debug.Log($"buildSettings.ledgeDropHeight = {buildSettings.ledgeDropHeight}");
            Debug.Log($"buildSettings.maxJumpAcrossDistance = {buildSettings.maxJumpAcrossDistance}");
            Debug.Log($"buildSettings.minRegionArea = {buildSettings.minRegionArea}");

            Debug.Log($"Building navigation mesh.");
            navMeshSurface.BuildNavMesh();

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

            NavigationStore.SolidObstaclesByInstancedIds.Clear();
            NavigationStore.ConnectionsByRoomPair.Clear();

            OnMeshesReset?.Invoke();
        }
    }
}