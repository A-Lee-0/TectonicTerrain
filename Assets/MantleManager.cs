using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using GlobeLines;

using GK;       // third party module for calculating the convex hull from a set of vertices.


/*
 * Added basic UI elements to allow player to change target area and speed towards centroid values for cells, and to 
 * choose from different cell respawn, and area calculation methods. Unfortunately the new respawn methods in particular 
 * are quite buggy. Using MTTH cell spawning can easily cause the number of cells to fall too low, causing the ConvexHull 
 * calculator to fail. Using the spawning at near-boundary vertices, to ensure the new cell has a small area, tends to 
 * cause chain spawns in the same spots, causing either the ConvexHull or the subsequent vertex finder to fail. Attempts 
 * to fix it by preventing spawns too close to other vertices then causes it to run out of vertices to try, throw index
 * out of bounds errors...
 * 
 * 
 * */

public class MantleManager : MonoBehaviour
{
    public List<MantleCell> mantleCells;
    public Planet planet;

    public List<LineDrawer> dirLines = new List<LineDrawer>(); // Holds lines from cell circles to the cell's region CoM


    public GameObject lineHolder;

    List<Vector3> planetIntersectionVertices = new List<Vector3>();
    List<float> planetIntersectionVertexScores = new List<float>();


    public Vector3[] DebugIntersections;
    public float[] DebugScores;


    Color[] region_colors = { Color.red, Color.blue, Color.yellow, Color.magenta, Color.green, Color.cyan };


    public float centroidAttraction = -0.05f;
    public float targetAreaFactor = 1f;
    public int strengthUpdateMethod = 1;
    public int respawnMethod = 0;


    public float respawnMTTH = 5f; // Mean Time To Happen for cell spawns in seconds.
    public float defaultRespawnStrength = 0.01f;

    public List<MantleCellRenderer> unassignedMantleCellRenderers;


    int lastColor = 0;

    // Runs when unity compiles the script (i.e. in edit mode, not play)
    private void OnValidate() {

        //strengthUpdateMethods.Add()

        // having nullreference exceptions running it here - call it from planet instead.
        //DebugSetup();
    }


    public void DebugSetup() {
        DateTime t0 = DateTime.Now;

        if (mantleCells == null) { mantleCells = new List<MantleCell>(); }
        mantleCells.Clear();
        
        planet = FindObjectOfType<Planet>();

        /*
        mantleCells.Add(new MantleCell(Vector3.right, planet, 2f)); 
        mantleCells.Add(new MantleCell(Vector3.forward+ Vector3.up, planet, 1.5f));
        mantleCells.Add(new MantleCell(Vector3.up, planet, 0.5f));
        mantleCells.Add(new MantleCell(new Vector3(-1f,-1f,-1.5f), planet, 0.5f));
        */

        UnityEngine.Random.InitState(123); 

        foreach (int i in Enumerable.Range(0, 20)) {
            mantleCells.Add(new MantleCell(UnityEngine.Random.onUnitSphere,planet,UnityEngine.Random.Range(0f,1f)));
            mantleCells[i].color = region_colors[lastColor];
            lastColor = (lastColor + 1)% region_colors.Length;
        }

        /*mantleCells.Add(new MantleCell(Vector3.right, planet, 1f));
        mantleCells.Add(new MantleCell(Vector3.forward + Vector3.up, planet, 2f));
        mantleCells.Add(new MantleCell(Vector3.up, planet, 1f));
        mantleCells.Add(new MantleCell(new Vector3(-1f, -1f, -1.5f), planet, 1f));
        */



        DateTime t1 = DateTime.Now;
        //PaintInfluenceOnMeshes(meshes);
        DateTime t2 = DateTime.Now;

        //Debug.Log(GetCellStrength(mantleCells[0], new Vector3(1, 0, 1).normalized * 2f, true));

        //draw lines

        //DrawCellCircles();


        // Mark all CellRenderers in the scene as unused for now.
        MantleCellRenderer[] renderers = FindObjectsOfType<MantleCellRenderer>();
        foreach (var renderer in renderers) {
            renderer.ClearCell();
            unassignedMantleCellRenderers.Add(renderer);
        }

        BuildPowerDiagramBoundaries(mantleCells.ToArray());
        DateTime t3 = DateTime.Now;

        // Draw lines from each circle to their 'CoM'
        DrawLinesToCoM(mantleCells);

        /*
        foreach (var cell in mantleCells) {
            cell.CalculateCentroid();
            cell.CalculateArea();
            if (cell.HasRegion) {
                var newLine = LineDrawer.NewGlobeLine(cell.PlanetPosition, cell.Centroid, 0.02f, Color.white, cell.Planet);
                dirLines.Add(newLine);
            }
        }

        if (lineHolder == null) { LineDrawer.GetLineHolder(this.gameObject, "line_holder"); }
        MeshFilter meshFilter = lineHolder.GetComponent<MeshFilter>();

        CombineInstance[] meshArray = new CombineInstance[dirLines.Count];
        for (int i = 0; i < dirLines.Count; i++) {
            meshArray[i].mesh = lines[i].mesh;
            meshArray[i].transform = transform.localToWorldMatrix;
        }

        meshFilter.sharedMesh.Clear(); 
        meshFilter.sharedMesh.CombineMeshes(meshArray);
        */

       

        //debug Spherical Triangle Drawing:
        var testCell = mantleCells[10];
        testCell.Renderer.MakeMesh(10);
    }


