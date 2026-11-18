using HarmonyLib;
using UnityEngine.AI;

namespace ReBot.Navigation;

[HarmonyPatch(typeof(NavMesh))]
internal static class NavMeshPatches
{
    [HarmonyPatch(nameof(NavMesh.GetSettingsByID))]
    [HarmonyPostfix]
    static void GetBuildSettings_Postfix(ref NavMeshBuildSettings __result)
    {
        __result.agentRadius = 0.3f;
        __result.agentClimb = 0.2f;
    }
}
