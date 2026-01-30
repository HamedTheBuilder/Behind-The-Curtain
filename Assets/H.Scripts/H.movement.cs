using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    //public float moveInput;

    [Header("Jump")]
    public float jumpForce = 10f;
    public float wallJumpUpForce = 15f;
    public float wallJumpSideForce = 10f;
    public float wallJumpTime = 0.2f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    private Rigidbody rb;
    private bool isGrounded;
    private bool isTouchingWall;
    private int wallSide; // 1 = right wall, -1 = left wall
    private float wallJumpTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Important: Freeze rotation and Z position
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationY |
                        RigidbodyConstraints.FreezeRotationZ |
                        RigidbodyConstraints.FreezePositionZ;
    }

    void Update()
    {
              
        CheckCollisions();

        //if (DialougeManagre.Instance.isDialogueActive)
        //{
        //    moveInput = 0f;
        //}
        //else
        //{
        //    moveInput = Input.GetAxisRaw("Horizontal");

        //}

        // Jump input
        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                Jump();
            }
            else if (isTouchingWall)
            {
                WallJump();
            }
        }
    }

    void FixedUpdate()
    {
      float  moveInput = Input.GetAxisRaw("Horizontal");

        // During wall jump, don't allow player control for a short time
        if (wallJumpTimer > 0)
        {
            wallJumpTimer -= Time.fixedDeltaTime;
            return;
        }

        // Normal movement
        rb.linearVelocity = new Vector3(moveInput * moveSpeed, rb.linearVelocity.y, 0);

        // Rotate player
        if (moveInput > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else if (moveInput < 0)
            transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    void CheckCollisions()
    {
        // Ground check - raycast downward
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer);

        // Wall check - raycast left and right
        bool rightWall = Physics.Raycast(transform.position, Vector3.right, 0.7f, wallLayer);
        bool leftWall = Physics.Raycast(transform.position, Vector3.left, 0.7f, wallLayer);

        isTouchingWall = (rightWall || leftWall) && !isGrounded;

        if (rightWall)
            wallSide = 1;
        else if (leftWall)
            wallSide = -1;
    }

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, 0);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void WallJump()
    {
        // Reset velocity completely
        rb.linearVelocity = Vector3.zero;

        // Calculate jump direction (opposite of wall)
        Vector3 jumpDirection = new Vector3(-wallSide * wallJumpSideForce, wallJumpUpForce, 0);

        // Apply force
        rb.AddForce(jumpDirection, ForceMode.Impulse);

        // Set timer to prevent immediate player control
        wallJumpTimer = wallJumpTime;

        // Rotate player to face jump direction
        if (wallSide == 1) // Right wall, jump left
            transform.rotation = Quaternion.Euler(0, 180, 0);
        else // Left wall, jump right
            transform.rotation = Quaternion.Euler(0, 0, 0);

        Debug.Log("Wall Jump! Direction: " + jumpDirection);
    }

    void OnDrawGizmos()
    {
        // Visualize raycasts
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Vector3.down * 1.1f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.right * 0.7f);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, Vector3.left * 0.7f);
    }
}