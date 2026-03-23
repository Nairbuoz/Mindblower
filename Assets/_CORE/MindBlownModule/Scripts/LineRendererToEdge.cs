using UnityEngine;

public class LineRendererToEdge : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private ParticleSystem particleSystem;

    private ParticleSystem.ShapeModule shapeModule;

    void Start()
    {
        if (particleSystem != null)
        {
            shapeModule = particleSystem.shape;
        }
    }

    void Update()
    {
        if (lineRenderer != null && particleSystem != null && lineRenderer.positionCount >= 2)
        {
            // Get the two points from the line renderer
            Vector3 point1 = lineRenderer.GetPosition(0);
            Vector3 point2 = lineRenderer.GetPosition(1);

            // Calculate the center position
            Vector3 center = (point1 + point2) / 2f;

            // Calculate the radius (half the distance between the points)
            float radius = Vector3.Distance(point1, point2) / 2f;

            // Calculate rotation to align with the line direction
            Vector3 direction = (point2 - point1).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

            // Update particle system shape
            shapeModule.position = transform.InverseTransformPoint(lineRenderer.transform.TransformPoint(center));
            shapeModule.radius = radius;
            shapeModule.rotation = rotation.eulerAngles;
            
            
        }
    }
}
