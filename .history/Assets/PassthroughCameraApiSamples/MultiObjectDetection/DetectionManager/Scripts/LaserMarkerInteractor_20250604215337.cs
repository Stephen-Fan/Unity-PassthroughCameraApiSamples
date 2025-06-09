using UnityEngine;

public class LaserMarkerInteractor : MonoBehaviour
{
    public Transform controllerTransform; // Assign your controller (right hand or laser origin)
    private MarkerPopupHandler lastHitMarker = null;

    void Update()
    {
        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Debug.Log("[]Ray hit: " + hit.collider.name);  // ← Add this line

            MarkerPopupHandler marker = hit.collider.GetComponent<MarkerPopupHandler>();
            if (marker != null)
            {
                if (marker != lastHitMarker)
                {
                    if (lastHitMarker != null)
                        lastHitMarker.HidePopup();

                    marker.ShowPopup("Object: " + marker.name); // or any info you want
                    lastHitMarker = marker;
                }
            }
            else if (lastHitMarker != null)
            {
                lastHitMarker.HidePopup();
                lastHitMarker = null;
            }
        }
        else if (lastHitMarker != null)
        {
            lastHitMarker.HidePopup();
            lastHitMarker = null;
        }
    }
}
