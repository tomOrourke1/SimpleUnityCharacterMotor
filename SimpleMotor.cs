using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEngine;



public class SimpleMotor : MonoBehaviour
{
    int maxBounces = 5;
    float skinWidth = 0.015f;
    float maxSlopeAngle = 55;

    Bounds bounds;
    [SerializeField] CapsuleCollider collider;

    bool isGrounded = false;
    [SerializeField]
    LayerMask layerMask;

    [Space]
    [SerializeField] Vector3 colliderOffset;


    [SerializeField]
    Vector3 upNormal = Vector3.up;

    Vector3 point1;
    Vector3 point2;


    Vector3 lastHitNormal = Vector3.zero;

    RaycastHit lastHit;

    public Vector3 Normal => lastHitNormal;
    public Vector3 UpDir => upNormal;


    float Radius => collider.bounds.extents.x;
    float BRadius => bounds.extents.x;

    public bool Grounded
    {
        get { return isGrounded; }
        set { isGrounded = value; }
    }

    public RaycastHit LastHit => lastHit;

    private void Awake()
    {
        SetColliderPoints();
    }


    void SetColliderPoints(Vector3 offset = default)
    {
        float x = ((collider.height / 2) - Radius);
        upNormal.Normalize();
        point1 = collider.center - upNormal * x/* new Vector3(0, x, 0)*/ + transform.position + offset;
        point2 = collider.center + upNormal * x /*new Vector3(0, x, 0)*/ + transform.position + offset;
    }

    private void Update()
    {
        bounds = collider.bounds;
        bounds.Expand(-2 * skinWidth);
    }

    private void OnDrawGizmos()
    {
    //    Gizmos.DrawWireSphere(point1, collider.bounds.extents.x);
    //    Gizmos.DrawWireSphere(point2, collider.bounds.extents.x);


   //     Gizmos.color = Color.red;
    //    Gizmos.DrawLine(lastHit.point, lastHit.point + lastHitNormal);
    }


    public void Move(Vector3 amount, float gravity)
    {
        amount = CollideAndSlide(amount, transform.position, 0, false, amount);
        var grav = upNormal * gravity;
        amount += CollideAndSlide(grav, transform.position + amount, 0, true, grav);

        transform.position += amount;
    }

    public void MoveMK2(Vector3 amount)
    {
        transform.position += CollideAndSlideMK2(amount, transform.position, 0, amount);
    }

    public Vector3 MoveMK3(Vector3 velocity, float deltaTime)
    {
        Vector3 vel = velocity * deltaTime;
        Vector3 delta = CollideAndSlideMK3(vel, transform.position, 0, vel);
        transform.position += delta;

        velocity = ProjectAndScale(velocity, lastHitNormal);

        return velocity;
    }
    


