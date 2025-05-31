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

    /// <summary>
    /// FixedUpdate is a prime place to put physics
    /// calculations happening over a period of time.
    /// </summary>
    void FixedUpdate()
    {
        /* If the game is paused, don't do anything */
        if (PauseScreenBehaviour.paused)
        {
            return;
        }

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
                horizontalSpeed = Input.acceleration.x * dodgeSpeed;
                break;

            case MobileHorizMovement.ScreenTouch:
                if (Input.touchCount > 0)
                {
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
        /* Using Keyboard/Controller to toggle pause menu */
        if (Input.GetButtonDown("Cancel"))
        {
            var pauseBehaviour = GameObject.FindObjectOfType<PauseScreenBehaviour>();
            pauseBehaviour.SetPauseMenu(!PauseScreenBehaviour.paused);
        }

        /* If the game is paused, don't do anything */
        if (PauseScreenBehaviour.paused)
        {
            return;
        }

#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 screenPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            TouchObjects(screenPos);
        }

#elif UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount > 0)
        {
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

    private static void TouchObjects(Vector2 screenPos)
    {
        Ray touchRay = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;

        int layerMask = ~0;

        if (Physics.Raycast(touchRay, out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore))
        {
            hit.transform.SendMessage("PlayerTouch", SendMessageOptions.DontRequireReceiver);
        }
    }

    private static void TouchObjects(Touch touch)
    {
        Ray touchRay = Camera.main.ScreenPointToRay(touch.position);
        RaycastHit hit;

        int layerMask = ~0;

        if (Physics.Raycast(touchRay, out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore))
        {
            hit.transform.SendMessage("PlayerTouch", SendMessageOptions.DontRequireReceiver);
        }
    }
}
