# TectonicTerrain
![Short example of project in Unity.](Images/demo2.gif)

## What Is This?
This project started as an attempt to make a tectonic plate driven terrain generator, hence the repository name.

However, it fairly quickly got side-tracked by my discovery of the Power Diagram, and its extension to the surface of a sphere, as detailed in [this paper by K. Sugihara](http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.95.3444&rep=rep1&type=pdf).
Instead, this has become a realtime implementation of the geometry described in the paper, with some user adjustable parameters for the growth, movement and creation of new cells on the sphere.

This has been created using Unity 2021, with custom meshes being created each frame to draw the N-sided spherical-polygon regions associated with each cell.

Computing the location of each corner for a cell's region is hard - a cell's region does not necessarily overlap with its circle, nor circle centre. 
However, dividing the spherical surface into regions can be shown to be equivalent to creating a 3D convex polyhedron where each cell contributes a plane intersecting its circle in 3D space.
These polyhedron edges can be projected onto the surface of the sphere to show the cell boundaries.

However, it can be made more efficient still by building the convex hull of the polyhedron's dual - i.e. for each polyhedron face, the dual has a vertex, and each polyhedron vertex has a dual face.
The dual-space vertices are easily calculated from the cell centres and radii, allowing the dual-space polyhedron to be computed more easily than its real-space counterpart.
The vertices on each dual-space face then tell us which real-space cells intersect to form it, which can then be used to compute the real-space intersection position.

[Many thanks to Oskar Sigvardsson for their implementation of the Quickhull algorithm in Unity C#](https://github.com/OskarSigvardsson/unity-quickhull/blob/master/Scripts/ConvexHullCalculator.cs).

## Problems.
There remain some problems with the code.

### 1. Cell's larger than π/2.
Using the mathematical framework from the paper above, there's no sensible behaviour for a cell whose angular radius approaches π/2.
The core scoring metric for a point and cell is the ratio of the cosine of the angle from the point to the cell's circle centre, and the cosine of the circle's angular width.

As the angular width of the circle goes to π/2, the scoring metric goes to infinity for points in the same hemisphere as the cell, and to -infinity for points on the far side.
Furthermore, the cell's corresponding point in dual-space also goes to infinity.

This means that for the points on the far hemisphere *any other cell*, no matter where on the sphere, so long as their angular width is θ < π/2, will be preferrable to the cell with θ = π/2.

For any reasonable 'ideal' of an infinitely smooth simulation, rather than the actual frame by frame simulation, there is a massive discontinuity in point scores as a cell transitions from θ < π/2 to θ > π/2.
This seems wrong.

Currently the angle can rise above π/2, which causes the dual-space vertex to suddenly be near infinite in the opposite direction.
Unsurprisingly this causes chaos as cells that should be stable on the far hemisphere are suddenly not part of the dual convex hull, and are removed.
It also seems to cause the cell itself to be removed, though I'm uncertain why.

### 2. Fewer than 4 cells.
Currently, the code exclusively uses the dual-space polyhedron to compute the cell region corners, as described above.
However, if there are fewer than 4 vertices, the dual-space polyhedron is not well defined, and the code errors, causing the simulation to crash.

3 cell, and even 2 cell geometries should be perfectly reasonable, but a special case would need to be implemented to handle these, as the dual-space polyhedron is simply not the correct solution.

### 3. MTTH spawning has no MTTH controls.
Currently, there are two broad categories of cell spawning methods implemented.
An 'instant' respawn, where a cell which has no corresponding region is immediately respawned at some new location on the sphere, and Mean Time to Happen (MTTH) spawning, where a new cell has some probability of spawning every dt, and cells who lose their region are destroyed.

It is the latter case that can cause problem 2 above, as the number of cells is able to decrease.

The only way to change the MTTH is to edit it in the source code, which means an MTTH which is too low will inevitably lead to problem 2, with no solution available to the user.

### 4. Instant respawn has no cell-count control.
The code currently creates 20 cells at the start. 
The only way to change the number of cells is to switch to an MTTH spawning, let some cells die spawn or die off, then switch back to Instant.

## Further developments.

1. Enforce cell θ < π/2 to prevent the cell spontaneous destruction when nearing half the sphere.
2. Implement specific logic to handle the 1,2,3 cell cases.
3. Add UI controls to set the number of cells for Instant, and the MTTH for MTTH respawns.
4. Add more interesting graphical effects than the current cell shading. Look into gpu shader code
5. Modify geometry code to allow dynamic tri count based on the angular width of the arc (large regions have obvious corners in the mesh).
6. Allow some user interaction with the cells themselves, e.g. dragging cell centres around, setting a cell's radius etc.

## Extensions.

1. Use the spherical power diagram as a base for the iterated prisoners dilemma. The radius corresponds to average score. Score weight from game with each neighbour depends on arc length of edge 

2. Tectonic Terrain generation, as originally planned.

