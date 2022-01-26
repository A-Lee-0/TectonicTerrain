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

        public static LineDrawer GlobeCircle(Vector3 centre, float radius, float lineWidth, Color color, Planet planet, int lineSegments = 30, float height = 0.013f) {
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


        /// <summary>
        /// Converts an angle in Degrees to an angle in Radians.
        /// </summary>
        /// <param name="angleInDegrees"></param>
        /// <returns></returns>
        public static float Rad(float angleInDegrees) { return angleInDegrees * Mathf.PI / 180f; }

        /// <summary>
        /// Converts an angle in Radians to an angle in Degrees.
        /// </summary>
        /// <param name="angleInRadians"></param>
        /// <returns></returns>
        public static float Deg(float angleInRadians) { return angleInRadians * 180f / Mathf.PI; }

        /// <summary>
        /// Calculates the 3D Moment for a uniform density polygon on the surface of a sphere.
        /// Based on Don Hatch's answer here: https://stackoverflow.com/questions/19897187/locating-the-centroid-center-of-mass-of-spherical-polygons
        /// This will be in the same direction as the 3D CoM, but differs by a factor of the area of the polygon.
        /// 3D CoM = Moment / area
        /// </summary>
        /// <param name="vertices"> An array of position vectors for the polygon's corners. Should work for non-convex, but untested.</param>
        /// <returns></returns>
        public static Vector3 PolygonMoment(Vector3[] vertices) {
            Vector3 moment = Vector3.zero;

            Vector3 vert1;
            Vector3 vert2;

            for (int i = 0; i < vertices.Length; i++) {
                vert1 = vertices[i];
                vert2 = vertices[(i + 1) % vertices.Length];

                moment += Vector3.Cross(vert1, vert2).normalized * Rad( Vector3.Angle(vert1, vert2) ) / 2;
            }

            return -moment;
        }

        public static float Area(Vector3[] vertices, Planet planet) {
            // check my paper notes or programming notes photo album for the derivation.
            if (vertices.Length < 3) { return 0f; }

            float internalAngles = 0f;
            float R = planet.radius;
            int N = vertices.Length;
            float angle;

            for (int i = 0; i < vertices.Length; i++) {
                Vector3 a = vertices[(i + 1) % vertices.Length];
                Vector3 b = vertices[(i) % vertices.Length];
                Vector3 c = vertices[(i + 2) % vertices.Length];

                b *= a.magnitude * a.magnitude / Vector3.Dot(a, b);
                c *= a.magnitude * a.magnitude / Vector3.Dot(a, c);

                b -= a; // b.Normalize();
                c -= a; // c.Normalize();

                angle = Vector3.Angle(b, c);
                Debug.Log("Angle for vertex " + i + " of cell is: " + angle + " deg.");
                internalAngles += Rad(Vector3.Angle(b, c));
            }


            return R * R * (internalAngles - Mathf.PI * (N - 2));
        }

    }







}
