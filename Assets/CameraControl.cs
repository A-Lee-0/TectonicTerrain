using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{

    public Camera camera;

    public Planet focus_planet;

    public float pos_r = 10f;
    public float pos_theta = 90f;
    public float pos_phi = 15f;

    public float vmax_theta = 180f;
    public float vmax_phi = 180f;

    float rmin;

    public float vmax_time = 2f;
    public float stop_time_const = 0.05f;

    public float v_theta;
    public float v_phi;

    Vector2 pos_r_bounds;
    Vector2 pos_theta_bounds;
    Vector2 pos_phi_bounds;
    Vector2 v_theta_bounds;
    Vector2 v_phi_bounds;

    // Runs when unity compiles the script (i.e. in edit mode, not play)
    private void OnValidate() {

    }

    // Start is called before the first frame update
    void Start()
    {
        if (focus_planet == null) { focus_planet = FindObjectOfType<Planet>(); }
        rmin = focus_planet.radius;
        pos_r_bounds = new Vector2(rmin, rmin * 20f);
        pos_theta_bounds = new Vector2(0f, 360f);
        pos_phi_bounds = new Vector2(-89.9f,89.9f);
        v_theta_bounds = new Vector2(-vmax_theta,vmax_theta);
        v_phi_bounds = new Vector2(-vmax_phi,vmax_phi);


    }

    // Update is called once per frame
    void Update()
    {
        float dT = Time.deltaTime;

        float xInput = Input.GetAxis("Horizontal");
        float yInput = Input.GetAxis("Vertical");
        float zInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(xInput) > 0.01f) { v_theta += xInput * vmax_theta * dT / vmax_time; }
        else { v_theta = Mathf.Lerp(v_theta, 0, stop_time_const); }       // Note this is actually going to be an exponential drop rather than going to 0 in stop_time.
        if (Mathf.Abs(yInput) > 0.01f) { v_phi += yInput * vmax_theta * dT / vmax_time; }
        else { v_phi = Mathf.Lerp(v_phi, 0, stop_time_const); }

        pos_r += zInput;


        pos_theta += v_theta * dT;
        pos_theta %= 360f;
        pos_phi += v_phi * dT;

        CheckBounds();

        /*if (pos_phi > 90f) { pos_phi = 90f; }
        if (pos_phi < -90f) { pos_phi = -90f; }
        if (pos_r > 20*rmin) { pos_r = 20 * rmin; }
        if (pos_r < rmin) { pos_r = rmin; }
        if (Mathf.Abs(v_phi) > vmax_phi) { v_phi *= vmax_phi / Mathf.Abs(v_phi); }
        if (Mathf.Abs(v_theta) > vmax_theta) { v_theta *= vmax_theta / Mathf.Abs(v_theta); }*/

        Vector3 camera_pos = Vector3.back * pos_r;
        Quaternion phi_rot = Quaternion.AngleAxis(pos_phi, Vector3.right);
        Quaternion theta_rot = Quaternion.AngleAxis(pos_theta, Vector3.down);           // LHS coordinates in unity.

        camera_pos = theta_rot * phi_rot * camera_pos;
        camera.gameObject.transform.position = camera_pos;
        camera.gameObject.transform.LookAt(Vector3.zero);


    }

    void CheckBounds() {
        pos_r = Bounds(pos_r, pos_r_bounds);
        pos_theta = PeriodicBounds(pos_theta, pos_theta_bounds);
        pos_phi = Bounds(pos_phi, pos_phi_bounds);
        v_theta = Bounds(v_theta, v_theta_bounds);
        v_phi = Bounds(v_phi, v_phi_bounds);
    }

    float Bounds(float arg, Vector2 bounds) {
        if (arg < bounds.x) { arg = bounds.x; }
        if (arg > bounds.y) { arg = bounds.y; }
        return arg;
    }

    float PeriodicBounds(float arg, Vector2 bounds) {
        float bound_range = bounds.y - bounds.x;
        while (arg < bounds.x) { arg += bound_range; }
        while (arg > bounds.y) { arg -= bound_range; }

        return arg;
    }

}
