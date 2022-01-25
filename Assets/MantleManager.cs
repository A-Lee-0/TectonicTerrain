﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using GlobeLines;

using GK;       // third party module for calculating the convex hull from a set of vertices.

public class MantleManager : MonoBehaviour
{
    public List<MantleCell> mantleCells;
    public Planet planet;
    public Mesh[] meshes;

    public LineDrawer[] lines;
    public GameObject[] lineHolders;

    Color[] region_colors = { Color.red, Color.blue, Color.yellow, Color.magenta, Color.green, Color.cyan };

    public List<MantleCellRenderer> mantleCellRenderers;


    // Runs when unity compiles the script (i.e. in edit mode, not play)
    private void OnValidate() {

        // having nullreference exceptions running it here - call it from planet instead.
        //DebugSetup();
    }

    public void DebugSetup() {
        if (mantleCells == null) { mantleCells = new List<MantleCell>(); }
        mantleCells.Clear();
        
        planet = FindObjectOfType<Planet>();
         
        
        mantleCells.Add(new MantleCell(Vector3.right, planet, 2f));
        mantleCells.Add(new MantleCell(Vector3.forward+ Vector3.up, planet, 1.5f));
        mantleCells.Add(new MantleCell(Vector3.up, planet, 0.5f));
        mantleCells.Add(new MantleCell(new Vector3(-1f,-1f,-1.5f), planet, 0.5f));
        

        /*mantleCells.Add(new MantleCell(Vector3.right, planet, 1f));
        mantleCells.Add(new MantleCell(Vector3.forward + Vector3.up, planet, 2f));
        mantleCells.Add(new MantleCell(Vector3.up, planet, 1f));
        mantleCells.Add(new MantleCell(new Vector3(-1f, -1f, -1.5f), planet, 1f));
        */



        Mesh[] meshes = planet.PlanetMeshes();
        this.meshes = meshes;

        PaintInfluenceOnMeshes(meshes);

        //Debug.Log(GetCellStrength(mantleCells[0], new Vector3(1, 0, 1).normalized * 2f, true));

        //draw lines

        //DrawCellCircles();
        BuildPowerDiagramBoundaries(mantleCells.ToArray());

    }


    void PaintInfluenceOnMeshes(Mesh[] meshes) {
        foreach (Mesh mesh in meshes) {
            int vert_count = mesh.vertexCount;
            Color[] mesh_colors = new Color[vert_count];
            for (int i = 0; i < vert_count; i++) {
                var vert = mesh.vertices[i];

                float strongest = float.NegativeInfinity;
                MantleCell strongest_cell;
                // check each vertex against all cells, to see which one scores highest.
                for (int j = 0; j < mantleCells.Count; j++) {
                    MantleCell cell = mantleCells[j];
                    float cell_strength = GetCellStrength(cell, vert);
                    if (cell_strength > strongest) {
                        strongest = cell_strength;
                        strongest_cell = cell;
                        mesh_colors[i] = region_colors[j % region_colors.Length];   // to make color list periodic if more than 6 cells.
                    }

                }
            }
            mesh.SetColors(mesh_colors);
            
            
        }
    }

    float GetCellStrength(MantleCell cell, Vector3 pos, bool debug = false) {
        return GetCellStrengthPowerDiagram(cell, pos, debug);
    }

    float GetCellStrengthConservedFlux(MantleCell cell, Vector3 pos, bool debug = false) {
        // The strength of a cell at a given position is just the strength of the cell divided by the perimeter of the slice through the sphere at the angule to the position.
        // I.e. the plane orthogonal to the cell's radial line that passes through the target point.
        // Can calculate using the dot product and pythagorus.
        // r = sqrt( R^2 - (c.p)^2 )
        


        Vector3 cell_pos = cell.PlanetPosition;
        float R = cell_pos.magnitude;
        float dot = Vector3.Dot(cell_pos, pos)/R;

        float circumference = 2 * Mathf.PI * Mathf.Sqrt(R * R - dot * dot);


        if (debug) {
            Debug.Log("cell_pos: " + cell_pos);
            Debug.Log("R: " + R);
            Debug.Log("dot: " + dot);
            Debug.Log("circumference: " + circumference);
            Debug.Log("result: " + cell.strength / circumference);
        }


        return cell.strength / circumference;
         
        

    }

