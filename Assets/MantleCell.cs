using UnityEngine;
using System.Collections;
//using GlobeLines;


public class MantleCell
{
    float theta;    // planar angle
    float phi;      // elevation
    Vector3 position;    // KEEP SAME AS ABOVE!!!
    public float strength = 10f;
    public float cosθ;
    Planet planet;
    public Color color;

    MantleCellRenderer renderer = null;


    Vector3 centroid;
    float area;

    


    public MantleCell(Vector2 pos, Planet planet, float strength = 10f) {
        Vector3 position = SphericalToCartesian(planet.radius, pos.x, pos.y);
        Initialise(position, planet, strength);
    }

    public MantleCell(Vector3 pos, Planet planet, float strength = 10f) {
        Initialise(pos, planet, strength);
    }

    void Initialise(Vector3 pos, Planet planet, float strength) {
        // Consolidated code for constructors into one place, to ensure uniformity when adding parameters.
        this.planet = planet;
        SetPosition(pos);
        SetStrength(strength);

        
    }




    public Planet Planet => this.planet;
    public Vector3 PlanetPosition => this.position;
    public Vector3 WorldPosition => planet.position + this.position;
    public Vector3[] Vertices => renderer.Vertices;
    public MantleCellRenderer Renderer => renderer;
    public Vector3 Centroid => centroid;
    public float Area => area;

    public bool HasRegion => Vertices.Length > 0;
    public bool HasRenderer => !(renderer == null);

    public void SetRenderer(MantleCellRenderer renderer) {
        this.renderer = renderer;
    }

    public void SetPosition(Vector3 newPosition) {
        position = newPosition.normalized * planet.radius;
        Vector2 polar_pos = CartesianToSpherical(position);
        this.theta = polar_pos.x;
        this.phi = polar_pos.y;
    }

    public void SetStrength(float strength) {
        this.strength = strength;

        // for PowerDiagram, makes sense to store the cos of the angle rather than the radius of the circle.
        if (strength > planet.Circumference / 4f) { Debug.Log("Warning: MantleCell circle diameter too large!"); }
        float θᵢ = strength / planet.radius;
        this.cosθ = Mathf.Cos(θᵢ);
    }


    public Vector3 CalculateCentroid() {
        if (HasRegion) {
            centroid = renderer.Centroid;
        }
        else {
            centroid = Vector3.zero;
        }
        return centroid;
    }

    public float CalculateArea() {
        if (HasRegion) {
            area = renderer.Area;
        }
        else {
            area = 0f;
        }
        return area;
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
