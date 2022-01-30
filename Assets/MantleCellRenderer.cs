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
    public MantleCell cell;
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
    GameObject meshHolder;

    public Shader lineShader;

    public Mesh cellMesh;
    public LineDrawer circleLine;
    public Mesh lineMesh;

    public float debugArea;
    public bool debugHasCell;
    public string debugCellName;
    public string debugLastDeathType;

    public List<SphericalTriangleMesh> cellSubMeshes = new List<SphericalTriangleMesh>();
    public List<LineDrawer> boundarySubLines = new List<LineDrawer>();

    /*    public MantleCellRenderer(MantleCell cell, Vector3[] boundaryVertices) {
            this.cell = cell;
            this.vertices = boundaryVertices;



        }*/

    private void Update() {
        debugHasCell = !(this.cell == null);
        if (!(this.cell == null)) {
            debugCellName = cell.DebugName;
        }
        else { debugCellName = "Null"; }
    }

    public void Setup(MantleCell cell, Vector3[] boundaryVertices) {
        this.cell = cell;
        this.vertices = SortClockwise(boundaryVertices);
        cell.SetRenderer(this);
        lineShader = Shader.Find("Particles/Standard Surface");
    }

    public void Reset(MantleCell cell, Vector3[] boundaryVertices) {
        this.cell = cell;
        this.vertices = SortClockwise(boundaryVertices);
        cell.SetRenderer(this);
    }

    public void ClearCell() { this.cell = null; }
    public void ClearVertices() { this.vertices = Array.Empty<Vector3>(); }

    public MantleCell Cell => cell;
    public Vector3[] Vertices => vertices;
//    public GameObject LineHolder => lineHolder;
//    public GameObject CircleHolder => circleHolder;
    public float Area => LineDrawer.Area(vertices, cell.Planet);
    public Vector3 Centroid => LineDrawer.PolygonMoment(Vertices).normalized * cell.Planet.radius;

    public void ClearMeshes() {
        if (circleHolder == null) { circleHolder = LineDrawer.GetLineHolder(this.gameObject, "circle_holder"); }
        if (lineHolder == null) { lineHolder = LineDrawer.GetLineHolder(gameObject, "line_holder"); }
        if (meshHolder == null) { meshHolder = LineDrawer.GetLineHolder(this.gameObject, "mesh_holder"); }

        circleHolder.GetComponent<MeshFilter>().mesh.Clear();
        lineHolder.GetComponent<MeshFilter>().mesh.Clear();
        meshHolder.GetComponent<MeshFilter>().mesh.Clear();
    }

    public void DrawCellCircle(Color color, float width = 0.1f) {
        //Debug.Log("dir: " + cell.PlanetPosition + " , strength: " + cell.strength);
        if (circleHolder == null) { circleHolder = LineDrawer.GetLineHolder(this.gameObject, "circle_holder"); }

        if (circleLine == null) { circleLine = LineDrawer.NewGlobeCircle(cell.PlanetPosition, cell.strength, width, color, cell.Planet); }
        else { circleLine.GlobeCircle(cell.PlanetPosition, cell.strength, width, color, cell.Planet); }

        //Debug.Log("no. verts: " + circleLine.mesh.vertices.Length);

        //foreach (var vert in circleLine.mesh.vertices) {
        //    Debug.Log("vert: " + vert);
        //} 
        circleHolder.GetComponent<MeshFilter>().sharedMesh = circleLine.mesh;

    }


    public void DrawCellBoundary(Color color, float width = 0.1f) {
        if (lineHolder == null) {lineHolder = LineDrawer.GetLineHolder(gameObject,"line_holder");}

        MeshFilter meshFilter = lineHolder.GetComponent<MeshFilter>();

        CombineInstance[] meshCombineArray = new CombineInstance[vertices.Length];

        for (int i = 0; i < vertices.Length; i++) {
            Vector3 thisVert = vertices[i];
            Vector3 nextVert = vertices[(i + 1) % vertices.Length];

            // Find or Make LineDrawers for each side of the cell.
            LineDrawer subLine;
            if (i < boundarySubLines.Count) {
                subLine = boundarySubLines[i];
                subLine.GlobeLine(thisVert, nextVert, width, color, cell.Planet);
            }
            else {
                subLine = LineDrawer.NewGlobeLine(thisVert, nextVert, width, color, cell.Planet);
                boundarySubLines.Add(subLine);
            }

            meshCombineArray[i].mesh = subLine.mesh;
            meshCombineArray[i].transform = transform.localToWorldMatrix;
        }


        if (lineMesh == null) { lineMesh = new Mesh(); }
        else { lineMesh.Clear(); }


        lineMesh.CombineMeshes(meshCombineArray);
        meshFilter.sharedMesh = lineMesh;        

    }


    public Mesh MakeMesh(int resolution) {
        Vector3 centre = Centroid;
        Vector3 v1;
        Vector3 v2;

        Vector3[] corners = new Vector3[3];
        corners[0] = centre;

        CombineInstance[] meshCombineArray = new CombineInstance[vertices.Length];

        for (int i = 0; i < vertices.Length; i++) {
            v1 = vertices[i];
            v2 = vertices[(i+1)%vertices.Length];
            corners[1] = v1; corners[2] = v2;


            // Find or Make submeshes for each component triangle in the cell.
            SphericalTriangleMesh submesh;
            if (i < cellSubMeshes.Count) {
                submesh = cellSubMeshes[i];
                submesh.ChangeVertices(corners, resolution);
            }
            else {
                submesh = new SphericalTriangleMesh(corners, resolution);
                cellSubMeshes.Add(submesh);
            }

            meshCombineArray[i].mesh = submesh.mesh;
            meshCombineArray[i].transform = transform.localToWorldMatrix;
        }

        // find obj holding the mesh in unity
        if (meshHolder == null) { meshHolder = LineDrawer.GetLineHolder(this.gameObject, "mesh_holder"); }
        MeshFilter meshFilter = meshHolder.GetComponent<MeshFilter>();

        if (cellMesh == null) { cellMesh = new Mesh(); }
        else { cellMesh.Clear(); } 

        
        cellMesh.CombineMeshes(meshCombineArray);
        meshFilter.sharedMesh = cellMesh;

        return cellMesh;

    }

    public void SetMeshColor(Color color) {
        if (meshHolder == null) { meshHolder = LineDrawer.GetLineHolder(this.gameObject, "mesh_holder"); }
        meshHolder.GetComponent<MeshRenderer>().material.color = color;
    }

    private Vector3[] SortClockwise(Vector3[] boundaryVertices) {
        // To draw the cell boundary, the boundary vertices need to be sorted.
        // this can be done by projecting the vertices to 2D along the cell's centre, then taking the Vector2.Angle between the point and a fixed reference

        //initially rotated using the cell.PlanetPosition, but this is not necessarily inside the cell region.
        //ideally just reuse centroid, but that is calculated using the clockwise sorted points here.
        //instead, just use the mean vector - it's gross, but should be fine as the regions are always convex.
        Vector3 meanVert = Vector3.zero;
        for (int i = 0; i < boundaryVertices.Length; i++) {
            meanVert += boundaryVertices[i];
        }
        Quaternion rotToUp = Quaternion.FromToRotation(meanVert, Vector3.up);

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

    


}