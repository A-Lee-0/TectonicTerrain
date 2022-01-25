using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
    }
}

namespace GlobeLines
{
    public class LineDrawer
    {
        public Mesh mesh;
        public int lineSegments;
        public float width;
        public Color color;
        public Shader shader = Shader.Find("Particles/Standard Surface");

        public LineDrawer(Mesh mesh, int lineSegments, float width, Color color) {
            this.mesh = mesh;
            this.lineSegments = lineSegments;
            this.width = width;
            this.color = color;
        }

        public static LineDrawer GlobeCircle(Vector3 centre, float radius, float lineWidth, Color color, Planet planet, int lineSegments = 30, float height = 0.01f) {
            Mesh newMesh = new Mesh();

            Vector3[] vertices = new Vector3[lineSegments * 2];
            int[] triangles = new int[lineSegments * 2 * 3];
            int triIndex = 0;
            Color[] pointColors = new Color[vertices.Length]; 
            for (int i = 0; i < pointColors.Length; i++) { pointColors[i] = color; }


            Vector3 localUp;
            if(centre.normalized == Vector3.up) { localUp = Vector3.back; }
            else { localUp = Vector3.up; }

            float angularWidth = (lineWidth / planet.radius) * (180 / Mathf.PI);
            float radialAngle = (radius / planet.radius) * (180 / Mathf.PI);
            Vector3 p_outer = Quaternion.AngleAxis(radialAngle + angularWidth / 2, Vector3.Cross(centre, localUp).normalized) * centre.normalized * (planet.radius + height);
            Vector3 p_inner = Quaternion.AngleAxis(radialAngle - angularWidth / 2, Vector3.Cross(centre, localUp).normalized) * centre.normalized * (planet.radius + height);
            Debug.Log("vector magnitudes: " + p_outer.magnitude + ", " + p_inner.magnitude);
            Quaternion nextSegment = Quaternion.AngleAxis(-360 / lineSegments, centre);


            for (int i = 0; i < lineSegments - 1; i++) {
                // Add vertices outside in, then proceed clockwise around the circle. 
                vertices[2 * i] = p_outer;
                vertices[2 * i + 1] = p_inner;
                p_outer = nextSegment * p_outer;
                p_inner = nextSegment * p_inner;
                //Debug.Log("vector magnitudes: " + p_outer.magnitude + ", " + p_inner.magnitude);

                //tri 1
                triangles[triIndex] = 2 * i;
                triangles[triIndex + 1] = 2 * i + 1;
                triangles[triIndex + 2] = 2 * i + 2;

                //tri 2
                triangles[triIndex + 3] = 2 * i + 1;
                triangles[triIndex + 4] = 2 * i + 3;
                triangles[triIndex + 5] = 2 * i + 2;

                triIndex += 6;


            }
            //do final points.
            int j = lineSegments - 1;
            vertices[2 * j] = p_outer;
            vertices[2 * j + 1] = p_inner;
            
            //tri 1
            triangles[triIndex] = 2 * j;
            triangles[triIndex + 1] = 2 * j + 1;
            triangles[triIndex + 2] = 0;

            //tri 2
            triangles[triIndex + 3] = 2 * j + 1;
            triangles[triIndex + 4] = 1;
            triangles[triIndex + 5] = 0;

            newMesh.Clear();
            newMesh.vertices = vertices;
            newMesh.triangles = triangles;
            //mesh.RecalculateNormals();
            newMesh.normals = vertices;
            newMesh.colors = pointColors;


            return new LineDrawer(newMesh, lineSegments, lineWidth, color);
        }

    }


}