    private Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit)
    {
        if (depth >= maxBounces)
        {
            return Vector3.zero;
        }

        CheckGrounded();

        float dist = vel.magnitude + skinWidth;
        RaycastHit hit;

        SetColliderPoints();
        bool doHit = Physics.CapsuleCast(point1, point2, BRadius, vel.normalized, out hit, dist, layerMask);

       // bool doHit = Physics.SphereCast(pos + colliderOffset, bounds.extents.x, vel.normalized, out hit, dist, layerMask);

        if (
            doHit
            )
        {
            lastHitNormal = hit.normal;
            lastHit = hit;

            Vector3 snapToSurface = vel.normalized * (hit.distance - skinWidth);
            Vector3 leftover = vel - snapToSurface;

            // needs to check opositite of down against the normal
            float angle = Vector3.Angle(upNormal, hit.normal);

            if (snapToSurface.magnitude <= skinWidth)
            {
                snapToSurface = Vector3.zero;
            }

            //normal gound / slope
            if (angle <= maxSlopeAngle)
            {
                if (gravityPass)
                {
                    return snapToSurface;
                }
                leftover = ProjectAndScale(leftover, hit.normal);
            }
            // wall or steep slope
            else
            {
                float scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(velInit.x, 0, velInit.z).normalized);


                if (isGrounded && !gravityPass)
                {
                    leftover = ProjectAndScale(
                        new Vector3(leftover.x, 0, leftover.z),
                        new Vector3(hit.normal.x, 0, hit.normal.z)
                        );
                    leftover *= scale;
                }
                else
                {
                    leftover = ProjectAndScale(leftover, hit.normal) * scale;
                }
            }

            return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth + 1, gravityPass, velInit);

        }

        return vel;
    }

    private Vector3 CollideAndSlideMK2(Vector3 vel, Vector3 pos, int depth, Vector3 velInit)
    {
        if (depth >= maxBounces)
        {
            return Vector3.zero;
        }

        float dist = vel.magnitude + skinWidth;
        RaycastHit hit;

        SetColliderPoints();
        bool doHit = Physics.CapsuleCast(point1, point2, BRadius, vel.normalized, out hit, dist, layerMask);

        // bool doHit = Physics.SphereCast(pos + colliderOffset, bounds.extents.x, vel.normalized, out hit, dist, layerMask);

        if (doHit)
        {
            lastHitNormal = hit.normal;
            lastHit = hit;

            Vector3 snapToSurface = vel.normalized * (hit.distance - skinWidth);
            Vector3 leftover = vel - snapToSurface;

            // needs to check opositite of down against the normal
            float angle = Vector3.Angle(upNormal, hit.normal);

            if (snapToSurface.magnitude <= skinWidth)
            {
                snapToSurface = Vector3.zero;
            }

            //normal gound / slope
            if (angle <= maxSlopeAngle)
            {
                leftover = ProjectAndScale(leftover, hit.normal);
            }
            // wall or steep slope
            else
            {
                // this used to have removed the y axis
                float scale = 1 - Vector3.Dot(
                    hit.normal.normalized/*new Vector3(hit.normal.x, 0, hit.normal.z).normalized*/,
                    -velInit.normalized/*-new Vector3(velInit.x, 0, velInit.z).normalized*/
                    );


                if (isGrounded)
                {
                    // this also used to have y of 0
                    leftover = ProjectAndScale(
                        leftover/*new Vector3(leftover.x, 0, leftover.z)*/,
                        hit.normal /*new Vector3(hit.normal.x, 0, hit.normal.z)*/
                        );
                    leftover *= scale;
                }
                else
                {
                    leftover = ProjectAndScale(leftover, hit.normal) * scale;
                }
            }

            return snapToSurface + CollideAndSlideMK2(leftover, pos + snapToSurface, depth + 1, velInit);

        }
        else // doesn't hit anything
        {
            lastHit = default;
            isGrounded = false;
            lastHitNormal = Vector3.zero;

        }

        return vel;
    }


    private Vector3 CollideAndSlideMK3(Vector3 vel, Vector3 pos, int depth, Vector3 velInit)
    {
        if (depth >= maxBounces)
        {
            return Vector3.zero;
        }

        float dist = vel.magnitude + skinWidth;
        RaycastHit hit;

        SetColliderPoints();
        bool doHit = Physics.CapsuleCast(point1, point2, BRadius, vel.normalized, out hit, dist, layerMask);

        // bool doHit = Physics.SphereCast(pos + colliderOffset, bounds.extents.x, vel.normalized, out hit, dist, layerMask);

        if (doHit)
        {
            lastHitNormal = hit.normal;
            lastHit = hit;


            Vector3 snapToSurface = vel.normalized * (hit.distance - skinWidth);
            Vector3 leftover = vel - snapToSurface;

            // needs to check opositite of down against the normal
            float angle = Vector3.Angle(upNormal, hit.normal);

            if (snapToSurface.magnitude <= skinWidth)
            {
                snapToSurface = Vector3.zero;
            }

            //normal gound / slope
            if (angle <= maxSlopeAngle)
            {
                leftover = ProjectAndScale(leftover, hit.normal);
            }
            // wall or steep slope
            else
            {
                // this used to have removed the y axis
                float scale = 1 - Vector3.Dot(
                    hit.normal.normalized/*new Vector3(hit.normal.x, 0, hit.normal.z).normalized*/,
                    -velInit.normalized/*-new Vector3(velInit.x, 0, velInit.z).normalized*/
                    );


                if (isGrounded)
                {
                    // this also used to have y of 0
                    leftover = ProjectAndScale(
                        leftover/*new Vector3(leftover.x, 0, leftover.z)*/,
                        hit.normal /*new Vector3(hit.normal.x, 0, hit.normal.z)*/
                        );
                    leftover *= scale;
                }
                else
                {
                    leftover = ProjectAndScale(leftover, hit.normal) * scale;
                }
            }

            return snapToSurface + CollideAndSlideMK3(leftover, pos + snapToSurface, depth + 1, velInit);

        }
        else // doesn't hit anything
        {
            lastHit = default;
            isGrounded = false;
            lastHitNormal = Vector3.zero;

        }

        return vel;
    }



    private Vector3 TestingMotor(Vector3 vel, Vector3 pos, int depth, Vector3 velInit)
    {
        if(depth >= maxBounces)
        {
            return Vector3.zero;
        }

        SetColliderPoints();
        var check = Physics.CheckCapsule(point1, point2, layerMask);
        var operlapAll = Physics.OverlapCapsule(point1, point2, Radius, layerMask);
        var vastAll = Physics.CapsuleCastAll(point1, point2, BRadius, vel.normalized, vel.magnitude, layerMask);



        return Vector3.zero;
    }


    public void CheckGrounded()
    {
        RaycastHit hit;
        bool hihi = Physics.Raycast(point1, Vector3.down, out hit, Radius + skinWidth);
        //bool doHit = Physics.CapsuleCast(point1, point2, BRadius, Vector3.down, out hit, 0.0001f, layerMask);
        isGrounded = hihi;
    }
    
    Vector3 ProjectAndScale(Vector3 p1, Vector3 p2)
    {
        float mag = p1.magnitude;
        p1 = Vector3.ProjectOnPlane(p1, p2).normalized;
        return (p1 * mag);
    }


    /// <summary>
    /// set the gravity direction
    /// will normalize internally
    /// </summary>
    /// <param name="gravNormal"></param>
    public void SetUpDirection(Vector3 gravNormal)
    {
        upNormal = gravNormal.normalized;
    }





}
