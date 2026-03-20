using UnityEngine;

public class DiarioInteraction : MonoBehaviour
{
    [SerializeField] private BoatDropController boat;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactionHighlight;

    [Header("Look Detection")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float lookDistance = 4f;

    private bool playerNear = false;
    private bool used = false;

    private void Start()
    {
        if (interactionHighlight != null)
            interactionHighlight.SetActive(false);

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerNear = false;

        if (interactionHighlight != null)
            interactionHighlight.SetActive(false);
    }

    private void Update()
    {
        if (used) return;

        bool lookingAtDiary = false;

        if (playerNear && playerCamera != null)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, lookDistance))
            {
                if (hit.collider != null && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
                {
                    lookingAtDiary = true;
                }
            }
        }

        if (interactionHighlight != null)
            interactionHighlight.SetActive(lookingAtDiary);

        if (playerNear && lookingAtDiary && Input.GetKeyDown(interactKey))
        {
            used = true;

            if (boat != null)
                boat.DropBoat();

            if (interactionHighlight != null)
                interactionHighlight.SetActive(false);

            gameObject.SetActive(false);
        }
    }
}