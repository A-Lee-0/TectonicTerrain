using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using GlobeLines;


public class MantleCellRenderer : MonoBehaviour
{

    /// <summary>
    /// The MantleCell to be rendered
    /// </summary>
    MantleCell cell;
    /// <summary>
    /// Array of vertices comprising the boundary of the MantleCell.
    /// </summary>
    public Vector3[] vertices;
    /// <summary>
    /// Array of MantleCells sharing a boundary with this MantleCell.
    /// </summary>
    MantleCell[] neighbours;

    GameObject circleHolder;
    GameObject lineHolder;

    /*    public MantleCellRenderer(MantleCell cell, Vector3[] boundaryVertices) {
            this.cell = cell;
            this.vertices = boundaryVertices;



        }*/

    public void Setup(MantleCell cell, Vector3[] boundaryVertices) {
        this.cell = cell;
        this.vertices = SortClockwise(boundaryVertices);
    }

    public void Reset(MantleCell cell, Vector3[] boundaryVertices) {
        this.cell = cell;
        this.vertices = SortClockwise(boundaryVertices);
    }

    public MantleCell Cell => cell;
    public Vector3[] Vertices => vertices;
    public GameObject LineHolder => lineHolder;
    public GameObject CircleHolder => circleHolder;


    public void DrawCellCircle(Color color, float width = 0.1f) {

        LineDrawer circle = LineDrawer.GlobeCircle(cell.PlanetPosition, cell.strength, width, color, cell.Planet);
        MeshFilter meshFilter;

        if (circleHolder == null) {
            circleHolder = new GameObject("circle_holder");
            circleHolder.transform.parent = transform;
            circleHolder.AddComponent<MeshRenderer>().sharedMaterial = new Material(circle.shader);
            meshFilter = circleHolder.AddComponent<MeshFilter>();
        }
        else { meshFilter = circleHolder.GetComponent<MeshFilter>(); }

        meshFilter.sharedMesh = circle.mesh;

    }


    public void DrawCellBoundary(Color color, float width = 0.1f) {
        LineDrawer line;
        //LineDrawer line = LineDrawer.GlobeCircle(cell.PlanetPosition, cell.strength, 0.1f, color, cell.Planet);
        MeshFilter meshFilter;

        Mesh fullMesh = new Mesh();

        List<Vector3> meshVertices = new List<Vector3>();
        List<int> meshTriangles = new List<int>();
        List<Color> meshColors = new List<Color>();

        if (lineHolder == null) {
            lineHolder = new GameObject("line_holder");
            lineHolder.transform.parent = transform;
            lineHolder.AddComponent<MeshRenderer>();
            meshFilter = lineHolder.AddComponent<MeshFilter>();
        }
        else { meshFilter = lineHolder.GetComponent<MeshFilter>(); }

        for (int i = 0; i < vertices.Length; i++) {
            Vector3 thisVert = vertices[i];
            Vector3 nextVert = vertices[(i + 1) % vertices.Length];

            line = LineDrawer.GlobeLine(thisVert, nextVert, width, color, cell.Planet);


            // TODO: consider replacing this junk with the Mesh.CombineMeshes() method.

            int prevVerts = meshVertices.Count;
            int[] tris = line.mesh.triangles;

            meshVertices.AddRange(line.mesh.vertices);
            for (int j = 0; j < tris.Length; j++) {
                tris[j] += prevVerts;
            }

            meshTriangles.AddRange(tris);
            meshColors.AddRange(line.mesh.colors);
            lineHolder.GetComponent<MeshRenderer>().sharedMaterial = new Material(line.shader);

        }

        fullMesh.vertices = meshVertices.ToArray();
        fullMesh.normals = meshVertices.ToArray();
        fullMesh.triangles = meshTriangles.ToArray();
        fullMesh.colors = meshColors.ToArray();



        meshFilter.sharedMesh = fullMesh;

    }

    private Vector3[] SortClockwise(Vector3[] boundaryVertices) {
        // To draw the cell boundary, the boundary vertices need to be sorted.
        // this can be done by projecting the vertices to 2D along the cell's centre, then taking the Vector2.Angle between the point and a fixed reference

        Quaternion rotToUp = Quaternion.FromToRotation(cell.PlanetPosition, Vector3.up);

        Vector3[] rotVerts = new Vector3[boundaryVertices.Length];

        for (int i = 0; i < boundaryVertices.Length; i++) {
            rotVerts[i] = rotToUp * boundaryVertices[i];
        }

        Vector2[] flatVerts = new Vector2[boundaryVertices.Length];
        for (int i = 0; i < boundaryVertices.Length; i++) {
            flatVerts[i].x = rotVerts[i].x;
            flatVerts[i].y = rotVerts[i].z;
        }

        float[] angles = new float[boundaryVertices.Length];

        for (int i = 0; i < boundaryVertices.Length; i++) {
            angles[i] = Vector2.SignedAngle(Vector2.right, flatVerts[i]);
        }


        Array.Sort(angles, boundaryVertices);


        return boundaryVertices;

    }

    public float Area => LineDrawer.Area(vertices, cell.Planet);


}