    float GetCellStrengthPowerDiagram(MantleCell cell, Vector3 pos, bool debug = false) {
        // In 2D, the 'distance' of a point from a cell is defined from the euclidean distance from the cell, d, and the radius of the cell's circle, r.
        //      D = d^2 - r^2.
        // Note that for points within the cell's circle, this distance is negative.
        // The region associated with the cell c_i is then the set of points for which D(c_i) < D(c_j)
        //
        // To extend this onto the surface of sphere, consider http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.95.3444&rep=rep1&type=pdf
        // Running through the maths for a non-unit sphere ends up similar, but more explicit about what is an angle vs an arclength:
        //      D = cos( θ(P,P_i) ) / cos( θ_i )
        // where the θs are the angle from the arbitrary point, P, to the centre of circle i, P_i, and the angular size of circle i, θ_i = r_i/R.
        // i.e instead of caring about the radius of a circle r_i, better to store the angle of a circle θ_i
        // 0 < θ_i < π/2



        //1. Find cos( θ(P,P_i) ) using dot product
        float top = Vector3.Dot(cell.PlanetPosition.normalized, pos.normalized);
        float bottom = cell.cosθ;

        //negate, as code currently looks for a 'strength' i.e. largest wins, rather than a 'distance' i.e. smallest wins.
        return top / bottom;
        
    }

    public void DrawCellCircles() {


        lines = new LineDrawer[mantleCells.Count];
        if (lineHolders.Length < mantleCells.Count) { Array.Resize(ref lineHolders, mantleCells.Count); }
        for (int i = 0; i < mantleCells.Count; i++) {
            MantleCell cell = mantleCells[i];
            LineDrawer circle = LineDrawer.GlobeCircle(cell.PlanetPosition, cell.strength, 0.1f, Color.black, cell.Planet);
            lines[i] = circle;

            MeshFilter meshFilter;
            if (lineHolders[i] == null) {
                lineHolders[i] = new GameObject("line_holder");
                lineHolders[i].transform.parent = transform;
                lineHolders[i].AddComponent<MeshRenderer>().sharedMaterial = new Material(circle.shader);
                meshFilter = lineHolders[i].AddComponent<MeshFilter>();
            }
            else { meshFilter = lineHolders[i].GetComponent<MeshFilter>(); }
            meshFilter.sharedMesh = circle.mesh;
        }
    }