    /// <summary>
    /// Using this instead of the Unity Update() method, as it allows me to control when in the update cycle it is triggered.
    /// 1a. Moves all MantleCells towards their region CoM, and changes radius depending on it's current area.
    ///  b. Moves MantleCells without a region to a new location.
    /// 2.  Recalculates the boundary lines for each MantleCell.
    /// </summary>
    public void DoUpdate() {
        respawnsThisFrame = 0;  // reset this so respawn indices are correct
        RecalculateVertexScoreRankings();
        DebugIntersections = planetIntersectionVertices.ToArray();
        DebugScores = planetIntersectionVertexScores.ToArray();

//        Debug.Log("Smallest At: " + vertsNear1[0]);
//        Debug.DrawRay(Vector3.zero, vertsNear1[0] * 5, Color.white);

        // 1a. Moves all MantleCells towards their region CoM, and changes radius depending on it's current area.
        // TODO: maybe make this more interesting? As is, once a cell's circle is slightly offset from its region, it begins a suicide charge away from itself.S

        float targetStr = 0f;
        MantleCell cellToMove;
        for (int i = 0; i< mantleCells.Count; i++) {
            cellToMove = mantleCells[i];
            if (cellToMove.HasRegion && cellToMove.Area > 0.01f) {
                targetStr = CalculateNewCellStrength(cellToMove, this.targetAreaFactor);
                cellToMove.SetPosition(Vector3.SlerpUnclamped(cellToMove.PlanetPosition, cellToMove.Centroid, this.centroidAttraction * 0.1f));
                cellToMove.SetStrength(Mathf.Lerp(cellToMove.strength, targetStr, 0.1f));

            }
            //  b. Moves MantleCells without a region to a new location.
            else {
                // TODO: Make this more interesting than just a random point. Ideally want to identify points that are natually bounded, e.g. intersection of two circles
                // could work with cell colour, and have the new region be the average colour - it's 'budded' from the parents.
                // or could just have bud be one parent colour, and have it be a 'colony'
                RespawnCell(cellToMove);
            }
        }

        // Spawn new cells for MTTH spawning
        // Probability of at least 1 event in time dt = 1 - e^(-dt/MTTH)
        if(UnityEngine.Random.value > Mathf.Exp(-Time.deltaTime / respawnMTTH)) {
            //spawn a new cell
            MantleCell newCell = RespawnCell();
            if (!(newCell == null)) { mantleCells.Add(RespawnCell()); }
            
        }

        //Debug.Log("Strengths this frame:");
        foreach (var cell in mantleCells) {
            if (cell.HasRenderer) {
                cell.Renderer.debugArea = cell.Area;
                //Debug.Log("cell strength: " + cell.strength);
                if (float.IsNaN(cell.strength)) {
                    Debug.Log("cell area: " + cell.Area + " , pos: " + cell.PlanetPosition);
                    Debug.Log("no cell verts: " + cell.Renderer.Vertices.Length);
                    for (int i = 0; i < cell.Renderer.Vertices.Length; i++) { Debug.Log("vert: " + cell.Renderer.Vertices[i]); }
                }
            }
        }
        

        // 2.  Recalculates the boundary lines for each MantleCell.
        //     Also Assigns renderers to cells if they need them.
        BuildPowerDiagramBoundaries(mantleCells.ToArray());



        // Draw lines from each circle to their 'CoM'
        DrawLinesToCoM(mantleCells);



        // Make surface meshes for each cell
        int surfaceResolution = 10; 
        foreach (var cell in mantleCells) {
            cell.Renderer.MakeMesh(surfaceResolution);
            cell.Renderer.SetMeshColor(cell.color);
        }

        // Moved Into loop above, using saved color on each cell. Useful for genetics down the road maybe.
        //for (int i = 0; i < mantleCells.Count; i++) {
        //    mantleCells[i].Renderer.SetMeshColor(region_colors[i % region_colors.Length]);
        //}

    }


