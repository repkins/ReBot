using ReBot.Main.AI.Decision.Abilities.Target;
using ReBot.Main.AI.State.Spacial;
using Unity.Collections;
using UnityEngine;

namespace ReBot.Main.AI.Decision.Target;

internal struct Elevator() : ITarget
{
    public NativeList<Vector3> Locations { get; } = ObstacleStore.ElevationTravelInsideLocations;

    public ActionEnum Enum { get; } = ActionEnum.Elevator;
}
