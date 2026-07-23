using UnityEditor;
using UnityEngine;

// ho bisogno che la porta funzioni in edit
[ExecuteInEditMode]
#if UNITY_EDITOR
public class DoorSpot : MonoBehaviour
{
    [SerializeField] private bool occupied = false;

    public bool Occupied { get => occupied; set => occupied = value; }

    private void OnDrawGizmos()
    {
        if (occupied) return;

        // gizmo per migliorare la leggibilità delle porte
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.2f);
        Handles.ArrowHandleCap(1, transform.position, transform.rotation, 1, EventType.Repaint);
    }
}
#endif