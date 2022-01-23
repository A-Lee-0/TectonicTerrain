using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MantleManager : MonoBehaviour
{
    public List<MantleCell> mantleCells;
    public Planet planet;
    public Mesh[] meshes;

    Color[] region_colors = { Color.red, Color.blue, Color.yellow, Color.magenta, Color.green, Color.cyan };


    // Runs when unity compiles the script (i.e. in edit mode, not play)
    private void OnValidate() {

        // having nullreference exceptions running it here - call it from planet instead.
        //DebugSetup();
    }

    public void DebugSetup() {
        if (mantleCells == null) { mantleCells = new List<MantleCell>(); }
        mantleCells.Clear();
        
        planet = FindObjectOfType<Planet>();
        

        mantleCells.Add(new MantleCell(Vector3.right, planet, 4000f));
        mantleCells.Add(new MantleCell(Vector3.forward, planet, 1000f));

        Mesh[] meshes = planet.PlanetMeshes();
        this.meshes = meshes;

        PaintInfluenceOnMeshes(meshes);

        Debug.Log(GetCellStrength(mantleCells[0], new Vector3(1, 0, 1).normalized * 2f, true));

    }


    void PaintInfluenceOnMeshes(Mesh[] meshes) {
        foreach (Mesh mesh in meshes) {
            int vert_count = mesh.vertexCount;
            Color[] mesh_colors = new Color[vert_count];
            for (int i = 0; i < vert_count; i++) {
                var vert = mesh.vertices[i];

                float strongest = -1f;
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
        // The strength of a cell at a given position is just the strength of the cell divided by the perimeter of the slice through the sphere at the angule to the position.
        // I.e. the plane orthogonal to the cell's radial line that passes through the target point.
        // Can calculate using the dot product and pythagorus.
        // r = sqrt( R^2 - (c.p)^2 )
        


        Vector3 cell_pos = cell.Position();
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


    // Start is called before the first frame update
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
