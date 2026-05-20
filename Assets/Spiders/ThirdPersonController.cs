using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 5f;

    [Header("相机")]
    public Transform cameraPivot;
    public float cameraDistance = 5f;
    public float cameraHeight = 2f;
    public float mouseSensitivity = 3f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("重力")]
    public bool useGravity = true;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Camera mainCam;
    private float yaw = 0f;
    private float pitch = 20f;
    private float verticalVelocity = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCam = Camera.main;

        if (cameraPivot == null)
            cameraPivot = transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleCameraRotation();
        HandleMovement();
    }

    void LateUpdate()
    {
        UpdateCameraPosition();
    }

    void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 基于相机朝向算移动方向
        Vector3 camForward = mainCam.transform.forward;
        Vector3 camRight = mainCam.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * v + camRight * h).normalized;
        Vector3 horizontalMove = moveDir * moveSpeed;

        // 重力
        if (useGravity)
        {
            if (controller.isGrounded && verticalVelocity < 0)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 finalMove = horizontalMove;
        finalMove.y = verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);

        // 注意：这里没有 transform.rotation 的修改，角色 transform 不会转
    }

    void UpdateCameraPosition()
    {
        Quaternion camRot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = camRot * new Vector3(0, 0, -cameraDistance);
        Vector3 targetPos = cameraPivot.position + Vector3.up * cameraHeight + offset;

        mainCam.transform.position = targetPos;
        mainCam.transform.LookAt(cameraPivot.position + Vector3.up * cameraHeight);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}