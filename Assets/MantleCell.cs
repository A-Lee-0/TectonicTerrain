using UnityEngine;
using System.Collections;


public class MantleCell
{
    float theta;    // planar angle
    float phi;      // elevation
    Vector3 position;    // KEEP SAME AS ABOVE!!!
    public float strength = 10f;
    public float cosθ;
    Planet planet;


    public MantleCell(Vector2 pos, Planet planet, float strength = 10f) {
        Vector3 position = SphericalToCartesian(planet.radius, pos.x, pos.y);
        Initialise(position, planet, strength);
    }

    public MantleCell(Vector3 pos, Planet planet, float strength = 10f) {
        Initialise(pos, planet, strength);
    }

    void Initialise(Vector3 pos, Planet planet, float strength) {
        // Consolidated code for constructors into one place, to ensure uniformity when adding parameters.
        Vector2 polar_pos = CartesianToSpherical(pos.normalized * planet.radius);
        this.theta = polar_pos.x;
        this.phi = polar_pos.y;
        this.position = pos.normalized * planet.radius;
        this.planet = planet;
        SetStrength(strength);

        
    }

    public void SetStrength(float strength) {
        this.strength = strength;

        // for PowerDiagram, makes sense to store the cos of the angle rather than the radius of the circle.
        if (strength > planet.Circumference / 4f) { Debug.Log("Warning: MantleCell circle diameter too large!"); }
        float θᵢ = strength / planet.radius;
        this.cosθ = Mathf.Cos(θᵢ);
    }


    public Planet Planet => this.planet;
    public Vector3 PlanetPosition => this.position;
    public Vector3 WorldPosition => planet.position + this.position;

    public static Vector2 CartesianToSpherical(Vector3 cartCoords) {
        Vector2 polar;
        if (cartCoords.x == 0)
            cartCoords.x = Mathf.Epsilon;
        polar.x = Mathf.Atan(cartCoords.z / cartCoords.x);
        if (cartCoords.x < 0)
            polar.x += Mathf.PI;
        polar.y = Mathf.Asin(cartCoords.y / cartCoords.magnitude);
        return polar;
    }

    public static Vector3 SphericalToCartesian(float radius, float polar, float elevation) {
        Vector3 cartesian;
        float a = radius * Mathf.Cos(elevation);
        cartesian.x = a * Mathf.Cos(polar);
        cartesian.y = radius * Mathf.Sin(elevation);
        cartesian.z = a * Mathf.Sin(polar);
        return cartesian;
    }
}
