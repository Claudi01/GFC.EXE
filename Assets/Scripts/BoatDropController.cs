using UnityEngine;

public class BoatDropController : MonoBehaviour
{
    [SerializeField] private Rigidbody boatRb;
    private bool hasDropped = false;

    private void Awake()
    {
        if (boatRb == null)
            boatRb = GetComponent<Rigidbody>();

        boatRb.useGravity = false;
        boatRb.isKinematic = true;
    }

    public void DropBoat()
    {
        if (hasDropped) return;

        hasDropped = true;
        boatRb.isKinematic = false;
        boatRb.useGravity = true;
    }
}