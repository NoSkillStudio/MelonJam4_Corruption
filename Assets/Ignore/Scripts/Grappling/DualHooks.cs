using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DualHooks : MonoBehaviour
{
    [Header("References")]
    public LineRenderer lineRenderer;
    public Transform gunTip;
    public Transform cam;
    public Transform player;
    public LayerMask whatIsGrappleable;
    public PlayerMovementDualSwinging pm;

    [Header("Swinging")]
    private float maxSwingDistance = 25f;
    private List<Vector3> swingPoints;
    private SpringJoint joints;

    [Header("Grappling")]
    public float maxGrappleDistance;
    public float grappleDelayTime;
    public float overshootYAxis;

    private List<bool> grapplesActive;

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
    public List<RaycastHit> predictionHits;
    public List<Transform> predictionPoints;
    public float predictionSphereCastRadius;

    [Header("Input")]
    public KeyCode swingKey1 = KeyCode.Mouse0;
    public KeyCode swingKey2 = KeyCode.Mouse1;


    [Header("DualSwinging")]
    public int amountOfSwingPoints = 2;
    public List<Transform> pointAimers;
    private List<bool> swingsActive;

    private void Start()
    {
        ListSetup();
    }

    private void ListSetup()
    {
        predictionHits = new List<RaycastHit>();

        swingPoints = new List<Vector3>();
       

        swingsActive = new List<bool>();
        grapplesActive = new List<bool>();

        currentGrapplePositions = new List<Vector3>();

        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            predictionHits.Add(new RaycastHit());
            
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

        if (joints != null) OdmGearMovement();

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
            if (Input.GetKeyDown(swingKey1)) StartGrapple(0);
            if (Input.GetKeyDown(swingKey2)) StartGrapple(1);
        }
        else
        {
            if (Input.GetKeyDown(swingKey1)) StartSwing(0);
            if (Input.GetKeyDown(swingKey2)) StartSwing(1);
        }

        // stopping is always possible
        //if (Input.GetKeyUp(swingKey1)) StopGrapple(0);
        //if (Input.GetKeyUp(swingKey2)) StopGrapple(1);
        if (Input.GetKeyUp(swingKey1)) StopSwing(0);
        if (Input.GetKeyUp(swingKey2)) StopSwing(1);
    }

    private void CheckForSwingPoints()
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            if (swingsActive[i]) { /* Do Nothing */ }
            else
            {
                RaycastHit sphereCastHit;
                Physics.SphereCast(pointAimers[i].position, predictionSphereCastRadius, pointAimers[i].forward, 
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
                    predictionPoints[i].gameObject.SetActive(true);
                    predictionPoints[i].position = realHitPoint;
                }
                // realHitPoint not found
                else
                {
                    predictionPoints[i].gameObject.SetActive(false);
                }

                predictionHits[i] = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
            }
        }
    }

    #region Swinging

    private void StartSwing(int swingIndex)
    {
        // return if predictionHit not found
        if (predictionHits[swingIndex].point == Vector3.zero) return;

        // deactivate active grapple
        CancelActiveGrapples();
        pm.ResetRestrictions();

        pm.swinging = true;
        swingsActive[swingIndex] = true;

        swingPoints[swingIndex] = predictionHits[swingIndex].point;
        joints = player.gameObject.AddComponent<SpringJoint>();
        joints.autoConfigureConnectedAnchor = false;
        joints.connectedAnchor = swingPoints[swingIndex];

        float distanceFromPoint = Vector3.Distance(player.position, swingPoints[swingIndex]);

        // the distance grapple will try to keep from grapple point. 
        joints.maxDistance = distanceFromPoint * 0.8f;
        joints.minDistance = distanceFromPoint * 0.25f;

        // customize values as you like
        joints.spring = 15f;
        joints.damper = 7f;
        joints.massScale = 4.5f;

        lineRenderer.positionCount = 2;
        currentGrapplePositions[swingIndex] = gunTip.position;
    }

    public void StopSwing(int swingIndex)
    {
        pm.swinging = false;

        swingsActive[swingIndex] = false;

        Destroy(joints);
    }

    #endregion

    #region Grappling

    private void StartGrapple(int grappleIndex)
    {
        if (grapplingCdTimer > 0) return;

        CancelActiveSwings();
        CancelAllGrapplesExcept(grappleIndex);

        // Case 1 - target point found
        if (predictionHits[grappleIndex].point != Vector3.zero)
        {
            Invoke(nameof(DelayedFreeze), 0.05f);

            grapplesActive[grappleIndex] = true;

            swingPoints[grappleIndex] = predictionHits[grappleIndex].point;

            StartCoroutine(ExecuteGrapple(grappleIndex));
        }

        // Case 2 - target point not found
        else
        {
            swingPoints[grappleIndex] = cam.position + cam.forward * maxGrappleDistance;

            StartCoroutine(StopGrapple(grappleIndex, grappleDelayTime));
        }

        lineRenderer.positionCount = 2;
        currentGrapplePositions[grappleIndex] = gunTip.position;
    }

    private void DelayedFreeze()
    {
        pm.freeze = true;
    }

    private IEnumerator ExecuteGrapple(int grappleIndex)
    {
        yield return new WaitForSeconds(grappleDelayTime);

        pm.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grapplePointRelativeYPos = swingPoints[grappleIndex].y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        pm.JumpToPosition(swingPoints[grappleIndex], highestPointOnArc);
    }

    public IEnumerator StopGrapple(int grappleIndex, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        pm.freeze = false;

        pm.ResetRestrictions();

        grapplesActive[grappleIndex] = false;

        grapplingCdTimer = grapplingCd;
    }

    #endregion

    #region OdmGear

    private Vector3 pullPoint;
    private void OdmGearMovement()
    {
        if (swingsActive[0] && !swingsActive[1]) pullPoint = swingPoints[0];
        if (swingsActive[1] && !swingsActive[0]) pullPoint = swingPoints[1];
        // get midpoint if both swing points are active
        if (swingsActive[0] && swingsActive[1])
        {
            Vector3 dirToGrapplePoint1 = swingPoints[1] - swingPoints[0];
            pullPoint = swingPoints[0] + dirToGrapplePoint1 * 0.5f;
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

            //print("shorten " + Time.time);
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
   
                joints.maxDistance = distanceFromPoint * 0.8f;
                joints.minDistance = distanceFromPoint * 0.25f;
            
        
    }

    #endregion

    #region CancleAbilities

    public void CancelActiveGrapples()
    {
        StartCoroutine(StopGrapple(0));
        StartCoroutine(StopGrapple(1));
    }

    private void CancelAllGrapplesExcept(int grappleIndex)
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
            if (i != grappleIndex) StartCoroutine(StopGrapple(i));
    }

    private void CancelActiveSwings()
    {
        StopSwing(0);
        StopSwing(1);
    }

    #endregion

    #region Visualisation

    private List<Vector3> currentGrapplePositions;

    private void DrawRope()
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            // if not grappling, don't draw rope
            if (!grapplesActive[i] && !swingsActive[i]) 
            {
                lineRenderer.positionCount = 0;
            }
            else
            {
                currentGrapplePositions[i] = Vector3.Lerp(currentGrapplePositions[i], swingPoints[i], Time.deltaTime * 8f);

                lineRenderer.SetPosition(0, gunTip.position);
                lineRenderer.SetPosition(1, currentGrapplePositions[i]);
            }
        }
    }

    #endregion
}
