using UnityEngine;

public class HoopAssistZone : MonoBehaviour
{
    public float pullStrength = 5f;
    public Transform targetPoint;

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && other.CompareTag("Basketball"))
        {
            Vector3 direction = (targetPoint.position - other.transform.position).normalized;
            float distance = Vector3.Distance(targetPoint.position, other.transform.position);
            float assist = pullStrength / Mathf.Max(distance, 0.1f); // Stronger when closer
            rb.AddForce(direction * assist, ForceMode.Acceleration);
        }
    }
}
