using UnityEngine;
using UnityEditor;
using System.Linq;

public class SphericalTriangleMesh
{
    public Vector3[] vertices;
    public int resolution;
    public Mesh mesh;

    Vector3[] flatVerts;
    int[] tris;

    public SphericalTriangleMesh(Vector3[] vertices, int resolution) {
        if (vertices.Length < 3) {
            Debug.Log("Error! Insufficient Vertices for a triangle!");
        }
        if (vertices.Length > 3) {
            Debug.Log("Warning! Too many Vertices supplied for Triangle! Only first 3 will be used!");
        }
        
        // Take the first three vertices in the argument as the triangle corners.
        this.vertices = vertices.Take(3).ToArray();
        this.resolution = resolution;

        (flatVerts, tris) = BuildTriangle(vertices, resolution);

        mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = Radialise(flatVerts,vertices[0].magnitude);
        mesh.triangles = tris;
        //mesh.RecalculateNormals();
        mesh.normals = mesh.vertices;
        //mesh.colors = pointColors;

    }

    public Mesh ChangeVertices(Vector3[] vertices,int resolution) {
        if (vertices.Length < 3) {
            Debug.Log("Error! Insufficient Vertices for a triangle!");
        }
        if (vertices.Length > 3) {
            Debug.Log("Warning! Too many Vertices supplied for Triangle! Only first 3 will be used!");
        }
        // Take the first three vertices in the argument as the triangle corners.
        Vector3[] newCorners = vertices.Take(3).ToArray();

        if(resolution == this.resolution) {
            flatVerts = BuildTriangleVerts(newCorners, resolution);
            mesh.vertices = Radialise(flatVerts, vertices[0].magnitude);
        }
        else {
            (flatVerts, tris) = BuildTriangle(vertices, resolution);
        }
        
        mesh.vertices = Radialise(flatVerts, vertices[0].magnitude);
        mesh.normals = mesh.vertices;


        return mesh;
    }

    public Mesh ChangeVertices(Vector3[] vertices) {
        return ChangeVertices(vertices, this.resolution);
    }

    /// <summary>
    /// Only build flat triangle vertices. Useful for when moving a triangle without changing the resolution.
    /// No need to recompute the triangle indices in this case.
    /// </summary>
    /// <param name="corners"></param>
    /// <param name="resolution"></param>
    /// <returns></returns>
    static Vector3[] BuildTriangleVerts(Vector3[] corners, int resolution) {
        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 2) / 2];



        // Build vertices
        Vector3 v0 = corners[0].normalized; 
        Vector3 v1 = (corners[1].normalized - corners[0].normalized) / resolution;
        Vector3 v2 = (corners[2].normalized - corners[1].normalized) / resolution;
        int maxPInLine = resolution + 1;
        int pointsInLine = 1;
        int vertIndex = 0;
        for (int i = 0; i < maxPInLine; i++) {
            for (int j = 0; j < pointsInLine; j++) {
                
                vertices[vertIndex] = v0 + i * v1 + j * v2;
                vertIndex++;
            }
            pointsInLine++;
        }

        return vertices;
    }

    static (Vector3[],int[]) BuildTriangle(Vector3[] corners, int resolution) {
        int[] tris = new int[resolution * resolution * 3];


        // Build vertices
        Vector3[] vertices = BuildTriangleVerts(corners, resolution);

        // Build list of triangles for the vertices:
        int trisInLine = 1;
        int refThisLine = 1;
        int refPrevLine = 0;
        int triIndex = 0;
        for (int i = 0; i < resolution; i++) {
            bool right = true;
            for (int j = 0; j < trisInLine; j++) {
                if (right) {
                    tris[triIndex + 0] = refThisLine;
                    tris[triIndex + 1] = refPrevLine;
                    tris[triIndex + 2] = refThisLine + 1;
                    triIndex += 3;

                    refThisLine++;
                    right = false;
                }
                else {
                    tris[triIndex + 0] = refPrevLine;
                    tris[triIndex + 1] = refPrevLine + 1;
                    tris[triIndex + 2] = refThisLine;
                    triIndex += 3;

                    refPrevLine++;
                    right = true;
                }
            }

            // use the final vertex index of the current and prev lines, to reset the refs for the next lines
            refThisLine++;
            refPrevLine++;
            trisInLine += 2;

        }


        return (vertices, tris);
    }

    public Mesh CopyMesh(Mesh newMesh) {
        mesh = newMesh;
        return mesh;
    }


    Vector3[] Normalise(Vector3[] vectors) {
        Vector3[] normalised = vectors;
        for (int i = 0; i < vectors.Length; i++) {
            normalised[i].Normalize();
        }
        return normalised;
    }

    Vector3[] Radialise(Vector3[] vectors, float radius) {
        Vector3[] normalised = vectors;
        //Vector3 scale = new Vector3(radius, radius, radius);
        /*foreach (var vec in normalised) {
            vec.Normalize();
            vec.Scale(scale);
        }*/

        for(int i = 0; i < vectors.Length; i++) {
            normalised[i].Normalize();
            normalised[i] *= radius;
        }

        return normalised;
    }

}

