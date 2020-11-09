﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RibCage_Wall_Movement : MonoBehaviour
{
    public Transform FrontObj;
    public Transform PlayerObj;
    public GameObject WeaponObj;
    
    [Header("Jump Variables")]
    public float gravity = 10;
    public float jumpRange = 10;
    public float WallJumpTime = .5f;
    public float jumpInitTime;
    public float ForwardJumpForce, UpwardJumpForce;
    public float deltaGround = .2f;
    public float lerpSpeed = 10f;
    public float jumpAfterTime;

    [Header("Wall Move Variables")]
    public float InitRotateSpeed;
    public float minTimeChange, maxTimeChange;
    public float minWallRotationSpeed, maxWallRotationSpeed;
    public float minTimeWait, maxTimeWait;
    public float minWallSpeed, maxWallSpeed;
    public float WallAcceleration;
    
    [Header("Wall Pounce Variables")]
    public float MinWallCrawlTime;
    public float MaxWallCrawlTime;
    public float WallForwardForce, WallUpwardForce;
    public float WallPounceInitTime;
    public float WallPounceEndTime;
    public float FinishTime;

    public UnityEvent InitEvent, FinishEvent;
    
    private bool checkJump, moving, rotating, isGrounded,Up, Reset;
    private Rigidbody rigidbody;
    private BoxCollider collider;
    private Vector3 surfaceNormal, myNormal, jumpDirection, moveDirection, pounceDirection;
    private float distGround, vertSpeed = 0, currentTime = 0, randomTimeChange, currentTimeChange,
        randomWallRotationSpeed, randomTimeWait, currentTimeWait, randomWallSpeed, currentWallSpeed,
        randomWallCrawlTime, currentWallCrawlTime;    
    private Ray ray;
    private RaycastHit hit;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<BoxCollider>();
        myNormal = transform.up;
        StartCoroutine(GravityForce());

    }

    public void Attack()
 {
     rigidbody.useGravity = false;
     WeaponObj.SetActive(false);     
     myNormal = transform.up;
     rigidbody.freezeRotation = true;
     distGround = collider.bounds.extents.y - collider.center.y;
     InitEvent.Invoke();
     moving = true;
     StartCoroutine(Move());
     checkJump = true;
     StartCoroutine(CheckJump(WallJumpTime));
 }


    private IEnumerator Move()
    {
        yield return new WaitForSeconds(jumpInitTime);
        jumpDirection = transform.forward * ForwardJumpForce + transform.up * UpwardJumpForce;
        rigidbody.AddForce(jumpDirection, ForceMode.Impulse);
        yield return new WaitForSeconds(jumpAfterTime);
        checkJump = false;
        currentTime = 90 / InitRotateSpeed;
        while (currentTime > 0)
        {
            transform.Rotate(0, InitRotateSpeed * Time.deltaTime, 0);
            currentTime -= Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        Vector3 rot = transform.rotation.eulerAngles;
        rot.x = 0;
        transform.rotation = Quaternion.Euler(rot);
        yield return new WaitForFixedUpdate();

        randomTimeChange = Random.Range(minTimeChange, maxTimeChange);
        currentTimeChange = randomTimeChange;
        randomWallRotationSpeed = Random.Range(minWallRotationSpeed, maxWallRotationSpeed);
        randomTimeWait = Random.Range(minTimeWait, maxTimeWait);
        currentTimeWait = randomTimeWait;
        randomWallSpeed = Random.Range(minWallSpeed, maxWallSpeed);
        currentWallSpeed = 0;
        Up = false;
        Reset = false;
        randomWallCrawlTime = Random.Range(MinWallCrawlTime, MaxWallCrawlTime);
        currentWallCrawlTime = 0;

        while (currentWallCrawlTime < randomWallCrawlTime)
        {
            currentWallCrawlTime += Time.deltaTime;
            if (currentWallSpeed < randomWallSpeed)
            {
                currentWallSpeed += Time.deltaTime * WallAcceleration;
            }
            else if (currentWallSpeed > randomWallSpeed)
            {
                currentWallSpeed -= Time.deltaTime * WallAcceleration;
            }

            ray = new Ray(transform.position, -myNormal); // cast ray downwards
            if (Physics.Raycast(ray, out hit))
            {
                // use it to update myNormal and isGrounded
                isGrounded = hit.distance <= distGround + deltaGround;
                surfaceNormal = hit.normal;
            }
            else
            {
                isGrounded = false;
                surfaceNormal = Vector3.up;
            }

            myNormal = Vector3.Lerp(myNormal, surfaceNormal, lerpSpeed * Time.deltaTime);
            Vector3 myForward = Vector3.Cross(transform.right, myNormal);
            Quaternion targetRot = Quaternion.LookRotation(myForward, myNormal);
            if (currentTimeChange > 0)
            {
                if (Up)
                {
                    Vector3 direction = targetRot.eulerAngles;
                    direction.x += randomWallRotationSpeed * Time.deltaTime;
                    targetRot = Quaternion.Euler(direction);
                }
                else
                {
                    Vector3 direction = targetRot.eulerAngles;
                    direction.x += -1 * randomWallRotationSpeed * Time.deltaTime;
                    targetRot = Quaternion.Euler(direction);

                }

                currentTimeChange -= Time.deltaTime;
            }
            else if (currentTimeWait > 0)
            {
                currentTimeWait -= Time.deltaTime;
            }
            else
            {
                float newRandomChange = Random.Range(minTimeChange, maxTimeChange);
                currentTimeChange = randomTimeChange + newRandomChange;
                randomTimeChange = newRandomChange;
                float newRandomWait = Random.Range(minTimeWait, maxTimeWait);
                currentTimeWait = randomTimeWait + newRandomWait;
                randomTimeWait = newRandomWait;
                Up = !Up;
                randomWallSpeed = Random.Range(minWallSpeed, maxWallSpeed);
            }

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, lerpSpeed * Time.deltaTime);
            transform.Translate(0, 0, Time.deltaTime * currentWallSpeed);
            yield return new WaitForFixedUpdate();
        }

        currentTime = ((90 + transform.rotation.eulerAngles.x)%360) / InitRotateSpeed;
        while (currentTime > 0)
        {
            transform.Rotate(0, InitRotateSpeed * Time.deltaTime, 0);
            currentTime -= Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }
        
        yield return new WaitForSeconds(WallPounceInitTime);

        pounceDirection = (PlayerObj.transform.position - transform.position).normalized; 
        yield return new WaitForSeconds(.1f);
        WeaponObj.SetActive(true);
        rigidbody.AddForce(pounceDirection*WallForwardForce, ForceMode.Impulse);
        yield return new WaitForSeconds(.25f);
        checkJump = true;
        StartCoroutine(CheckJump(WallJumpTime));
        yield return new WaitForSeconds(WallPounceEndTime);
        rigidbody.useGravity = true;
        yield return new WaitForSeconds(FinishTime);
        FinishEvent.Invoke();
        WeaponObj.SetActive(false);


    }

    private IEnumerator GravityForce()
    {
        while (true)
        {
            rigidbody.AddForce(-gravity*rigidbody.mass*myNormal, ForceMode.Force);
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator CheckJump(float jumpTime)
    {
        while (checkJump)
        {
            if (Physics.Raycast(FrontObj.transform.position, transform.forward, out hit, jumpRange))
            {
                // wall ahead?
                StartCoroutine(RotateToWall(hit.point, hit.normal, jumpTime)); // yes: jump to the wall
            }
            if (Physics.Raycast(transform.position, -myNormal, out hit)){ // use it to update myNormal and isGrounded
                surfaceNormal = hit.normal;
            }
            else {
                surfaceNormal = Vector3.up; 
            }
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator RotateToWall(Vector3 point, Vector3 normal, float jumpTime)
    {
        rotating = true;
        rigidbody.isKinematic = true;
        Vector3 orgPos = transform.position;
        Quaternion orgRot = transform.rotation;
        Vector3 dstPos = point + (normal * (distGround + 0.5f));
        Vector3 myForward = Vector3.Cross(transform.right, normal);
        Quaternion dstRot = Quaternion.LookRotation(myForward, normal);
        float t = 0f;
        while (t < jumpTime)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(orgPos, dstPos, GeneralFunctions.ConvertRange(0,jumpTime, 0, 1, t));
            transform.rotation = Quaternion.Slerp(orgRot, dstRot, GeneralFunctions.ConvertRange(0,jumpTime, 0, 1, t));
            yield return new WaitForFixedUpdate();
        }
        myNormal = normal;
        rigidbody.isKinematic = false;
        rotating = false;
        checkJump = false;

    }
    
    public bool CheckDestination(Vector3 Dest01, Vector3 Dest02, float offset)
    {
        if ((Dest01.x >= Dest02.x - offset
             && Dest01.x <= Dest02.x + offset)
            &&(Dest01.y >= Dest02.y - offset
               && Dest01.y <= Dest02.y + offset)
            &&(Dest01.z >= Dest02.z - offset
               && Dest01.z <= Dest02.z + offset))
        {
            return true;
        }
        return false;
    }
   
}