using System.Reflection;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace ReBot.Navigation;

internal static class NavMeshLinkExtensions
{
    private static readonly FieldInfo navMeshLinkInstanceField = typeof(NavMeshLink).GetField("m_LinkInstance", BindingFlags.NonPublic | BindingFlags.Instance);

    extension(NavMeshLink link)
    {
        public NavMeshLinkInstance Instance => (NavMeshLinkInstance)navMeshLinkInstanceField.GetValue(link);
    }
}
