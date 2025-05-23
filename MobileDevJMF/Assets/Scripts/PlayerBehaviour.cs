using UnityEngine;

/// <summary>
/// Responsible for moving the player automatically and receiving input.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerBehaviour : MonoBehaviour
{
    /// <summary>
    /// A reference to the Rigidbody component
    /// </summary>
    private Rigidbody rb;

    [Tooltip("How fast the ball moves left/right")]
    public float dodgeSpeed = 5;

    [Tooltip("How fast the ball moves forward automatically")]
    [Range(0, 10)]
    public float rollSpeed = 5;

    /// <summary>
    /// Movement types for horizontal mobile control
    /// </summary>
    public enum MobileHorizMovement
    {
        Accelerometer,
        ScreenTouch
    }

    [Tooltip("What horizontal movement type should be used")]
    public MobileHorizMovement horizMovement = MobileHorizMovement.Accelerometer;

    [Header("Swipe Properties")]
    [Tooltip("How far will the player move upon swiping")]
    public float swipeMove = 2f;

    [Tooltip("How far must the player swipe before we will execute the action (in inches)")]
    public float minSwipeDistance = 0.25f;

    /// <summary>
    /// Used to hold the value that converts minSwipeDistance to pixels
    /// </summary>
    private float minSwipeDistancePixels;

    /// <summary>
    /// Stores the starting position of mobile touch events
    /// </summary>
    private Vector2 touchStart;

    /// <summary>
    /// The current scale of the player
    /// </summary>
    private float currentScale = 1;

    [Header("Scaling Properties")]
    [Tooltip("The minimum size (in Unity units) that the player should be")]
    public float minScale = 0.5f;

    [Tooltip("The maximum size (in Unity units) that the player should be")]
    public float maxScale = 3.0f;

    // Start is called before the first frame update
    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        minSwipeDistancePixels = minSwipeDistance * Screen.dpi;
    }

    void FixedUpdate()
{
    // Default horizontal input
    float horizontalSpeed = Input.GetAxis("Horizontal") * dodgeSpeed;

#if UNITY_STANDALONE || UNITY_EDITOR
    if (Input.GetMouseButton(0))
    {
        Vector3 screenPos = Input.mousePosition;
        horizontalSpeed = CalculateMovement(screenPos);
    }

#elif UNITY_IOS || UNITY_ANDROID
    switch (horizMovement)
    {
        case MobileHorizMovement.Accelerometer:
            // Move player based on accelerometer direction
            horizontalSpeed = Input.acceleration.x * dodgeSpeed;
            break;

        case MobileHorizMovement.ScreenTouch:
            // Check if Input has registered more than zero touches
            if (Input.touchCount > 0)
            {
                // Store the first touch detected
                Touch firstTouch = Input.touches[0];
                Vector3 screenPos = firstTouch.position;
                horizontalSpeed = CalculateMovement(screenPos);
            }
            break;
    }
#endif

    rb.AddForce(horizontalSpeed, 0, rollSpeed);
}


    private float CalculateMovement(Vector3 screenPos)
    {
        Camera cam = Camera.main;
        Vector3 viewPos = cam.ScreenToViewportPoint(screenPos);
        float xMove = viewPos.x < 0.5f ? -1f : 1f;
        return xMove * dodgeSpeed;
    }

   /// <summary>
/// Update is called once per frame
/// </summary>
private void Update()
{
    /* Check if we are running either in the Unity
       editor or in a
     * standalone build.*/
    #if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_EDITOR
    /* If the mouse is tapped */
    if (Input.GetMouseButtonDown(0))
    {
        Vector2 screenPos = new Vector2(
            Input.mousePosition.x,
                Input.mousePosition.y);
        TouchObjects(screenPos);
    }
    /* Check if we are running on a mobile device */
    #elif UNITY_IOS || UNITY_ANDROID
        /* Check if Input has registered more than
           zero touches */
        if (Input.touchCount > 0)
        {
            /* Store the first touch detected */
            Touch touch = Input.touches[0];
            TouchObjects(touch.position);
            SwipeTeleport(touch);
            ScalePlayer();
        }
    #endif
}
    

    private void SwipeTeleport(Touch touch)
    {
        if (touch.phase == TouchPhase.Began)
        {
            touchStart = touch.position;
        }
        else if (touch.phase == TouchPhase.Ended)
        {
            Vector2 touchEnd = touch.position;
            float x = touchEnd.x - touchStart.x;

            if (Mathf.Abs(x) < minSwipeDistancePixels)
            {
                return;
            }

            Vector3 moveDirection = x < 0 ? Vector3.left : Vector3.right;

            RaycastHit hit;
            if (!rb.SweepTest(moveDirection, out hit, swipeMove))
            {
                Vector3 movement = moveDirection * swipeMove;
                Vector3 newPos = rb.position + movement;
                rb.MovePosition(newPos);
            }
        }
    }

    private void ScalePlayer()
    {
        if (Input.touchCount != 2)
        {
            return;
        }

        Touch touch0 = Input.touches[0];
        Touch touch1 = Input.touches[1];

        Vector2 t0Pos = touch0.position;
        Vector2 t1Pos = touch1.position;
        Vector2 t0Delta = touch0.deltaPosition;
        Vector2 t1Delta = touch1.deltaPosition;

        Vector2 t0Prev = t0Pos - t0Delta;
        Vector2 t1Prev = t1Pos - t1Delta;

        float prevTDeltaMag = (t0Prev - t1Prev).magnitude;
        float tDeltaMag = (t0Pos - t1Pos).magnitude;

        float deltaMagDiff = prevTDeltaMag - tDeltaMag;

        float newScale = currentScale;
        newScale -= (deltaMagDiff * Time.deltaTime);
        newScale = Mathf.Clamp(newScale, minScale, maxScale);

        transform.localScale = Vector3.one * newScale;
        currentScale = newScale;
    }

    /// <summary>
/// Will determine if we are touching a game object
/// and if so call events for it
/// </summary>
/// <param name="screenPos">The position of the touch in screen space</param>
private static void TouchObjects(Vector2 screenPos)
{
    // Convert the position into a ray
    Ray touchRay = Camera.main.ScreenPointToRay(screenPos);
    RaycastHit hit;

    // Create a LayerMask that will collide with all possible channels
    int layerMask = ~0;

    // Are we touching an object with a collider?
    if (Physics.Raycast(touchRay, out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore))
    {
        // Call the PlayerTouch function if it exists on a component attached to this object
        hit.transform.SendMessage("PlayerTouch", SendMessageOptions.DontRequireReceiver);
    }
}

/// <summary>
/// Overload: Will determine if we are touching a game object
/// using a Touch input and if so call events for it
/// </summary>
/// <param name="touch">Our touch event</param>
private static void TouchObjects(Touch touch)
{
    // Convert the position into a ray
    Ray touchRay = Camera.main.ScreenPointToRay(touch.position);
    RaycastHit hit;

    // Create a LayerMask that will collide with all possible channels
    int layerMask = ~0;

    // Are we touching an object with a collider?
    if (Physics.Raycast(touchRay, out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore))
    {
        // Call the PlayerTouch function if it exists on a component attached to this object
        hit.transform.SendMessage("PlayerTouch", SendMessageOptions.DontRequireReceiver);
    }
}

}
