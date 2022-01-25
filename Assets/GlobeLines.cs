using UnityEngine;
using UnityEditor;


namespace GlobeLines
{
    public class LineDrawer
    {
        public Mesh mesh;
        public int lineSegments;
        public float width;
        public Color color;
        public Shader shader = Shader.Find("Particles/Standard Surface");

        int[] innerPoints;
        int[] outerPoints;
        Vector3[] verts;

        public LineDrawer(Mesh mesh, int lineSegments, float width, Color color, int[] innerPoints, int[] outerPoints, Vector3[] verts) {
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

            int[] innerPoints = new int[lineSegments];
            int[] outerPoints = new int[lineSegments];

            Vector3 localUp;
            if (centre.normalized == Vector3.up) { localUp = Vector3.back; }
            else { localUp = Vector3.up; }

            float angularWidth = (lineWidth / planet.radius) * (180 / Mathf.PI);
            float radialAngle = (radius / planet.radius) * (180 / Mathf.PI);
            Vector3 p_outer = Quaternion.AngleAxis(radialAngle + angularWidth / 2, Vector3.Cross(centre, localUp).normalized) * centre.normalized * (planet.radius + height);
            Vector3 p_inner = Quaternion.AngleAxis(radialAngle - angularWidth / 2, Vector3.Cross(centre, localUp).normalized) * centre.normalized * (planet.radius + height);
            //Debug.Log("vector magnitudes: " + p_outer.magnitude + ", " + p_inner.magnitude);
            Quaternion nextSegment = Quaternion.AngleAxis(-360 / lineSegments, centre);


            for (int i = 0; i < lineSegments - 1; i++) {
                // Add vertices outside in, then proceed clockwise around the circle. 
                vertices[2 * i] = p_outer;
                vertices[2 * i + 1] = p_inner;
                p_outer = nextSegment * p_outer;
                p_inner = nextSegment * p_inner;

                outerPoints[i] = 2 * i;
                innerPoints[i] = 2 * i + 1;

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

            outerPoints[j] = 2 * j;
            innerPoints[j] = 2 * j + 1;

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


            return new LineDrawer(newMesh, lineSegments, lineWidth, color, innerPoints, outerPoints, vertices);
        }


        public static LineDrawer GlobeLine(Vector3 startPoint, Vector3 endPoint, float lineWidth, Color color, Planet planet, int lineSegments = 30, float height = 0.01f) {
            Mesh newMesh = new Mesh();
            Vector3[] vertices = new Vector3[lineSegments * 2 + 2];
            int[] triangles = new int[lineSegments * 2 * 3];
            int triIndex = 0;
            Color[] pointColors = new Color[vertices.Length];
            for (int i = 0; i < pointColors.Length; i++) { pointColors[i] = color; }

            int[] leftPoints = new int[lineSegments + 1];
            int[] rightPoints = new int[lineSegments + 1];

            Vector3 localRight = Vector3.Cross(startPoint,endPoint).normalized;

            float angularLength = Vector3.Angle(startPoint, endPoint); 
            float angularWidth = (lineWidth / planet.radius) * (180 / Mathf.PI);

            Vector3 p_right = Quaternion.AngleAxis(angularWidth / 2, Vector3.Cross(startPoint, localRight)) * startPoint.normalized * (planet.radius + height);
            Vector3 p_left = Quaternion.AngleAxis(-angularWidth / 2, Vector3.Cross(startPoint, localRight)) * startPoint.normalized * (planet.radius + height);

            Quaternion nextSegment = Quaternion.AngleAxis(angularLength / lineSegments, localRight);

            vertices[0] = p_right; rightPoints[0] = 0;
            vertices[1] = p_left; leftPoints[0] = 1;

            p_right = nextSegment * p_right;
            p_left = nextSegment * p_left;

            for (int i = 0; i < lineSegments; i++) {
                vertices[2 * i + 2] = p_right; rightPoints[i + 1] = 2 * i + 2;
                vertices[2 * i + 3] = p_left; leftPoints[i + 1] = 2 * i + 3;

                p_right = nextSegment * p_right;
                p_left = nextSegment * p_left;

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




            newMesh.Clear();
            newMesh.vertices = vertices;
            newMesh.triangles = triangles;
            //mesh.RecalculateNormals();
            newMesh.normals = vertices;
            newMesh.colors = pointColors;


            return new LineDrawer(newMesh, lineSegments, lineWidth, color, leftPoints, rightPoints, vertices);
        }
    }







}
