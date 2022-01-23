using UnityEngine;
using System.Collections;

public class MantleCell
{
    float theta;    // planar angle
    float phi;      // elevation
    public float strength = 10f;
    Planet planet;


    public MantleCell(Vector2 pos, Planet planet, float strength = 10f) {
        this.theta = pos.x;
        this.phi = pos.y;
        this.planet = planet;
        this.strength = strength;
    }

    public MantleCell(Vector3 pos, Planet planet, float strength = 10f) {
        Vector2 polar_pos = CartesianToSpherical(pos - planet.position);
        this.theta = polar_pos.x;
        this.phi = polar_pos.y;
        this.planet = planet; 
        this.strength = strength;
    }



    public Vector3 Position() {
        return SphericalToCartesian(planet.radius, theta, phi);
    }

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
