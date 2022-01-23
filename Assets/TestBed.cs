using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBed : MonoBehaviour
{

    public Planet focus_planet;

    public Vector3 startPoint = Vector3.up;
    public Vector3 endPoint = Vector3.forward;

    public Camera camera;

    public int lineSegments = 30;
    LineRenderer test_line;

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
        test_line.SetPositions(positions);
        
    }

    void Initialise() {
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
}
