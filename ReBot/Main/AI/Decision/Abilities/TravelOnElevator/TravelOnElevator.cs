using ReBot.Main.AI.State.Spacial;
using Unity.Collections;
using UnityEngine;

namespace ReBot.Main.AI.Decision.Abilities.TravelOnElevator;

internal struct TravelOnElevator(BotRef botRef)
{
    Vector3 playerPosition = botRef.Value.PlayerStore.Position;

    NativeList<ObstacleRef> elevationTravelObstacles = ObstacleStore.ElevationTravelObstacles;
    NativeList<Vector3> elevationTravelChamberLocations = ObstacleStore.ElevationTravelChamberLocations;
    NativeList<Vector3> elevationTravelInteractLocations = ObstacleStore.ElevationTravelInteractLocations;
    NativeList<bool> elevationTravelInteractAbles = ObstacleStore.ElevationTravelInteractAbles;

    NativeReference<(bool, Vector3)> interactPositionRef = botRef.Value.InteractionStore.InteractWithPositionRef;
    NativeReference<(bool, Vector3)> lookAtPositionRef = botRef.Value.MovementStore.LookAtPositionRef;

    public bool TryGetElevationIdx(ObstacleRef obstacleRef, out int elevationIdx)
    {
        if (elevationTravelObstacles.IndexOf(obstacleRef) is var resultElevationIdx && resultElevationIdx > -1)
        {
            elevationIdx = resultElevationIdx;
            return true;
        }

        elevationIdx = default;
        return false;
    }

    public void Impacting(int elevationIdx, NativeList<ImpactingAction> impactingActions)
    {
        var cost = 5f;

        impactingActions.Add(cost, ActionEnum.TravelsOn | ActionEnum.Elevator, elevationIdx);
    }

    public void Enabling(int elevationIdx, NativeList<BeliefRef> beliefsToEnable)
    {
        var playerToChamberDist = Vector3.Distance(elevationTravelChamberLocations[elevationIdx], playerPosition);
        if (playerToChamberDist < 1f)
        {
            var travelInteractPosition = elevationTravelInteractLocations[elevationIdx];
            lookAtPositionRef.Value = (true, travelInteractPosition);

            if (elevationTravelInteractAbles[elevationIdx])
            {
                interactPositionRef.Value = (true, travelInteractPosition);
            }
        }
    }
}