    public Dictionary<int, string> respawnMethods = new Dictionary<int, string> { { 0, "Random, Instant" },
                                                                                  { 1, "At ~1 score vertex, Instant" },
                                                                                  { 2, "At most score vertex, Instant" },
                                                                                  { 3, "Random, MTTH  (Poisson)" },
                                                                                  { 4, "At ~1 score vertex, MTTH" },
                                                                                  { 5, "At most score vertex, MTTH" }};

    int respawnsThisFrame = 0;
    MantleCell RespawnCell(MantleCell cell) {       // used for cell-by-cell respawns (i.e. instant)
        switch (respawnMethod) {
            case 0:
                Vector3 newPos = UnityEngine.Random.onUnitSphere;
                while (SpawnIsTooCloseToAnotherCell(newPos)){
                    Debug.Log("Retry respawn - random location too close to another cell! Trying again...");
                    respawnsThisFrame += 1;
                    newPos = UnityEngine.Random.onUnitSphere;
                }
                Debug.Log("Respawned: " + respawnMethods[0]);
                cell.SetPosition(newPos);
                cell.SetStrength(defaultRespawnStrength);
                respawnsThisFrame += 1;
                return cell;
            case 1:
                while (SpawnIsTooCloseToAnotherCell(vertsNear1[respawnsThisFrame])){
                    Debug.Log("Retry respawn - index " + respawnsThisFrame + "too close to another cell! Trying again...");
                    respawnsThisFrame += 1;
                }
                Debug.Log("Respawned: " + respawnMethods[1] + " at: " + vertsNear1[respawnsThisFrame] + "using index: " + respawnsThisFrame);
                cell.SetPosition(vertsNear1[respawnsThisFrame]);
                cell.SetStrength(defaultRespawnStrength);
                respawnsThisFrame += 1;
                return cell;
            case 2:
                while (SpawnIsTooCloseToAnotherCell(vertsLargest[respawnsThisFrame])) {
                    Debug.Log("Retry respawn - index " + respawnsThisFrame + "too close to another cell! Trying again...");
                    respawnsThisFrame += 1;
                }
                Debug.Log("Respawned: " + respawnMethods[2]);
                cell.SetPosition(vertsLargest[respawnsThisFrame]);
                cell.SetStrength(defaultRespawnStrength);
                respawnsThisFrame += 1;
                return cell;
            default: // If not doing one of the above, then delete the cell.
                Debug.Log("Deleting cell");
                mantleCells.Remove(cell); //remove cell from list of cells. c# GC 'should' pick it up from here...
                unassignedMantleCellRenderers.Add(cell.Renderer); // Add to list of unused cells.
                cell.Renderer.ClearCell();  // Make the renderer forget the cell
                cell.SetRenderer(null);     // Make the cell forget the renderer
                return null;
        }
    }
    MantleCell RespawnCell() {                  // used for update-wide respawns (i.e. non-instant)
        MantleCell newCell;
        switch (respawnMethod) {
            case 3:
                Vector3 newPos = UnityEngine.Random.onUnitSphere;
                while (SpawnIsTooCloseToAnotherCell(newPos)) {
                    Debug.Log("Retry respawn - random location too close to another cell! Trying again...");
                    respawnsThisFrame += 1;
                    newPos = UnityEngine.Random.onUnitSphere;
                }
                Debug.Log("Respawned: " + respawnMethods[3]);
                newCell = new MantleCell(newPos, this.planet, defaultRespawnStrength);
                mantleCells.Add(newCell);
                newCell.color = region_colors[lastColor];
                lastColor = (lastColor + 1) % region_colors.Length;
                respawnsThisFrame += 1;
                return newCell;
            case 4:
                while (SpawnIsTooCloseToAnotherCell(vertsNear1[respawnsThisFrame])) {
                    Debug.Log("Retry respawn - index " + respawnsThisFrame + "too close to another cell! Trying again...");
                    respawnsThisFrame += 1;
                }
                Debug.Log("Respawned: " + respawnMethods[4]);
                newCell = new MantleCell(vertsNear1[respawnsThisFrame], this.planet, defaultRespawnStrength);
                mantleCells.Add(newCell);
                newCell.color = region_colors[lastColor];
                lastColor = (lastColor + 1) % region_colors.Length;
                respawnsThisFrame += 1;
                return newCell;
            case 5:
                while (SpawnIsTooCloseToAnotherCell(vertsLargest[respawnsThisFrame])) {
                    Debug.Log("Retry respawn - index " + respawnsThisFrame + "too close to another cell! Trying again...");
                    respawnsThisFrame += 1;
                }
                Debug.Log("Respawned: " + respawnMethods[5]);
                newCell = new MantleCell(vertsLargest[respawnsThisFrame], this.planet, defaultRespawnStrength);
                mantleCells.Add(newCell);
                newCell.color = region_colors[lastColor];
                lastColor = (lastColor + 1) % region_colors.Length;
                respawnsThisFrame += 1;
                return newCell;
            default:
                return null;
        }
        
    }


