using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerBehaviour : MonoBehaviour
{
    
    private Rigidbody rb;
    [Tooltip("How fast the ball moves left/right")]
    public float dodgeSpeed = 5;
    [Tooltip("How fast the ball moves forward  automatically")]
    [Range(0, 10)]
    public float rollSpeed = 5;
    // Start is called before the first frame update
    public void Start()
    {
        // Get access to our Rigidbody component
        rb = GetComponent<Rigidbody>();
    }
   
    void FixedUpdate()
    {
        // Check if we're moving to the side
        var horizontalSpeed = Input.GetAxis("Horizontal") *
                              dodgeSpeed;
        rb.AddForce(horizontalSpeed, 0, rollSpeed);
    }
}