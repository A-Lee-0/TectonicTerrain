using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetManager : MonoBehaviour
{

    public Planet planet1;
    public Vector3 P1pos = Vector3.zero;
    public float P1radius = 2f;
    public string P1name = "The Homeworld";

    public List<Planet> planets;


    //temp, remove later!
//    public float radius = 2f;

     
    // Runs when unity compiles the script (i.e. in edit mode, not play)
    private void OnValidate() {
        if (planet1 == null) {
            GameObject p1go = new GameObject(P1name);
            p1go.transform.parent = transform;
            p1go.transform.position = P1pos;
            planet1 = p1go.AddComponent<Planet>();
            planet1.radius = P1radius;
        }
        if (!planets.Contains(planet1)) { planets.Add(planet1); }
        //Initialise();
        //GenerateMesh();
        //GetComponent<SphereCollider>().radius = radius;
    }

    
}