    public Dictionary<int, string> strengthUpdateMethods = new Dictionary<int, string> { { 0, "Spherical Area" },
                                                                                         { 1, "Circular Area" }};
    float CalculateNewCellStrength(MantleCell cell, float areaFactor) {
        float targetArea = areaFactor * cell.Area;
        float radius = cell.Planet.radius;
        switch (strengthUpdateMethod) {
            case  0:
                return radius * Mathf.Acos(1 - Mathf.Max(targetArea / (2 * Mathf.PI * radius * radius),-0.99f));
            case 1:
                return Mathf.Min(Mathf.Sqrt(targetArea / Mathf.PI), 0.99f * Mathf.PI * radius);

            default:
                return 0f;

        }
    }

    Vector3[] vertsLargest;
    public Vector3[] vertsNear1;
    
    void RecalculateVertexScoreRankings() {
        vertsLargest = planetIntersectionVertices.ToArray();
        Array.Sort(planetIntersectionVertexScores.ToArray(), vertsLargest);
        Array.Reverse(vertsLargest);

        vertsNear1 = planetIntersectionVertices.ToArray();

        float[] scoreNear1 = planetIntersectionVertexScores.ToArray();
        for (int i = 0; i < scoreNear1.Length; i++) {
            scoreNear1[i] = Mathf.Abs(scoreNear1[i] - 1);
        }
        Array.Sort(scoreNear1, vertsNear1);
    }

    bool SpawnIsTooCloseToAnotherCell(Vector3 proposedSpawn, float threshold = 0.0006f) {// approx. within 2 degress -> fail.
        float limit = 1f - threshold;   
        Vector3 norm = proposedSpawn.normalized;
        foreach (var cell in mantleCells) {
            if( Vector3.Dot(norm, cell.PlanetPosition.normalized) > limit) {
                return true;
            }
        }

        return false;
    }

    
    // Obsolete, as now each cell builds its own mesh.
    /*
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
    */

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
        // where the θs are the angle from the arbitrary point, P, to the centre of circle i, P_i, and the angular radius of circle i, θ_i = r_i/R.
        // i.e instead of caring about the radius of a circle r_i, better to store the angle of a circle θ_i
        // 0 < θ_i < π/2


        // TODO: currently breaks when a cell covers more than half the sphere. Figure out solution, or ensure a cell can never be larger than a hemisphere.


        //1. Find cos( θ(P,P_i) ) using dot product
        float top = Vector3.Dot(cell.PlanetPosition.normalized, pos.normalized);
        float bottom = cell.cosθ;

