using UnityEngine;

public class GravityZone : MonoBehaviour
{
    public Vector3 GetGravityDir(Vector3 position)
    {
        // TODO
        return (transform.position - position).normalized;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
            return;

        player.GravityZone = this;
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
            return;

        player.GravityZone = null;
    }
}