using UnityEngine;

public class SimpleLaserPointer : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float laserLength = 10f;

    void Start()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.002f;
            lineRenderer.endWidth = 0.002f;
            lineRenderer.enabled = false; // hide laser at start
        }
    }

    void Update()
    {
        // Check right controller grip (OVR or Unity input system)
        bool gripPressed = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);

        if (gripPressed)
        {
            if (!lineRenderer.enabled)
                lineRenderer.enabled = true;

            Ray ray = new Ray(transform.position, transform.forward);
            lineRenderer.SetPosition(0, ray.origin);

            if (Physics.Raycast(ray, out RaycastHit hit, laserLength))
            {
                lineRenderer.SetPosition(1, hit.point);
            }
            else
            {
                lineRenderer.SetPosition(1, ray.origin + ray.direction * laserLength);
            }
        }
        else
        {
            if (lineRenderer.enabled)
                lineRenderer.enabled = false;
        }
    }
}
