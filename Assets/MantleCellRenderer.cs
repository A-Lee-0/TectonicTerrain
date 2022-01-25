using UnityEngine;
using UnityEditor;

public class MantleCellRenderer : MonoBehaviour
{

    /// <summary>
    /// The MantleCell to be rendered
    /// </summary>
    MantleCell cell;
    /// <summary>
    /// Array of vertices comprising the boundary of the MantleCell.
    /// </summary>
    Vector3[] vertices;
    /// <summary>
    /// Array of MantleCells sharing a boundary with this MantleCell.
    /// </summary>
    MantleCell[] neighbours;

    public MantleCellRenderer(MantleCell cell, Vector3[] boundaryVertices,MantleCell[] neighbouringCells) {
        this.cell = cell;
        this.vertices = boundaryVertices;
        this.neighbours = neighbouringCells;
    }


}