        //negate, as code currently looks for a 'strength' i.e. largest wins, rather than a 'distance' i.e. smallest wins.
        // for some reason it doesn't want the negation? don't quite understand why tbh...
        // Right. In Euclidean, the score d²-r² is clearly negative for r>d, i.e. points within the circle.
        // But on Sphere, cos(θ) is always positive for 0 < θ < π/2. As an evaluated point gets closer to the centre of the cell, the numerator goes to 1.
        // I.e. no need to negate, as the score already decreases as you move away from the circle.
        return top / bottom;
        
    }


    // TODO: Should probably move to the MantleCellRenderer class at some point...
    // Only reason not to is it seems a little wasteful to have a whole extra Mesh on each CellRenderer, just to hold a single basic line :/
    // Maybe combine the line mesh with the circle mesh?
    public void DrawLinesToCoM(List<MantleCell> cells) {
        int regions = 0;
        for (int i = 0; i < cells.Count; i++) {
            var cell = cells[i];
            cell.CalculateCentroid();
            cell.CalculateArea();
            if (cell.HasRegion) {
                regions++;

                // Find or Make LineDrawers for each cell with a region.
                if (regions <= dirLines.Count) {
                    dirLines[regions - 1].GlobeLine(cell.PlanetPosition, cell.Centroid, 0.02f, Color.white, cell.Planet);
                }
                else { dirLines.Add(LineDrawer.NewGlobeLine(cell.PlanetPosition, cell.Centroid, 0.02f, Color.white, cell.Planet)); }
            }
        }


        if (lineHolder == null) { lineHolder = LineDrawer.GetLineHolder(this.gameObject, "line_holder"); }
        MeshFilter meshFilter = lineHolder.GetComponent<MeshFilter>(); 

        CombineInstance[] meshArray = new CombineInstance[regions];
        for (int i = 0; i < regions; i++) {
            meshArray[i].mesh = dirLines[i].mesh;
            meshArray[i].transform = transform.localToWorldMatrix;
        }

        if (meshFilter.sharedMesh == null) { meshFilter.sharedMesh = new Mesh(); }
        else { meshFilter.sharedMesh.Clear(); }
        meshFilter.sharedMesh.CombineMeshes(meshArray);
    }

    public void BuildPowerDiagramBoundaries(MantleCell[] cells) {
        // http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.95.3444&rep=rep1&type=pdf
        // stores triangle vertex data from the dual convex hull.
        // each three points stores a face from the dual hull - i.e. where 3 half-spaces from the mantleCells will form a point on their convex hull.
        int[] cellIntersections; List<Vector3> normals;
        (cellIntersections, normals) = FindDualConvexHull(cells);

        /*
        Debug.Log("Program found " + cellIntersections.Length + " intersections");
        for (int i = 0; i < cellIntersections.Length; i++) {
            Debug.Log("Intersection " + i + ": " + cellIntersections[i]);
        }*/

        List <Vector3>[] cellPoints = new List<Vector3>[cells.Length];
        for (int i = 0; i < cells.Length;i++){ cellPoints[i] = new List<Vector3>(); }
        this.planetIntersectionVertices.Clear();
        this.planetIntersectionVertexScores.Clear();
        
        // for each face in dual space, calculate corresponding intersection in real space, and add it to cellPoints list for each cell.
        // In principle this could error if all three points lie on a great-circle of the sphere, but for any reasonable number of regions, this won't happen.
        // TODO: fix for 3 verts on great circle case.
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

            if (!noError) {
                Debug.LogError("Unable to find intersection point for three planes!" + plane1 + ", " + plane2 + ", " + plane3);
                Debug.LogError("Strengths: " + cell1.strength + ", " + cell2.strength + ", " + cell3.strength);
            }

            // check if intersection point is on wrong side of globe (e.g. all cells on northern hemisphere still has intersections on southern hemisphere)
            Vector3 normal = normals[3 * i];
            if(Vector3.Dot(normal,intersection) < 0f) { intersection *= -1f; }

            intersection.Normalize();

            cellPoints[c1].Add(intersection);
            cellPoints[c2].Add(intersection);
            cellPoints[c3].Add(intersection);

            // Also record where the vertices are, and what the cells' 'distance' to that point are:
            this.planetIntersectionVertices.Add(intersection);
            this.planetIntersectionVertexScores.Add(GetCellStrength(cell1, intersection));
        }

        // Need this section to handle orphaned mantleCellRenderers from the onValidate calls.
        // Wasn't a problem when MantleManager held its own list of the Renderers, but trying to move away from that.

        MantleCell cell;
        for (int i = 0; i < mantleCells.Count; i++) {
            cell = mantleCells[i];
            Vector3[] boundaryVertices = cellPoints[i].ToArray();

            if (!cell.HasRenderer) {
                if (unassignedMantleCellRenderers.Count > 0) {
                    //Debug.Log("Reassigning Renderer");
                    cell.SetRenderer(unassignedMantleCellRenderers[0]);
                    cell.Renderer.Reset(mantleCells[i], boundaryVertices);

                    unassignedMantleCellRenderers.RemoveAt(0);
                }
                else {
                    Debug.Log("Creating new cell_renderer for cell " + i);
                    GameObject cellObj = new GameObject("cell_renderer");
                    cellObj.transform.parent = transform;

                    MantleCellRenderer cellRenderer = cellObj.AddComponent<MantleCellRenderer>();
                    cellRenderer.Setup(mantleCells[i], boundaryVertices);
                }
            }
            else {
                cell.Renderer.Reset(mantleCells[i], boundaryVertices);
            }

            cell.Renderer.DrawCellCircle(Color.black,0.03f);
            cell.Renderer.DrawCellBoundary(Color.gray,0.03f);
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
