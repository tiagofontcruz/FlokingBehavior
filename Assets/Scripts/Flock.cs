using UnityEngine;
using System.Collections.Generic;

public class Flock : MonoBehaviour
{
    //Static reference to be be accessed from Boid script
    public static Flock instance;

    //Boid prefab that will be instantiated
    public GameObject boidPrefab;

    //Number of boids in the scene
    public int numBoids = 100;

    //Boid's array
    public List<Boid> boids;

    //Minimum and maximum values for the random boid's positions in the scene
    public float minX = -50.0f;
    public float maxX = 50.0f;
    public float minY = -50.0f;
    public float maxY = 50.0f;

    void Start()
    {
        //Set the instance of the BoidManager
        instance = this;

        //Initialize the list of boids
        boids = new List<Boid>();

        //Loop through the specified number of boids to create in the scene
        for (int i = 0; i < numBoids; i++)
        {
            //Choose a random position with the specified range
            Vector2 position = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));

            //Random rotation
            Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            //Instantiate a new boid at the random position and random rotation
            GameObject boid = Instantiate(boidPrefab, position, rotation) as GameObject;

            //Set the color of the triangle shape randomly
            SpriteRenderer spriteRenderer = boid.GetComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));

            //Add the Boid component to the list
            boids.Add(boid.GetComponent<Boid>());
        }
    }
}