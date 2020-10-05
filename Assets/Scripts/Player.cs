﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody2D mRigidbody;
    public float jumpSpeed = 5f;
    
    private bool mOnGround = true;
    private bool mJumping = true;
    private float mJumpTimer = 0f;
    private float mJumpGraceTimeTimer = 0f;
    private float mSpeed = 5;
    private float mClimbSpeed = 3;
    private bool mClimbing = false;
    private float mOldGravityScale;
    private float mJumpedFromClimbTimer = 0f;
    private const float JUMP_GRACE_TIME = 0.15f;
    private const float GROUND_CHECK_DIST = 0.015f;

    // phys layers
    // Note, this could also be done with LayerMask.GetMask("UserLayerA", "UserLayerB")) using the names of layers
    private const int PHYS_LAYER_DEFAULT = 0;
    private const int PHYS_LAYER_PLATFORM = 8;
    private const int PHYS_LAYER_CLIMBABLE = 9;
    private const int PHYS_LAYER_CLIMBING = 10;
    // 11 = AnimalNoCollide
    // 12 = FallingFruit
    private const int RAIN_BLOCKING_PLATFORM_PHYS_LAYER = 13;
    // 14 = Rain particles
    private const int PHYS_LAYER_BLOCKING_ENV = 15;
    private const int GROUND_LAYER_MASK = 1 << PHYS_LAYER_PLATFORM | 1 << RAIN_BLOCKING_PLATFORM_PHYS_LAYER;

    // Start is called before the first frame update
    void Start()
    {
        mRigidbody = GetComponent<Rigidbody2D>();
        mOldGravityScale = mRigidbody.gravityScale;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 vel = new Vector2();
        Collider2D collider = GetComponent<Collider2D>();
        float horiz = Input.GetAxis("Horizontal");
        bool sideBlocked = false;
        if(horiz != 0f)
        {
            float direction = Mathf.Sign(horiz);
            vel.x = (direction * mSpeed);
            RaycastHit2D[] sideHits = Physics2D.BoxCastAll(transform.position + new Vector3(direction * 0.95f * collider.bounds.extents.x, 0, 0), collider.bounds.size, 0, vel, GROUND_CHECK_DIST, 1 << PHYS_LAYER_BLOCKING_ENV);
            foreach(RaycastHit2D hit in sideHits)
            {
                if(hit.collider != null && hit.normal.x != 0f && hit.distance <= GROUND_CHECK_DIST && hit.collider != collider)
                {
                    sideBlocked = true;
                    break;
                }
            }
            
            if(sideBlocked)
            {
                Debug.Log("side blocked");
                vel.x = 0;
            }
        }

        float vert = Input.GetAxis("Vertical");        
        {
            Collider2D climbable = Physics2D.OverlapBox(transform.position, collider.bounds.size, 0, 1 << PHYS_LAYER_CLIMBABLE);
            if(climbable != null)
            {
                Vector3 zAlignedPos = new Vector3(transform.position.x, transform.position.y, climbable.bounds.center.z);
                if(!mClimbing)
                {
                    bool initiateClimbing = (mJumpedFromClimbTimer <= 0f) && (climbable.bounds.Contains(zAlignedPos));
                    if(initiateClimbing)
                    {
                        mClimbing = true;
                        gameObject.layer = PHYS_LAYER_CLIMBING;
                        mRigidbody.gravityScale = 0;
                    }
                }
                
                if(mClimbing)
                {
                    vel.y = vert == 0f ? 0f : Mathf.Sign(vert) * mClimbSpeed;
                }
            }
            else
            {
                mClimbing = false;
                gameObject.layer = PHYS_LAYER_DEFAULT;
                mRigidbody.gravityScale = mOldGravityScale;
            }
        }
        
        // jumping
        bool onGround = false;
        if (!mJumping) // Don't even bother with an on-ground check if the player is moving up a because they jumped
        {
            RaycastHit2D[] groundHits = Physics2D.BoxCastAll(transform.position, collider.bounds.size, 0, new Vector2(0, -1), GROUND_CHECK_DIST, GROUND_LAYER_MASK);
            foreach (RaycastHit2D hit in groundHits)
            {
                if (hit.collider != null && hit.normal.y > 0 && hit.distance <= GROUND_CHECK_DIST && hit.collider != collider)
                {
                    onGround = true;
                    break;
                }
            }
        }

        if(mJumpTimer > 0f)
        {
            mJumpTimer -= Time.fixedDeltaTime;
        }

        if(mJumpGraceTimeTimer > 0f)
        {
            mJumpGraceTimeTimer -= Time.fixedDeltaTime;
        }

        if(mOnGround && !onGround)
        {
            // just left the ground
            mJumpGraceTimeTimer = JUMP_GRACE_TIME;
        }

        bool jump = Input.GetButton("Jump") || Input.GetAxis("Vertical") > 0f;
        if(jump && ((mClimbing && horiz != 0f)|| onGround || mJumpGraceTimeTimer > 0f) && mJumpTimer <= 0f)
        {
            mJumping = true;
            vel.y = jumpSpeed;
            mOnGround = false;
            mJumpTimer = 0.2f;

            if(mClimbing)
            {
                mJumpedFromClimbTimer = 0.1f;
                mClimbing = false;
                mRigidbody.gravityScale = mOldGravityScale;
            }
        }
        else if(!mClimbing)
        {
            vel.y = mRigidbody.velocity.y;
        }

        mOnGround = onGround;
        mRigidbody.velocity = vel;
        if (mJumping) { mJumping = (mRigidbody.velocity.y > 0f); } // Stay in "jumping" state until velocity is <= 0f
    }

    private void Update()
    {
        if(mJumpedFromClimbTimer > 0f)
            mJumpedFromClimbTimer -= Time.deltaTime;
    }
}
