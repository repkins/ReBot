using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using MEC;
using System;
using System.Collections.Generic;
using System.IO;
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
            Debug.Log($"Building navigation mesh.");

            var mapgenRoot = GameObject.Find("MapgenRoot");
            var navMeshSurface = mapgenRoot.AddComponent<NavMeshSurface>();
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurface.collectObjects = CollectObjects.All;
            navMeshSurface.BuildNavMesh();

            Debug.Log($"Building navigation mesh done.");

            OnMeshesConnected?.Invoke();
        }

        public void ResetMeshes()
        {
            Debug.Log($"Resetting navigation mesh.");

            var mapgenRoot = GameObject.Find("MapgenRoot");
            var navMeshSurface = mapgenRoot.gameObject.GetComponent<NavMeshSurface>();
            if (navMeshSurface)
            {
                UnityEngine.Object.Destroy(navMeshSurface);
            }

            Debug.Log($"Resetting navigation mesh done.");

            OnMeshesReset?.Invoke();
        }
    }
}