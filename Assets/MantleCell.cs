using UnityEngine;
using System.Collections;
using System.Linq;
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


    public string DebugName;

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

        this.DebugName = RandomString(10);
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
        if (area < 0f) { Debug.Log("Warning! Negative area!"); }
        return Mathf.Abs(area);
    }


    public static MantleCell MergeCells(MantleCell cell1, MantleCell cell2) {
        Vector3 color1 = new Vector3(cell1.color.r, cell1.color.g, cell1.color.b );
        Vector3 color2 = new Vector3(cell2.color.r, cell2.color.g, cell2.color.b);
        Vector3 colorNew = Vector3.Slerp(color1, color2, cell1.area / (cell1.area + cell2.area));
        Vector3 posNew = Vector3.Slerp(cell1.position, cell2.position, cell1.area / (cell1.area + cell2.area));

        cell1.SetPosition(posNew);
        cell1.color = new Color(colorNew.x, colorNew.y, colorNew.z);
        cell1.SetStrength(Mathf.Max(cell1.strength, cell2.strength));

        return cell1;
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

    
    private static System.Random random = new System.Random();
    public static string RandomString(int length) {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }


}
