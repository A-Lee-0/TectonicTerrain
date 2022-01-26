using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GlobeLines;

public class TestBed : MonoBehaviour
{

    public Planet focus_planet;

    public Vector3 startPoint = Vector3.up;
    public Vector3 endPoint = Vector3.forward;

    public Camera camera;

    public int lineSegments = 30;
    LineRenderer test_line;
    public LineDrawer test_circle;

    // Runs when unity compiles the script (i.e. in edit mode, not play)
    private void OnValidate() {
        Initialise();
    }

    // Start is called before the first frame update
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit)) {
            Transform objectHit = hit.transform;


            // Do something with the object that was hit by the raycast.
            endPoint = hit.point;
        }

        //LineRenderer test_line = new LineRenderer{loop = false, positionCount = 30,startColor=Color.red,endColor=Color.red};


        //LineRenderer test_line = gameObject.GetComponent<LineRenderer>();
        Vector3[] positions = new Vector3[lineSegments];
        for (int i=0; i< lineSegments; i++) {
            positions[i] = Vector3.Slerp(startPoint*1.01f, endPoint*1.01f, i / 30f);
        }

        
         
    }

    void Initialise() {



        //DebugLineDraw();

        //DebugSphericalCoM();



    }

    void DebugSphericalCoM() {
        Vector3 p1 = Vector3.up + 2 * Vector3.right;
        Vector3 p2 = Vector3.forward;
        Vector3 p3 = Vector3.right;

        float lineThickess = 0.02f;
        float lineHeight = 0.015f;

        p1.Normalize(); p2.Normalize(); p3.Normalize();

        DebugLineDraw("top-fwd", p1, p2, Color.white, lineThickess, lineHeight);
        DebugLineDraw("fwd-right", p2, p3, Color.white, lineThickess, lineHeight);
        DebugLineDraw("right-top", p3, p1, Color.white, lineThickess, lineHeight);

        int[] masses = new int[] { 1, 2, 3 };

        //Debug.Log("Angle between orthogonal is: " + Vector3.Angle(Vector3.up, Vector3.right));

        // Vector3.Angle returns in Degrees!
        float angle12 = Mathf.Deg2Rad * Vector3.Angle(p1, p2);
        float angle23 = Mathf.Deg2Rad * Vector3.Angle(p2, p3);
        float angle31 = Mathf.Deg2Rad * Vector3.Angle(p3, p1);

        float hcos12 = 1f;///Mathf.Cos(angle12 / 2f);
        float hcos23 = 1f;///Mathf.Cos(angle23 / 2f);
        float hcos31 = 1f;///Mathf.Cos(angle31 / 2f);




        Vector3 com1 = Vector3.RotateTowards(p1, p2, angle12 * hcos12 * masses[0] / (masses[0] + masses[1]), 0f);
        Vector3 com2 = Vector3.RotateTowards(p2, p3, angle23 * hcos23 * masses[1] / (masses[1] + masses[2]), 0f);
        Vector3 com3 = Vector3.RotateTowards(p3, p1, angle31 * hcos31 * masses[2] / (masses[2] + masses[0]), 0f);

        DebugLineDraw("top-c2", p1, com2, Color.white, lineThickess, lineHeight);
        DebugLineDraw("fwd-c3", p2, com3, Color.white, lineThickess, lineHeight);
        DebugLineDraw("right-c1", p3, com1, Color.white, lineThickess, lineHeight);
    }


    void DebugLineRenderer() {
        // if no focus_planet, find planet
        if (focus_planet == null) { focus_planet = FindObjectOfType<Planet>(); }

        // if no line renderer on parent, create linerenderer on parent
        if (gameObject.GetComponent<LineRenderer>() == null) { this.test_line = gameObject.AddComponent<LineRenderer>(); }
        else { this.test_line = gameObject.GetComponent<LineRenderer>(); }
        // if line renderer has no points, create points

        startPoint.Normalize();
        endPoint.Normalize();
        startPoint *= focus_planet.radius;
        endPoint *= focus_planet.radius;

        test_line.startColor = Color.red;
        test_line.endColor = Color.red;
        test_line.positionCount = lineSegments;
        test_line.loop = false;
        test_line.widthMultiplier = 0.1f;
        Vector3[] positions = new Vector3[lineSegments];
        for (int i = 0; i < lineSegments; i++) {
            positions[i] = Vector3.Slerp(startPoint * 1.01f, endPoint * 1.01f, i / 30f);
        }
        test_line.SetPositions(positions);
    }

    void DebugLineDraw() {
        // test globeline
        LineDrawer line = LineDrawer.GlobeLine(Vector3.up, Vector3.right, 0.1f, Color.magenta, focus_planet);

        GameObject lineMeshObj;
        MeshFilter lineMeshFilter;
        if (transform.Find("line_mesh") == null) {
            lineMeshObj = new GameObject("line_mesh");         // create new GameObject with name of "planet_mesh"
            lineMeshObj.transform.parent = transform;                       // set the parent to this object}
            lineMeshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(line.shader);
            lineMeshFilter = lineMeshObj.AddComponent<MeshFilter>();
        }
        else {
            lineMeshObj = transform.Find("line_mesh").gameObject;
            lineMeshFilter = lineMeshObj.GetComponent<MeshFilter>();
        }

        lineMeshFilter.sharedMesh = line.mesh;
    }

    void DebugLineDraw(string name, Vector3 start, Vector3 end, Color color, float thickness = 0.1f, float height = 0.01f, int segments = 30) {
        // test globeline
        LineDrawer line = LineDrawer.GlobeLine(start, end, thickness, color, focus_planet, segments, height);

        GameObject lineMeshObj;
        MeshFilter lineMeshFilter;
        if (transform.Find(name) == null) {
            lineMeshObj = new GameObject(name);         // create new GameObject with name of "planet_mesh"
            lineMeshObj.transform.parent = transform;                       // set the parent to this object}
            lineMeshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(line.shader);
            lineMeshFilter = lineMeshObj.AddComponent<MeshFilter>();
        }
        else {
            lineMeshObj = transform.Find(name).gameObject;
            lineMeshFilter = lineMeshObj.GetComponent<MeshFilter>();
        }

        lineMeshFilter.sharedMesh = line.mesh;
    }


    void DebugCircleDraw() {
        // test circle
        LineDrawer circle = LineDrawer.GlobeCircle(Vector3.up, 25f, 0.1f, Color.magenta, focus_planet);

        GameObject meshObj;
        MeshFilter meshFilter;
        if (transform.Find("circle_mesh") == null) {
            meshObj = new GameObject("circle_mesh");         // create new GameObject with name of "planet_mesh"
            meshObj.transform.parent = transform;                       // set the parent to this object}
            meshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(circle.shader);
            meshFilter = meshObj.AddComponent<MeshFilter>();
        }
        else {
            meshObj = transform.Find("circle_mesh").gameObject;
            meshFilter = meshObj.GetComponent<MeshFilter>();
        }

        meshFilter.sharedMesh = circle.mesh;
    }
}
