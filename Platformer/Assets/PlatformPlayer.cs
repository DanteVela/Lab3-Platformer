using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlatformPlayer : MonoBehaviour
{
    // Public references to sprite frames
    public Sprite STANDING;
    public Sprite JUMPING;

    // Movement constants (can make public to tune, just make sure to put the values back in the script)
    private float MAX_SPEED = 6.0f;

    private Vector2 SCREEN_MAX;
    private Vector2 SCREEN_MIN;
    private Vector2 SELF_EXTENTS;

    // Velocity persists frame-to-frame, so store as a class variable
    private Vector2 _velocity = Vector2.zero;

    // ..........................................................................................................

    // Stage 1 Code
    // Platform LayerMask for Raycast
    int platformLayerMask = 1 << 6;

    // Acceleration due to Gravity
    // private Vector2 _acceleration = Vector2.zero;

    // ...........................................................................................................

    void Start()
    {
        // Screen coordinates 0,0 -> 1,1 converted into world coordinates
        SCREEN_MAX = Camera.main.ViewportToWorldPoint(new Vector2(1,1));
        SCREEN_MIN = Camera.main.ViewportToWorldPoint(Vector2.zero);

        // Extents is the half width/height of the sprite in world units
        // (note taking the bounds of the collider rather than the sprite, to avoid inconsistencies with the collision triggers)
        SELF_EXTENTS = GetComponent<BoxCollider2D>().bounds.extents;
    }

    // .................................................................................................................

    // OnTriggerStay2D Collision for Platforms
    private void OnTriggerStay2D(Collider2D platform)
    {
        // Uses trigger stay as opposed to trigger enter/exit

        // Get the bounds of the platform
        Vector2 extents = platform.GetComponent<BoxCollider2D>().bounds.extents;

        // Store the difference in position, we'll need the signs later
        Vector2 dist = platform.transform.position - transform.position;

        // Get the absolute overlap distances (overlaps indicated by negative values)
        Vector2 overlap = new Vector2(Mathf.Abs(dist.x), Mathf.Abs(dist.y)) - (extents + SELF_EXTENTS);

        // Only need to act if both x and y overlap (seperating axis theorem)
        if(overlap.x < 0 && overlap.y < 0) {
            // As approximation, push out along the axis with the smaller overlap distance (bigger negative)
            if(overlap.y > overlap.x) {
                // Less overlap in y, push vertical (use saved sign to know which direction to push)
                transform.position = transform.position + new Vector3(0f, (overlap.y) * Mathf.Sign(dist.y), 0f);

                // Kill vertical velocity
                _velocity = new Vector2(_velocity.x, 0f);
            } else {
                // Less overlap in x, push horizontal
                transform.position = transform.position + new Vector3((overlap.x) * Mathf.Sign(dist.x), 0f, 0f);

                // Kill horizontal velocity
                _velocity = new Vector2(0f, _velocity.y);
            }
        }
    }

    // ................................................................................................................

    // FixedUpdate is called on a fixed time interval, which is better for physics simulation than the variable frame rate
    void FixedUpdate()
    {
        // Acceleration due to Gravity
        Vector2 _acceleration = Vector2.zero;

        // Perform Ground Check w/ Raycast
        RaycastHit2D GroundCheck = Physics2D.Raycast(new Vector2(transform.position.x, 
        (transform.position.y - SELF_EXTENTS.y)), Vector2.down,  0.001f, platformLayerMask);

        // User Input
        Vector2 dir = new Vector2(Input.GetAxisRaw("Horizontal"), 0f);

        // Movement Behavior when on the Ground
        if(GroundCheck.collider != null)
        {
            // Debug.Log("Grounded is Applied");

            // Use Standing Sprite
            transform.GetComponent<SpriteRenderer>().sprite = STANDING;

            // Set facing based on movement direction (flip x-axis scale positive or negative)
            if (dir.x > 0) transform.localScale = (Vector3)new Vector2(1.0f, 1.0f);
            else if (dir.x < 0) transform.localScale = (Vector3)new Vector2(-1.0f, 1.0f);

            // Set constant velocity based on User Input
            // _velocity = dir * MAX_SPEED;

            // ....................................................................................................

            // Debug.Log("Acceleration is Applied");
            // In case we just landed on this frame, zero out vertical velocity
            _velocity.y = 0.0f;

            // Set Acceleration to alter Velocity based on User Input
                // If we have User Input "Left" or "Right", set Constant Acceleration in that direction
            if(Input.GetKey(KeyCode.A))
            {
                _acceleration = new Vector2(-10.00f, 0.0f);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                _acceleration = new Vector2(10.00f, 0.0f);
            }

            // Otherwise (No User Input)
                // If Velocity is really small already, just set to zero to avoid jitter

                // Set Acceleration to the opposite of current Velocity (to Decelerate)
                // (conveniently, if Velocity is 0, so is Acceleration)
            else if(Input.anyKey == false)
            {
                if(_velocity.x < 0.001f)
                {
                    _velocity.x = 0.0f;
                }

                _acceleration = new Vector2(-30.00f, 0.0f);          // -_velocity;

                if(_velocity == Vector2.zero)
                {
                    _acceleration = _velocity;
                }
            }
            // Integrate Acceleration to update Velocity
            _velocity += (_acceleration * Time.deltaTime);

            // Make sure it doesn't go over MAX_SPEED (can use Vector2.ClampMagnitude)
            Vector2.ClampMagnitude(_velocity, MAX_SPEED);

            // Jump using Impulse (add instantaneous Velocity up)
            if(Input.GetKey(KeyCode.Space)) {
                // Debug.Log("Jumping is Applied");

                // Add a vertical component to current Velocity (Vector2.up * a jump_speed constant)
                _velocity += Vector2.up * (20.0f);      // MAX_SPEED * 3

                // Zero out Acceleration (no Horizontal User Control or Deceleration in the air)
                _acceleration = Vector2.zero;
            }
        }
        
        // Movement Behavior when Airborn
        else
        {
            // Debug.Log("Gravity is Applied");

            // Use Jumping Sprite
            transform.GetComponent<SpriteRenderer>().sprite = JUMPING;

            // Acceleration due to Gravity only
            _acceleration = new Vector2(0f, 1.50f);

            // Integrate Acceleration to update Velocity
            _velocity += (Vector2.down * _acceleration);
        }

        // and Integrate Velocity to update Position
        transform.position = (Vector2)transform.position + (_velocity * Time.deltaTime);
    }
}