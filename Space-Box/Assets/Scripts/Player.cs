﻿using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public Rigidbody rb;                        //Rigidbody of player

    //Control fields that affect how the player handles.
    public bool autoRoll;                       //Boolean if to use auto-roll
    public bool strafe;                         //If strafing is enabled.
    public float maxRotateX;                    //maximum X axis rotation
    public float maxRotateY;                    //maximum Y axis rotation
    public int sensetivityRotateX;              //X axis rotation sensetivity
    public int sensetivityRotateY;              //Y axis rotation sensetivity
    public int rollRate;                        //Rate of roll
    public int maxSpeed;                        //Maximum speed
    public int accelRate;                       //Acceleration strength
    public float deaccelRate;                   //Active deacceleration magnitude
    public float angleDrag;                     //Angular drag
    public float moveDrag;                      //Movement drag

    //Fields for lerping. Player velocity lerps towards the intended direction of movement.
    public float lerpMult;                      //Constant multiplier for lerping.
    private Vector3 inputDir;                   //Direction of input.
    private float lerpRate;                     //Scale (0-1) multiplier for lerping that temporarily shrinks to 0 upon collision.

    //Fields for bounce physics. Player can be very bouncy or not bouncy at all.
    public float bounceMultiplier;              //Multiplier for bounce effect.
    public float bounceCut;                     //Multiplier to reduce subsequent bounces after a bounce.
    public float bounceRaise;                   //Rate at which the bounce multiplier returns to its orignal value.
    public float minBounceMultiplier;           //Minimum value for the bounce multiplier.
    private float maxBounceMultiplier;          //Maximum value for the bounce multiplier.

    // Use this for initialization
    void Start()
    {
        //Init stuff
        inputDir = transform.forward;
        lerpRate = 1;
        maxBounceMultiplier = bounceMultiplier;

        //Set inertia tensor to something that doesn't make the auto roll get all bizarro.
        rb.inertiaTensor = new Vector3(0.2f, 0.2f, 0.2f);
    }

    // Update is called once per frame
    void Update()
    {
        moveYaw();
        movePitch();
        moveRoll();
        moveThrust();
        moveManipulate();

        //Lock cursor.
        if (UnityEngine.Cursor.lockState != CursorLockMode.Locked)
        {
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
    }

    //Left-Right movement
    void moveYaw()
    {
        //Mouse x-axis yaw
        float rotate = Input.GetAxisRaw("Mouse X") * sensetivityRotateX;
        if (rotate > maxRotateX)    //Limit rotation
        {
            rotate = maxRotateX;
        }
        else if (rotate < -maxRotateX)
        {
            rotate = -maxRotateX;
        }
        rb.AddRelativeTorque(Vector3.up * rotate * Time.deltaTime);
    }

    //Up-Down movement
    void movePitch()
    {
        //Mouse y-axis pitch
        float rotate = Input.GetAxisRaw("Mouse Y") * sensetivityRotateY;
        if (rotate > maxRotateY)    //Limit rotation
        {
            rotate = maxRotateY;
        }
        else if (rotate < -maxRotateY)
        {
            rotate = -maxRotateY;
        }
        rb.AddRelativeTorque(Vector3.right * rotate * Time.deltaTime);
    }

    //Roll movement
    void moveRoll()
    {
        if(autoRoll)
        {
            float temp = transform.rotation.eulerAngles.z;

            //Auto roll
            if (temp > 180 && temp < 360)
            {
                rb.AddRelativeTorque(Vector3.forward * Time.deltaTime * rollRate * (360 - temp) / 180);
            }
            if (temp > 0 && temp < 180)
            {
                rb.AddRelativeTorque(-Vector3.forward * Time.deltaTime * rollRate * temp / 180);
            }
        }
        else
        {
            //Key roll
            if (Input.GetKey("q"))
            {
                rb.AddRelativeTorque(Vector3.forward * Time.deltaTime * rollRate);
            }
            if (Input.GetKey("e"))
            {
                rb.AddRelativeTorque(-Vector3.forward * Time.deltaTime * rollRate);
            }
        }
    }

    //Forward-back movement
    void moveThrust()
    {
        //Key input for accelerate and deaccelerate
        if (
            Input.GetKey("w") ||
            (Input.GetKey("a") && strafe) ||
            (Input.GetKey("d") && strafe) &&
            rb.velocity.magnitude < maxSpeed)
        {
            rb.AddForce(inputDir * Time.deltaTime * accelRate);
        }
        else if (Input.GetKey("s"))
        {
            rb.AddForce(-rb.velocity * deaccelRate);
        }
    }

    //Misc movement stuff. Drag, lerp, etc.
    void moveManipulate()
    {
        //Drag
        rb.AddTorque(-rb.angularVelocity * angleDrag * Time.deltaTime);
        rb.AddForce(-rb.velocity * moveDrag * Time.deltaTime);

        //Increase lerprate back to 1f
        lerpRate += .1f * Time.deltaTime;
        if (lerpRate > 1)
            lerpRate = 1f;

        //Lerp velocity to the direction of movement.
        if (strafe && (Input.GetKey("w") || Input.GetKey("d") || Input.GetKey("a")))
        {
            inputDir = Vector3.zero; //Reset lerp direction

            if (Input.GetKey("w"))  //Forward
            {
                inputDir += transform.forward;
            }
            if (Input.GetKey("d"))  //Strafe right
            {
                inputDir += transform.right;
            }
            if (Input.GetKey("a"))  //Strafe left
            {
                inputDir += -transform.right;
            }

            inputDir = inputDir.normalized;   //Normalize lerp direction.
        }
        else if(!strafe)
        {
            inputDir = transform.forward;
        }
        
        rb.velocity = Vector3.Lerp(rb.velocity, inputDir * rb.velocity.magnitude, lerpMult * lerpRate);     //Lerp main towards the velocity vector.

        //Limit velocity to max speed after a single bounce. Prevents chaining bounces to get ludicrous speed.
        if (rb.velocity.magnitude > maxSpeed * bounceMultiplier)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed * bounceMultiplier;
        }

        //Return bouncerate to normal.
        bounceMultiplier += bounceRaise * Time.deltaTime;
        if (bounceMultiplier > maxBounceMultiplier)
            bounceMultiplier = maxBounceMultiplier;
    }

    //Effects that occur upon collision.
    void OnCollisionEnter(Collision collision)
    {
        lerpRate = 0;                       //Set lerping to 0. It'll increase to normal afterwards.
        rb.velocity *= bounceMultiplier;    //Multiply velocity by bounce multiplier.
        
        bounceMultiplier *= bounceCut;      //Cut reduce bounce multiplier.

        //Bounce multiplier does not go below minimum.
        if (bounceMultiplier < minBounceMultiplier)
            bounceMultiplier = minBounceMultiplier;
    }
}