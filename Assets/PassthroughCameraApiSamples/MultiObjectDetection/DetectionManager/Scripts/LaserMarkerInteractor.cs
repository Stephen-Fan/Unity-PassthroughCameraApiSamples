using UnityEngine;

public class LaserMarkerInteractor : MonoBehaviour
{
    public Transform controllerTransform; // Assign your controller (right hand or laser origin)
    private MarkerPopupHandler lastHitMarker = null;

    void Update()
    {
        // Only raycast if the right grip button is held
        bool gripHeld = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        if (!gripHeld)
        {
            // Hide any currently shown popup if grip is released
            if (lastHitMarker != null)
            {
                lastHitMarker.HidePopup();
                lastHitMarker = null;
            }
            return;
        }

        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // Debug.Log("[Laser] Ray hit: " + hit.collider.name);  // ← Add this line

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
