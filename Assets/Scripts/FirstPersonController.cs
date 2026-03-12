using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 5.0f;
    public float gravity = -9.81f;

    [Header("Camera Settings")]
    public Camera playerCamera;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        // Trava o mouse no centro da tela e esconde o cursor (clique ESC para soltar no teste)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- 1. OLHAR (CÂMERA) ---
        // Pega a movimentação do mouse (Eixos X e Y)
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit); // Limita para não dar "cambalhota" com o pescoço
        
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0); // Gira a câmera para cima/baixo
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0); // Gira o corpo do player para os lados

        // --- 2. ANDAR ---
        // Verifica se o personagem está encostando no chão
        if (characterController.isGrounded)
        {
            // Pega o input do teclado (WASD ou Setas)
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            
            float curSpeedX = walkSpeed * Input.GetAxis("Vertical");
            float curSpeedY = walkSpeed * Input.GetAxis("Horizontal");
            
            moveDirection = (forward * curSpeedX) + (right * curSpeedY);
        }

        // --- 3. GRAVIDADE ---
        // Aplica a gravidade constantemente puxando para baixo
        moveDirection.y += gravity * Time.deltaTime;

        // --- 4. APLICAR MOVIMENTO ---
        characterController.Move(moveDirection * Time.deltaTime);
    }
}