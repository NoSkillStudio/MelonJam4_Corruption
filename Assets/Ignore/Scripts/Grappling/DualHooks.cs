using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DualHooks : MonoBehaviour
{
    [Header("References")]
    public List<LineRenderer> lineRenderers;
    public List<Transform> gunTips;
    public Transform cam;
    public Transform player;
    public LayerMask whatIsGrappleable;
    public PlayerMovementDualSwinging pm;

    [Header("Swinging")]
    private float maxSwingDistance = 25f;
    private List<Vector3> swingPoints;
    private List<SpringJoint> joints;
    private Vector3 swingPoint;
    private SpringJoint joint;

    [Header("Grappling")]
    public float maxGrappleDistance;
    public float grappleDelayTime;
    public float overshootYAxis;

    private bool grapplesActive;

    [Header("Cooldown")]
    public float grapplingCd;
    private float grapplingCdTimer;

    [Header("OdmGear")]
    public Transform orientation;
    public Rigidbody rb;
    public float horizontalThrustForce;
    public float forwardThrustForce;
    public float extendCableSpeed;

    [Header("Prediction")]
    public RaycastHit predictionHits;
    public Transform predictionPoints;
    public float predictionSphereCastRadius;

    [Header("Input")]
    public KeyCode swingKey1 = KeyCode.Mouse0;


    [Header("DualSwinging")]
    public int amountOfSwingPoints = 2;
    public Transform pointAimers;
    private bool swingsActive;

    private void Start()
    {
        ListSetup();
    }

    private void ListSetup()
    {
        joints = new List<SpringJoint>();

        swingsActive = new List<bool>();
        grapplesActive = new List<bool>();

        currentGrapplePositions = new List<Vector3>();

        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            predictionHits.Add(new RaycastHit());
            joints.Add(null);
            swingPoints.Add(Vector3.zero);
            swingsActive.Add(false);
            grapplesActive.Add(false);
            currentGrapplePositions.Add(Vector3.zero);
        }
    }

    private void Update()
    {
        MyInput();
        CheckForSwingPoints();

        if (joints[0] != null || joints[1] != null) OdmGearMovement();
        if (joint != null) OdmGearMovement();

        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void MyInput()
    {
        // starting swings or grapples depends on whether or not shift is pressed
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(swingKey1)) StartGrapple();
            
        }
        else
        {
            if (Input.GetKeyDown(swingKey1)) StartSwing();
            
        }

        // stopping is always possible
        //if (Input.GetKeyUp(swingKey1)) StopGrapple(0);
        //if (Input.GetKeyUp(swingKey2)) StopGrapple(1);
        if (Input.GetKeyUp(swingKey1)) StopSwing();
    }

    private void CheckForSwingPoints()
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            if (swingsActive) { /* Do Nothing */ }
            else
            {
                RaycastHit sphereCastHit;
                Physics.SphereCast(pointAimers.position, predictionSphereCastRadius, pointAimers.forward, 
                                    out sphereCastHit, maxSwingDistance, whatIsGrappleable);

                RaycastHit raycastHit;
                Physics.Raycast(cam.position, cam.forward, 
                                    out raycastHit, maxSwingDistance, whatIsGrappleable);

                Vector3 realHitPoint;

                // Option 1 - Direct Hit
                if (raycastHit.point != Vector3.zero)
                    realHitPoint = raycastHit.point;

                // Option 2 - Indirect (predicted) Hit
                else if (sphereCastHit.point != Vector3.zero)
                    realHitPoint = sphereCastHit.point;

                // Option 3 - Miss
                else
                    realHitPoint = Vector3.zero;

                // realHitPoint found
                if (realHitPoint != Vector3.zero)
                {
                    predictionPoints.gameObject.SetActive(true);
                    predictionPoints.position = realHitPoint;
                }
                // realHitPoint not found
                else
                {
                    predictionPoints.gameObject.SetActive(false);
                }

                predictionHits = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
            }
        }
    }

    #region Swinging

    private void StartSwing()
    {
        // return if predictionHit not found
        if (predictionHits.point == Vector3.zero) return;

        // deactivate active grapple
        CancelActiveGrapples();
        pm.ResetRestrictions();

        pm.swinging = true;
        swingsActive = true;

        swingPoints[swingIndex] = predictionHits[swingIndex].point;
        joints[swingIndex] = player.gameObject.AddComponent<SpringJoint>();
        joints[swingIndex].autoConfigureConnectedAnchor = false;
        joints[swingIndex].connectedAnchor = swingPoints[swingIndex];
        swingPoint = predictionHits.point;
        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;

        float distanceFromPoint = Vector3.Distance(player.position, swingPoint);

        // the distance grapple will try to keep from grapple point. 
        joints[swingIndex].maxDistance = distanceFromPoint * 0.8f;
        joints[swingIndex].minDistance = distanceFromPoint * 0.25f;
        joint.maxDistance = distanceFromPoint * 0.8f;
        joint.minDistance = distanceFromPoint * 0.25f;

        // customize values as you like
        joints[swingIndex].spring = 15f;
        joints[swingIndex].damper = 7f;
        joints[swingIndex].massScale = 4.5f;
        joint.spring = 15f;
        joint.damper = 7f;
        joint.massScale = 4.5f;

        lineRenderers[swingIndex].positionCount = 2;
        currentGrapplePositions[swingIndex] = gunTips[swingIndex].position;
        lineRenderer.positionCount = 2;
        currentGrapplePositions = gunTip.position;
    }

    public void StopSwing()
    {
        pm.swinging = false;

        swingsActive = false;

        Destroy(joints[swingIndex]);
        Destroy(joint);
    }

    #endregion

    #region Grappling

    private void StartGrapple()
    {
        if (grapplingCdTimer > 0) return;

        CancelActiveSwings();
        CancelAllGrapplesExcept();

        // Case 1 - target point found
        if (predictionHits.point != Vector3.zero)
        {
            Invoke(nameof(DelayedFreeze), 0.05f);

            grapplesActive = true;

            swingPoint = predictionHits.point;

            StartCoroutine(ExecuteGrapple());
        }

        // Case 2 - target point not found
        else
        {
            swingPoint = cam.position + cam.forward * maxGrappleDistance;

            StartCoroutine(StopGrapple(grappleDelayTime));
        }

        lineRenderers[grappleIndex].positionCount = 2;
        currentGrapplePositions[grappleIndex] = gunTips[grappleIndex].position;
        lineRenderer.positionCount = 2;
        currentGrapplePositions = gunTip.position;
    }

    private void DelayedFreeze()
    {
        pm.freeze = true;
    }

    private IEnumerator ExecuteGrapple()
    {
        yield return new WaitForSeconds(grappleDelayTime);

        pm.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grapplePointRelativeYPos = swingPoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        pm.JumpToPosition(swingPoint, highestPointOnArc);
    }

    public IEnumerator StopGrapple(float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        pm.freeze = false;

        pm.ResetRestrictions();

        grapplesActive = false;

        grapplingCdTimer = grapplingCd;
    }

    #endregion

    #region OdmGear

    private Vector3 pullPoint;
    private void OdmGearMovement()
    {
        
        
        // get midpoint if both swing points are active
        if (swingsActive)
        {
            Vector3 dirToGrapplePoint1 = swingPoint - swingPoint;
            pullPoint = swingPoint + dirToGrapplePoint1 * 0.5f;
        }

        // right
        if (Input.GetKey(KeyCode.D)) rb.AddForce(orientation.right * horizontalThrustForce * Time.deltaTime);
        // left
        if (Input.GetKey(KeyCode.A)) rb.AddForce(-orientation.right * horizontalThrustForce * Time.deltaTime);
        // forward
        if (Input.GetKey(KeyCode.W)) rb.AddForce(orientation.forward * forwardThrustForce * Time.deltaTime);
        // backward
        /// if (Input.GetKey(KeyCode.S)) rb.AddForce(-orientation.forward * forwardThrustForce * Time.deltaTime);
        // shorten cable
        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 directionToPoint = pullPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * Time.deltaTime);

            // calculate the distance to the grapplePoint
            float distanceFromPoint = Vector3.Distance(transform.position, pullPoint);

            // the distance grapple will try to keep from grapple point.
            UpdateJoints(distanceFromPoint);

            print("shorten " + Time.time);
        }
        // extend cable
        if (Input.GetKey(KeyCode.S))
        {
            // calculate the distance to the grapplePoint
            float extendedDistanceFromPoint = Vector3.Distance(transform.position, pullPoint) + extendCableSpeed;

            // the distance grapple will try to keep from grapple point.
            UpdateJoints(extendedDistanceFromPoint);

            print("extend " + Time.time);
        }
    }

    private void UpdateJoints(float distanceFromPoint)
    {
        for (int i = 0; i < joints.Count; i++)
        {
            if (joints[i] != null)
            {
                joints[i].maxDistance = distanceFromPoint * 0.8f;
                joints[i].minDistance = distanceFromPoint * 0.25f;
            }
        }
   
                joint.maxDistance = distanceFromPoint * 0.8f;
                joint.minDistance = distanceFromPoint * 0.25f;
            
        
    }

    #endregion

    #region CancleAbilities

    public void CancelActiveGrapples()
    {
        StartCoroutine(StopGrapple(0));
        StartCoroutine(StopGrapple(1));
    }

    private void CancelAllGrapplesExcept()
    {      
             StartCoroutine(StopGrapple());
    }

    private void CancelActiveSwings()
    {
        StopSwing();
    }

    #endregion

    #region Visualisation

    private Vector3 currentGrapplePositions;

    private void DrawRope()
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            // if not grappling, don't draw rope
            if (!grapplesActive && !swingsActive) 
            {
                lineRenderers[i].positionCount = 0;
            }
            else
            {
                currentGrapplePositions = Vector3.Lerp(currentGrapplePositions, swingPoint, Time.deltaTime * 8f);

                lineRenderers[i].SetPosition(0, gunTips[i].position);
                lineRenderers[i].SetPosition(1, currentGrapplePositions[i]);
                lineRenderer.SetPosition(0, gunTip.position);
                lineRenderer.SetPosition(1, currentGrapplePositions);
            }
        }
    }

    #endregion
}
