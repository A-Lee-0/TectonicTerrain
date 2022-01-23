using UnityEngine;
using System.Collections;

public class Planet : MonoBehaviour
{
    public Vector3 position;
    public float radius;
    public Vector3 rotationAxis = Vector3.up;
    public float rotationPeriod = 60f;


    [Range(2, 100)]
    public int resolution = 30;

    //public string shader = "Standard";
    public Shader shader;

    //[SerializeField, HideInInspector]
    [SerializeField]
    MeshFilter[] meshFilters;
    TerrainFace[] terrainFaces;


    // Runs when unity compiles the script (i.e. in edit mode, not play)
    private void OnValidate() {
        Setup();

        //debug setup for MantleManager as it's erroring due to null reference exceptions for terrainFaces.
        FindObjectOfType<MantleManager>().DebugSetup();

    }

    void Setup() { 
        Initialise();
        GenerateMesh();
        
        if (GetComponent<SphereCollider>() == null) { gameObject.AddComponent<SphereCollider>(); }
        GetComponent<SphereCollider>().radius = radius;
    }


    void Initialise() {
        // if no shader, find one:
        if (shader = null) { shader = Shader.Find("Standard"); }


        if (meshFilters == null || meshFilters.Length == 0) {
            meshFilters = new MeshFilter[6];
        }
        terrainFaces = new TerrainFace[6];

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        for (int i = 0; i < 6; i++) {
            if (meshFilters[i] == null) {
                GameObject meshObj = new GameObject("planet_mesh");         // create new GameObject with name of "planet_mesh"
                meshObj.transform.parent = transform;                       // set the parent to this object

                meshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(shader);
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
            }

            terrainFaces[i] = new TerrainFace(meshFilters[i].sharedMesh, resolution, directions[i], radius);
        }
    }

    void GenerateMesh() {
        foreach (TerrainFace face in terrainFaces) {
            face.ConstructMesh();
        }

        
        //test vertex colors
        /*foreach (TerrainFace face in terrainFaces) {

            Mesh test = face.mesh;
            int pointCount = test.vertexCount;
            Color[] colors = { Color.red, Color.blue };
            Color[] pointColors = new Color[pointCount];
            for (int i = 0; i < pointCount; i++) {
                pointColors[i] = colors[i % 2];
            }
            test.SetColors(pointColors);
        }
        */
    }

    public TerrainFace[] PlanetFaces() {
        return terrainFaces;
    }

    public Mesh[] PlanetMeshes() {
        Debug.Log(terrainFaces);
        Mesh[] meshes = new Mesh[terrainFaces.Length];
        for (int i=0; i<terrainFaces.Length; i++) {
            meshes[i] = terrainFaces[i].mesh;
        }
        return meshes;
    }

}
