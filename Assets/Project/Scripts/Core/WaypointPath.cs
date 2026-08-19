using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    public Transform[] waypoints;

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(
                    waypoints[i].position,
                    waypoints[i + 1].position
                );
            }
        }
    }
}