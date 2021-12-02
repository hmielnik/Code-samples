using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float walkSpeed = 2;
    public float runSpeed = 6;
    public float gravity = -12;
    public float jumpHeight = 1;
    public float slidingDamageReduction = 0.5f;
    [Range(0, 1)]
    public float airControlPercent;

    public float turnSmoothTime = 0.2f;
    float turnSmoothVelocity;

    public float speedSmoothTime = 0.1f;
    float speedSmoothVelocity;
    float currentSpeed;
    public float velocityY;
    public float timeAfterGroundLeft = 0;
    public float groundLeftSmoothTime = 0.1f;
    public float maxSlopeLimit = 40f;
    float maxVelocity = 0;
    public bool isSliding = false;
    public bool isStationary = false;
    public bool isOnPlatform = false;
    Vector3 slide;

    public Vector3 currentPos;
    public Vector3 jumpMomentPos;

    public Animator animator;
    public Transform cameraT;
    public CharacterController controller;

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        slide = Vector3.up;
    }

    void Update()
    {
        Debug.DrawLine(transform.position, transform.position + slide,Color.red);
        // input
        Vector2 input = InputGlobals.Move;
        Vector2 inputDir = input.normalized;
        bool running = InputGlobals.Run;

        if(!isStationary)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
            if (isOnPlatform && input == Vector2.zero)
            {
                return;
            }
            else
            {
                Move(inputDir, running);
            }
        }

        
        
        // animator
        float animationSpeedPercent = ((running) ? currentSpeed / runSpeed : currentSpeed / walkSpeed * .5f);
        animator.SetFloat("speedPercent", animationSpeedPercent, speedSmoothTime, Time.deltaTime);

    }

    void Move(Vector2 inputDir, bool running)
    {
        if (inputDir != Vector2.zero)
        {
            float targetRotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
            transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, GetModifiedSmoothTime(turnSmoothTime));
        }

        if(!controller.isGrounded)
        {
            timeAfterGroundLeft += Time.deltaTime;
            slide = Vector3.up;
            jumpMomentPos = currentPos;
        }

        float targetSpeed = ((running) ? runSpeed : walkSpeed) * inputDir.magnitude;
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, GetModifiedSmoothTime(speedSmoothTime));
        velocityY += Time.deltaTime * gravity;
        Vector3 velocity = transform.forward * currentSpeed + slide * velocityY ;
        controller.Move(velocity * Time.deltaTime);
        currentSpeed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;

        if (velocityY < maxVelocity)
        {
            maxVelocity = velocityY;
        }

        if (controller.isGrounded)
        {
            // float calc = 0;
            currentPos = transform.position;
            if(currentPos != jumpMomentPos && jumpMomentPos.y > currentPos.y + 8)
            {
                Debug.Log("Skok!" + (jumpMomentPos.y - currentPos.y));
                FoxCharacter.Instance.ReceiveDamage((int) Mathf.Pow(1.2f,(jumpMomentPos.y - currentPos.y) - 10));
                jumpMomentPos = currentPos;
            }
            RaycastHit hit;
            Debug.DrawLine(transform.position + Vector3.up * 0.2f, transform.position + Vector3.down * 10, Color.cyan);
            if (Physics.Raycast(transform.position + Vector3.up*0.2f, Vector3.down, out hit, 10f))
            {
                if (hit.normal != Vector3.up)
                {
                    float angle = Vector3.Angle(Vector3.up, hit.normal);
                    if (Mathf.Abs(angle) > maxSlopeLimit && !isOnPlatform)
                    {
                        Vector3 sl = Vector3.Cross(Vector3.up, hit.normal);
                        slide = -Vector3.Cross(sl, hit.normal);
                        if (isSliding)
                        {
                            maxVelocity = 0;
                        }
                        isSliding = true;
                    }
                    else
                    {
                        slide = Vector3.up;
                        timeAfterGroundLeft = 0;                     
                        velocityY = -0.1f;
                        if (isSliding)
                        {
                            maxVelocity = 0;
                        }
                        isSliding = false;
                    }

                    //Debug.Log(angle);
                }
                else
                {
                    slide = Vector3.up;
                    timeAfterGroundLeft = 0;
                    velocityY = -0.1f;
                    if(isSliding)
                    {
                        maxVelocity = 0;
                    }
                    isSliding = false;
                }
            }
            //calc = Mathf.Abs(maxVelocity / Mathf.Sqrt(-2 * gravity * jumpHeight));
            //if (calc >= 1.7f)
            //{
            //    FoxCharacter.Instance.ReceiveDamage((int)Mathf.Pow(calc / 1.7f, 2));
            //}
            maxVelocity = 0;
        }

    }

    void Jump()
    {
        if ((controller.isGrounded || timeAfterGroundLeft <= groundLeftSmoothTime) && !isSliding)
        {
            timeAfterGroundLeft = groundLeftSmoothTime;
            float jumpVelocity = Mathf.Sqrt(-2 * gravity * jumpHeight);
            velocityY = jumpVelocity;
        }
    }

    float GetModifiedSmoothTime(float smoothTime)
    {
        if (controller.isGrounded)
        {
            return smoothTime;
        }

        if (airControlPercent == 0)
        {
            return float.MaxValue;
        }
        return smoothTime / airControlPercent;
    }
}

