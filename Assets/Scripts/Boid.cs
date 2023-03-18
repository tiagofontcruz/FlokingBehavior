using UnityEngine;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine.UIElements;

public class Boid : MonoBehaviour
{

    //Speed parameters
    public float speed = 2f;
    public float rotationSpeed = 5f;
    
    //Distance parameters
    public float neighborDistance = 5f;
    public float separationDistance = 20f;
    
    //Weight parameters
    public float separationWeight = 1.0f;
    public float alignmentWeight = 1.0f;
    public float cohesionWeight = 1.5f;
    public float maxSteerForce = 1f;

    private Rigidbody2D rigidBody;
    private Camera mainCamera;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        float xMin = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, 0)).x;
        float xMax = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;
        float yMin = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, 0)).y;
        float yMax = mainCamera.ScreenToWorldPoint(new Vector3(0, Screen.height, 0)).y;

        //Spawn boids randomly in the scene (z axis is not being used)
        transform.position = new Vector3(Random.Range(xMin, xMax), Random.Range(yMin, yMax), 0);
    }

    void Update()
    {
        List<Boid> boids = Flock.instance.boids;

        //Avoid collisions, match velocities and maintain group cohesion
        Vector2 separation = Separation(boids);        
        Vector2 alignment = Alignment(boids);        
        Vector2 cohesion = Cohesion(boids);
        
        //Calculates the overall steering force by summing the separation, alignment, and cohesion vectors                
        Vector2 steer = separation * separationWeight + alignment * alignmentWeight + cohesion * cohesionWeight;
        
        //Prevents excessive acceleration
        steer = Vector2.ClampMagnitude(steer, maxSteerForce);

        //Adjusts the velocity
        rigidBody.velocity += steer * Time.deltaTime;
        
        //Prevents the boid from moving too fast
        rigidBody.velocity = Vector2.ClampMagnitude(rigidBody.velocity, speed);

        //Checks if the boid is moving
        if (rigidBody.velocity != Vector2.zero)
        {
            //Calculates the angle of rotation needed to orient the boid in the direction of its velocity
            float angle = Mathf.Atan2(rigidBody.velocity.y, rigidBody.velocity.x) * Mathf.Rad2Deg - 90f;
            
            //Rotates the boid to the calculated angle
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        //Wrap-around
        Vector3 pos = transform.position;
        if (pos.x < mainCamera.ScreenToWorldPoint(new Vector3(0, 0, 0)).x)
        {
            pos.x = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;
        }
        if (pos.x > mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x)
        {
            pos.x = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, 0)).x;
        }
        if (pos.y < mainCamera.ScreenToWorldPoint(new Vector3(0, 0, 0)).y)
        {
            pos.y = mainCamera.ScreenToWorldPoint(new Vector3(0, Screen.height, 0)).y;
        }
        if (pos.y > mainCamera.ScreenToWorldPoint(new Vector3(0, Screen.height, 0)).y)
        {
            pos.y = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, 0)).y;
        }
        transform.position = pos;
    }


    Vector2 Separation(List<Boid> boids)
    {
        //Initialize the steering force
        Vector2 steer = Vector2.zero;
        int countNearbyBoids = 0;
                
        foreach (Boid other in boids)
        {
            //Check that the other boid is not the current boid
            if (other != this)
            {
                //Calculate the distance between the two boids
                float distance = Vector2.Distance(transform.position, other.transform.position);

                //Check that the other boid is too close
                if (distance < separationDistance)
                {
                    //Calculate the vector pointing away from the other boid
                    Vector2 difference = transform.position - other.transform.position;

                    //Normalize the vector and scale it by the distance to the other boid
                    difference.Normalize();
                    difference /= distance;
                    
                    //Add the vector to the steering force
                    steer += difference;                    
                    countNearbyBoids++;
                }
            }
        }

        //Calculate the average steering force
        if (countNearbyBoids > 0)
        {
            steer /= countNearbyBoids;
        }

        //Normalize and scale it by the maximum speed and the maximum steering force
        if (steer.magnitude > 0)
        {
            steer.Normalize();
            steer *= speed;
            steer -= rigidBody.velocity;
            steer = Vector2.ClampMagnitude(steer, maxSteerForce);
        }
        return steer;
    }

    Vector2 Alignment(List<Boid> boids)
    {
        //Initialize the average velocity
        Vector2 averageVelocity = Vector2.zero;
        int countNearbyBoids = 0;
                
        foreach (Boid other in boids)
        {
            //Check that the other boid is not the current boid
            if (other != this)
            {
                //Calculate the distance between the two boids
                float distance = Vector2.Distance(transform.position, other.transform.position);
                
                //Check that the other boid is a nearby boid and add its velocity to the average velocity
                if (distance < neighborDistance)
                {
                    averageVelocity += other.rigidBody.velocity;                    
                    countNearbyBoids++;
                }
            }
        }
                
        if (countNearbyBoids > 0)
        {
            //Calculate the average velocity
            averageVelocity /= countNearbyBoids;
            
            //Normalize the average velocity, scale it by the maximum speed and calculate the steering force
            averageVelocity.Normalize();
            averageVelocity *= speed;
            Vector2 steer = averageVelocity - rigidBody.velocity;
            steer = Vector2.ClampMagnitude(steer, maxSteerForce);
            return steer;
        }
        //If there are no nearby boids, return a zero
        else
        {
            return Vector2.zero;
        }
    }

    Vector2 Cohesion(List<Boid> boids)
    {
        //Center of mass of neighboring boids
        Vector3 centerOfMass = Vector2.zero;                
        int countNearbyBoids = 0;
                
        foreach (Boid other in boids)
        {
            //Check that the other boid is not the current boid
            if (other != this)
            {
                //Calculate the distance between the current boid and the neighboring boid
                float distance = Vector2.Distance(transform.position, other.transform.position);

                //Check if the neighbor boid is within the range
                if (distance < neighborDistance)
                {
                    //Add its position to the centerOfMass
                    centerOfMass += other.transform.position;
                    countNearbyBoids++;
                }
            }
        }
                
        if (countNearbyBoids > 0)
        {
            //Calculate the average position of the boids
            centerOfMass /= countNearbyBoids;
            //Return a steering force to move towards the calculated center of mass
            return Seeking(centerOfMass);
        }
        //If there are no nearby boids, return a zero
        else
        {
            return Vector2.zero;
        }
    }

    Vector2 Seeking(Vector3 target)
    {        
        //Takes the object directly to the target
        Vector2 desired = target - transform.position;
                
        //It will dictate the direction of movement but not the speed, given magnitude of 1
        desired.Normalize();
        
        //Multiplies the desired vector by a scalar speed value to set the desired speed of movement
        desired *= speed;

        //Calculates the difference between the desired velocity and the current velocity
        //This is the steering force that will be applied to the object to adjust its velocity
        Vector2 steer = desired - rigidBody.velocity;

        //Limits the magnitude of the steer to a maximum value        
        steer = Vector2.ClampMagnitude(steer, maxSteerForce);
        return steer;
    }
}
