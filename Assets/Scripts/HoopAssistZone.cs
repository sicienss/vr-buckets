using UnityEngine;

public class HoopAssistZone : MonoBehaviour
{
    float pullStrength = 7.5f;
    public Transform targetPoint;

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        Basketball basketball = rb.GetComponent<Basketball>();
        if (rb != null && basketball != null && !basketball.enteredTop)
        {
            Vector3 direction = (targetPoint.position - other.transform.position).normalized;
            float distance = Vector3.Distance(targetPoint.position, other.transform.position);
            float assist = pullStrength / Mathf.Max(distance, 0.1f); // Stronger when closer
            rb.AddForce(direction * assist, ForceMode.Acceleration);
        }
    }
}
