using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    [Header("Wallrunning")]
    public LayerMask whatIsGround;
    public LayerMask whatIsWall;
    public float wallRunForce;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
    public float maxWallRunTime;
    private float wallRunTimer;

    [Header("Attachment")]
    public float reattachAngleThreshold = 15;
    private Vector3 lastWallNormal;
    public bool recentlyDetached = false;

    [Header("Input")]
    public KeyCode jumpKey = KeyCode.Space;
    private float horizontalInput;
    private float verticalInput;

    [Header("Gravity")]
    public float gravityCounterForce;

    [Header("Exiting")]
    private bool exitingWall;
    public float exitWallTime;
    private float exitWallTimer;

    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallRight;
    private bool wallLeft;


    [Header("References")]
    public PlayerCam cam;
    public Transform orientation;
    private PlayerMovement pm;
    Rigidbody rb;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckForWall();
        StateMachine();
    }

    void FixedUpdate() {
        if (pm.wallrunning) {
            WallRunningMovement();
        }
    }

    private void CheckForWall() {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround() {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void StateMachine() {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (pm.grounded) {
            recentlyDetached = false;
        }

        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall && !CloseAngle()) {
            //Start wallrunning here
            if (!pm.wallrunning) {
                StartWallRun();
            }
            if (wallRunTimer > 0)
                wallRunTimer -= Time.deltaTime;

            if (wallRunTimer <= 0 && pm.wallrunning) {
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }

            if (Input.GetKeyDown(jumpKey)) {
                WallJump();
            }
        }
        else if (exitingWall) {
            if (pm.wallrunning)
                StopWallRun();

            if (exitWallTimer > 0) {
                exitWallTimer -= Time.deltaTime; 
            }

            if (exitWallTimer <= 0)
                exitingWall = false;
        }
        else {
            if (pm.wallrunning) {
                StopWallRun();
            }
        }
    }

    private void StartWallRun() {
        pm.wallrunning = true;
        wallRunTimer = maxWallRunTime;
        recentlyDetached = false;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        cam.DoFov(90f);
        if (wallLeft) cam.DoTilt(-5f);
        if (wallRight) cam.DoTilt(5f);
    }

    private bool CloseAngle() {
        if (!recentlyDetached)
            return false;
        
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        float angle = Vector3.Angle(lastWallNormal, wallNormal);

        return angle < reattachAngleThreshold;
    }

    private void WallRunningMovement() {
        rb.useGravity = false;


        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        lastWallNormal = wallNormal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude) {
            wallForward = -wallForward;
        }

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if (!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0)) {
            rb.AddForce(-wallNormal * 100, ForceMode.Force);
        }

        // rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
    }
    private void StopWallRun() {
        Debug.Log("Stopping Wall Run" + Time.time);
        rb.useGravity = true;
        pm.wallrunning = false;
        recentlyDetached = true;
        cam.DoFov(80f);
        cam.DoTilt(0f);
    }

    private void WallJump() {
        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}