    public void BuildPowerDiagramBoundaries(MantleCell[] cells) {

        // stores triangle vertex data from the dual convex hull.
        // each three points stores a face from the dual hull - i.e. where 3 half-spaces from the mantleCells will form a point on their convex hull.
        int[] cellIntersections; List<Vector3> normals;
        (cellIntersections, normals) = FindDualConvexHull(cells);

        Debug.Log("Program found " + cellIntersections.Length + " intersections");
        for (int i = 0; i < cellIntersections.Length; i++) {
            Debug.Log("Intersection " + i + ": " + cellIntersections[i]);
        }

        List <Vector3>[] cellPoints = new List<Vector3>[cells.Length];
        for (int i = 0; i < cells.Length;i++){ cellPoints[i] = new List<Vector3>(); }
        
        // for each face in dual space, calculate corresponding intersection in real space, and add it to cellPoints list for each cell.
        // In principle this could error if all three points lie on a great-circle of the sphere, but for any reasonable number of regions, this won't happen.
        for (int i = 0; i< cellIntersections.Length/3; i++) {

            //use cell indices to find cells joining at point.
            int c1 = cellIntersections[3 * i];
            int c2 = cellIntersections[3 * i + 1]; 
            int c3 = cellIntersections[3 * i + 2];

            MantleCell cell1 = cells[c1];
            MantleCell cell2 = cells[c2];
            MantleCell cell3 = cells[c3];

            Plane plane1 = new Plane(cell1.PlanetPosition.normalized, cell1.PlanetPosition.magnitude * cell1.cosθ);
            Plane plane2 = new Plane(cell2.PlanetPosition.normalized, cell2.PlanetPosition.magnitude * cell2.cosθ);
            Plane plane3 = new Plane(cell3.PlanetPosition.normalized, cell3.PlanetPosition.magnitude * cell3.cosθ);

            Vector3 intersection;
            bool noError = PlanesIntersectAtSinglePoint(plane1, plane2, plane3, out intersection);

            if (!noError) { Debug.LogError("Unable to find intersection point for three planes!" + plane1 + ", " + plane2 + ", " + plane3); }

            // check if intersection point is on wrong side of globe (e.g. all cells on northern hemisphere still has intersections on southern hemisphere)
            Vector3 normal = normals[3 * i];
            if(Vector3.Dot(normal,intersection) < 0f) { intersection *= -1f; }

            intersection.Normalize();

            cellPoints[c1].Add(intersection);
            cellPoints[c2].Add(intersection);
            cellPoints[c3].Add(intersection);
        }

        for(int i = 0; i< mantleCells.Count; i++) {
            Vector3[] boundaryVertices = cellPoints[i].ToArray();

            GameObject cellObj;
            MantleCellRenderer cellRenderer;

            if (mantleCellRenderers.Count < i + 1) {
                cellObj = new GameObject("cell_renderer");
                
                cellObj.transform.parent = transform;

                cellRenderer = cellObj.AddComponent<MantleCellRenderer>();
                cellRenderer.Setup(mantleCells[i], boundaryVertices);
                mantleCellRenderers.Add(cellRenderer);
            }
            else {
                cellRenderer = mantleCellRenderers[i];
                cellRenderer.Reset(mantleCells[i], boundaryVertices);
            }

            cellRenderer.DrawCellCircle(Color.black);
            cellRenderer.DrawCellBoundary(Color.gray);
        }
        


    }


    public (int[],List<Vector3>) FindDualConvexHull(MantleCell[] cells) {


        // Create list of Dual space vertices.
        // index of dual vertex = index of its corresponding cell in cells[].
        List<Vector3> dualVertices = new List<Vector3>();
        for (int i = 0; i < mantleCells.Count; i++) {
            // Pᵢ* = Pᵢ / cosθᵢ
            MantleCell cell = mantleCells[i];
            dualVertices.Add(cell.PlanetPosition / cell.cosθ);
        }

        var calc = new ConvexHullCalculator();
        var verts = new List<Vector3>();
        var vertMap = new List<int>();      // Added by ALee. For each vertex added to verts, keeps track of it's original index in dualVertices.
        var tris = new List<int>();
        var normals = new List<Vector3>();
        


        calc.GenerateHull(dualVertices, true, ref verts, ref tris, ref normals, ref vertMap);

        var cellIntersections = new int[tris.Count];

        for (int i = 0; i<tris.Count; i++) {
            cellIntersections[i] = vertMap[tris[i]];
        }

        return (cellIntersections,normals);


    }


    // Start is called before the first frame update
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }



    


    /// <summary>
    /// Returns true if the three planes connect at a point, else false.
    /// The intersection point is output in the intersectionPoint field.
    /// </summary>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="intersectionPoint"></param>
    /// <returns></returns>
    private bool PlanesIntersectAtSinglePoint(Plane p0, Plane p1, Plane p2, out Vector3 intersectionPoint) {
        const float EPSILON = 1e-4f;

        var det = Vector3.Dot(Vector3.Cross(p0.normal, p1.normal), p2.normal);
        if (Mathf.Abs(det) < EPSILON) {
            intersectionPoint = Vector3.zero;
            return false;
        }

        intersectionPoint =
            (-(p0.distance * Vector3.Cross(p1.normal, p2.normal)) -
            (p1.distance * Vector3.Cross(p2.normal, p0.normal)) -
            (p2.distance * Vector3.Cross(p0.normal, p1.normal))) / det;

        return true;
    }
}
