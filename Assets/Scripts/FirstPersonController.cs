using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 5.0f;
    public float gravity = -9.81f;

    [Header("Jump Settings")]
    public float jumpHeight = 1.5f;

    [Header("Camera Settings")]
    public Camera playerCamera;
    public float lookSpeed = 2.0f;
    [Tooltip("Limite vertical da camera. 90 graus permite olhar totalmente para cima e para baixo.")]
    public float lookXLimit = 90.0f;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (GameplayState.Instancia != null && GameplayState.Instancia.EstaBloqueado)
            return;

        // --- 1. LOOK WITH THE CAMERA ---
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        playerCamera.transform.localRotation =
            Quaternion.Euler(rotationX, 0f, 0f);

        transform.rotation *= Quaternion.Euler(
            0f,
            Input.GetAxis("Mouse X") * lookSpeed,
            0f
        );

        // --- 2. CHECK WHETHER THE PLAYER IS ON THE GROUND ---
        bool estaNoChao = characterController.isGrounded;

        // Keep the Character Controller touching the ground.
        if (estaNoChao && moveDirection.y < 0f)
        {
            moveDirection.y = -2f;
        }

        // --- 3. WASD MOVEMENT ---
        Vector3 forward =
            transform.TransformDirection(Vector3.forward);

        Vector3 right =
            transform.TransformDirection(Vector3.right);

        float movimentoVertical =
            Input.GetAxis("Vertical");

        float movimentoHorizontal =
            Input.GetAxis("Horizontal");

        Vector3 movimentoNoChao =
            (forward * movimentoVertical) +
            (right * movimentoHorizontal);

        movimentoNoChao *= walkSpeed;

        moveDirection.x = movimentoNoChao.x;
        moveDirection.z = movimentoNoChao.z;

        // --- 4. JUMP ---
        if (estaNoChao && Input.GetKeyDown(KeyCode.Space))
        {
            moveDirection.y = Mathf.Sqrt(
                jumpHeight * -2f * gravity
            );
        }

        // --- 5. GRAVITY ---
        moveDirection.y += gravity * Time.deltaTime;

        // --- 6. APPLY MOVEMENT ---
        characterController.Move(
            moveDirection * Time.deltaTime
        );
    }
}
