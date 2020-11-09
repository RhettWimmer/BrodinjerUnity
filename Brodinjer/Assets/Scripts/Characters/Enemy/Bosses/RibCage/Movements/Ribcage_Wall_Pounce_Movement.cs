﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Character/Enemy/Boss/Ribs/Attack/Wall Pounce")]
public class Ribcage_Wall_Pounce_Movement : Enemy_Attack_Base
{
    [Header("Jump Variables")]
    public float gravity = 10;
    public float jumpRange = 10;
    public float WallJumpTime = .5f;
    public float ForwardJumpForce, UpwardJumpForce;
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
    public float WallPounceForce;
    public float WallPounceInitTime;
    
    
    private bool checkJump, rotating, Up, Reset, calculateGravity;
    private Rigidbody rigidbody;
    private BoxCollider collider;
    private Vector3 surfaceNormal, myNormal, jumpDirection, moveDirection, pounceDirection, rot, myForward, direction, orgPos, dstPos;
    private float distGround, vertSpeed = 0, currentTime = 0, randomTimeChange, currentTimeChange,
        randomWallRotationSpeed, randomTimeWait, currentTimeWait, randomWallSpeed, currentWallSpeed,
        randomWallCrawlTime, currentWallCrawlTime;    
    private Ray ray;
    private RaycastHit hit;
    private Quaternion targetRot, orgRot, dstRot;
    private Coroutine moveFunc, jumpFunc, gravityFunc;
    private bool isGrounded;
    private float deltaGround = .2f;

    public override void Init(MonoBehaviour caller, GameObject MeleeAttack, Transform player, Animator animator, GameObject enemy)
    {
        base.Init(caller, MeleeAttack, player, animator, enemy);
        rigidbody = enemy.GetComponent<Rigidbody>();
        collider = enemy.GetComponent<BoxCollider>();
    }

    public override IEnumerator Attack()
    {
        caller.StartCoroutine(GravityForce());
        checkJump = true;
        caller.StartCoroutine(CheckJump(WallJumpTime));
        distGround = collider.bounds.extents.y - collider.center.y;
        rigidbody.freezeRotation = true;
        myNormal = enemyObj.transform.up;

        yield return new WaitForSeconds(AttackStartTime);
        jumpDirection = enemyObj.transform.forward * ForwardJumpForce + enemyObj.transform.up * UpwardJumpForce;
        rigidbody.AddForce(jumpDirection, ForceMode.Impulse);
        yield return new WaitForSeconds(jumpAfterTime);
        checkJump = false;
        currentTime = 90 / InitRotateSpeed;
        while (currentTime > 0)
        {
            enemyObj.transform.Rotate(0, InitRotateSpeed * Time.deltaTime, 0);
            currentTime -= Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        Vector3 rot = enemyObj.transform.rotation.eulerAngles;
        rot.x = 0;
        enemyObj.transform.rotation = Quaternion.Euler(rot);
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

            ray = new Ray(enemyObj.transform.position, -myNormal); // cast ray downwards
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
            Vector3 myForward = Vector3.Cross(enemyObj.transform.right, myNormal);
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

            enemyObj.transform.rotation = Quaternion.Lerp(enemyObj.transform.rotation, targetRot, lerpSpeed * Time.deltaTime);
            enemyObj.transform.Translate(0, 0, Time.deltaTime * currentWallSpeed);
            yield return new WaitForFixedUpdate();
        }

        currentTime = ((90 + enemyObj.transform.rotation.eulerAngles.x)%360) / InitRotateSpeed;
        while (currentTime > 0)
        {
            enemyObj.transform.Rotate(0, InitRotateSpeed * Time.deltaTime, 0);
            currentTime -= Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }
        
        yield return new WaitForSeconds(WallPounceInitTime);

        pounceDirection = (player.transform.position - enemyObj.transform.position).normalized; 
        yield return new WaitForSeconds(.1f);
        rigidbody.AddForce(pounceDirection*WallPounceForce, ForceMode.Impulse);
        yield return new WaitForSeconds(.25f);
        checkJump = true;
        caller.StartCoroutine(CheckJump(WallJumpTime));
        yield return new WaitForSeconds(CoolDownTime);
        checkJump = false;

    }

    private IEnumerator GravityForce()
    {
        while (true)
        {
            rigidbody.AddForce(-gravity*rigidbody.mass*myNormal);
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator CheckJump(float jumpTime)
    {
        while (checkJump)
        {
            if (Physics.Raycast(enemyObj.transform.position, enemyObj.transform.forward, out hit, jumpRange))
            {
                // wall ahead?
                caller.StartCoroutine(RotateToWall(hit.point, hit.normal, jumpTime)); // yes: jump to the wall
            }
            if (Physics.Raycast(enemyObj.transform.position, -myNormal, out hit)){ // use it to update myNormal and isGrounded
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
        Vector3 orgPos = enemyObj.transform.position;
        Quaternion orgRot = enemyObj.transform.rotation;
        Vector3 dstPos = point + (normal * (distGround + 0.5f));
        Vector3 myForward = Vector3.Cross(enemyObj.transform.right, normal);
        Quaternion dstRot = Quaternion.LookRotation(myForward, normal);
        float t = 0f;
        while (t < jumpTime)
        {
            t += Time.deltaTime;
            enemyObj.transform.position = Vector3.Lerp(orgPos, dstPos, GeneralFunctions.ConvertRange(0,jumpTime, 0, 1, t));
            enemyObj.transform.rotation = Quaternion.Slerp(orgRot, dstRot, GeneralFunctions.ConvertRange(0,jumpTime, 0, 1, t));
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

    public override Enemy_Attack_Base getClone()
    {
        Ribcage_Wall_Pounce_Movement temp = CreateInstance<Ribcage_Wall_Pounce_Movement>();
        temp.gravity = gravity;
        temp.jumpRange = jumpRange;
        temp.WallJumpTime = WallJumpTime;
        temp.ForwardJumpForce = ForwardJumpForce;
        temp.UpwardJumpForce = UpwardJumpForce;
        temp.lerpSpeed = lerpSpeed;
        temp.jumpAfterTime = jumpAfterTime;
        temp.InitRotateSpeed = InitRotateSpeed;
        temp.minTimeChange = minTimeChange;
        temp.maxTimeChange = maxTimeChange;
        temp.minWallRotationSpeed = minWallRotationSpeed;
        temp.maxWallRotationSpeed = maxWallRotationSpeed;
        temp.minTimeWait = minTimeWait;
        temp.maxTimeWait = maxTimeWait;
        temp.minWallSpeed = minWallSpeed;
        temp.maxWallSpeed = maxWallSpeed;
        temp.WallAcceleration = WallAcceleration;
        temp.MinWallCrawlTime = MinWallCrawlTime;
        temp.MaxWallCrawlTime = MaxWallCrawlTime;
        temp.WallPounceForce = WallPounceForce;
        temp.WallPounceInitTime = WallPounceInitTime;
        temp.AttackActiveTime = AttackActiveTime;
        temp.AttackStartTime = AttackStartTime;
        temp.CoolDownTime = CoolDownTime;
        temp.animations = animations;
        temp.attackWhileMoving = attackWhileMoving;
        return temp;
    }
}