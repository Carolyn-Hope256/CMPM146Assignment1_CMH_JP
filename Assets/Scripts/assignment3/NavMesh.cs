using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Threading.Tasks;

public class NavMesh : MonoBehaviour
{
    //[SerializeField] public GameObject markerprefab;
    // implement NavMesh generation here:
    //    the outline are Walls in counterclockwise order
    //    iterate over them, and if you find a reflex angle
    //    you have to split the polygon into two
    //    then perform the same operation on both parts
    //    until no more reflex angles are present
    //
    //    when you have a number of polygons, you will have
    //    to convert them into a graph: each polygon is a node
    //    you can find neighbors by finding shared edges between
    //    different polygons (or you can keep track of this while 
    //    you are splitting)
    public Graph MakeNavMesh(List<Wall> outline)
    {
        Graph g = new Graph();
        g.all_nodes = new List<GraphNode>();
        int idnum = 0;

        //List of exterior and interior walls that is iterated through to find cuts.
        //Interior walls are inserted after the wall that "called" them for sake of the algorithm.
        List<Wall> Walls = new List<Wall>();
        Walls.Clear();
        
        //Master list of actual exterior and interior walls. Append-only.
        //A wall's index in this list functions as a sort of ID number.
        List<Wall> Canon = new List<Wall>();
        Canon.Clear();
        
        //A list of int lists, each representing a polygon. Each integer entry coresponds to a wall entry in the master list.
        List<List<int>> Polygons = new List<List<int>>();
        Polygons.Clear();

        //Create the first polygon, and stock it with edge wall IDs
        //and the Canon list with the actual perimeter walls.
        Polygons.Add(new List<int>());
        for (int i = 0; i < outline.Count; i++)
        {
            Walls.Add(outline[i]);
            Canon.Add(outline[i]);
            Polygons[0].Add(i);
        }

        //Traverse along the list of walls, searching for reflex angles and cutting appropriately
        //Much of this code is probably unnecessary, left over from when I was still troubleshooting cuts
        for (int i = 0; i < Walls.Count; i++)
        {
            //Debug.DrawRay(Walls[i].start, Walls[i].direction * Walls[i].length, Color.green, 1);
            //await Task.Delay(1000);
            bool fanning = false;
            if((Walls[i].start - Walls[GetWallIndex(i, 1, Walls.Count)].start).magnitude < Mathf.Epsilon)
            {
                fanning = true;
            }
            
            //If we've arrived at a reflex angle...
            if (IsReflex(Walls[i], Walls[GetWallIndex(i, 1, Walls.Count)], fanning))
            {
                Debug.Log("Vertex at the end of wall " + i + " is a reflex angle!");
                //GameObject marker = (GameObject)Instantiate(markerprefab, walls[i].end, Quaternion.identity);
                //marker.transform.localScale = new Vector3(10, 10, 10);
                //marker.GetComponent<BreadcrumbController>().lifetime = 20;

                //Initialize candidate information.
                Vector3 candidate = new Vector3(0, 0, 0);
                float bestAngle = 180f;
                bool foundCandidate = false;
                
                //Skip ahead to the first vertex that could be drawn to and form a polygon, and begin searching for candidates.
                for(int j = 2; j < Walls.Count; j++)
                {
                    Wall cur = Walls[GetWallIndex(i, j, Walls.Count)];
                    
                    //A potential candidate vector must have a non-positive angle (so that further reflexes aren't created),
                    //favoring angles closer to 0, and must not overlap or intersect existing walls
                    float a = Vector3.SignedAngle(Walls[i].direction, (cur.end - Walls[GetWallIndex(i, 1, Walls.Count)].start).normalized, Vector3.up);
                    if(a <= 0 && Mathf.Abs(a) < bestAngle && IsValid(new Wall(Walls[GetWallIndex(i, 1, Walls.Count)].start, cur.end), Walls))//todo: check if proposed wall intersects/is a duplicate
                    {
                        Debug.DrawRay(Walls[i].end, Walls[i].direction * 10, Color.green, 1000);
                        Debug.DrawRay(Walls[i].end, (cur.end - Walls[i].end), Color.blue, 1000);
                        //Debug.Log("Found new best candidate after" + j + "jumps of angle " + a);
                        candidate = cur.end;
                        bestAngle = a;
                        foundCandidate = true;
                    }
                }
                
                
                //If candidate found, then slice
                if (foundCandidate)
                {
                    //Instantiate(markerprefab, candidate, Quaternion.identity);
                    
                    int jumps = 1;

                    
                    List<int> newPoly = new List<int>();
                    int cutPoly = 0;

                    Debug.Log("Creating Polygon " + Polygons.Count);

                    for (int j = 1; j < Walls.Count; j++)
                    {
                        jumps++;

                        Wall cur = Walls[GetWallIndex(i, j, Walls.Count)];
                        //Debug.DrawRay(cur.start, cur.direction * cur.length, Color.green, 1);
                        int cInd = GetCanonWallIndex(cur, Canon);
                        if(cInd < 0)
                        {
                            //Debug.Log("Couldn't find wall " + GetWallIndex(i, j, Walls.Count) );
                        }

                        newPoly.Add(cInd);

                        for (int p = 0; p < Polygons.Count; p++)
                        {
                            if (Polygons[p].Contains(cInd) && Mathf.Abs(cInd) < outline.Count)
                            {
                                Debug.Log("Cutting wall " + cInd + " from Polygon " + p);
                                cutPoly = p;
                                Polygons[p].Remove(cInd);
                            }
                        }


                        if ((cur.end - candidate).magnitude < Mathf.Epsilon)
                        {
                            break;
                        }
                        
                    }
                    

                    Canon.Add(new Wall(candidate, Walls[GetWallIndex(i, 1, Walls.Count)].start));
                    Walls.Insert(GetWallIndex(i, 1, Walls.Count), new Wall(candidate, Walls[GetWallIndex(i, 1, Walls.Count)].start));
                    
                    Debug.Log("Walls length: " + Walls.Count + ". Canon Length: " + Canon.Count);
                    //Polygons[cutPoly].Add(-1 * (Canon.Count - 1));
                    newPoly.Add(Canon.Count - 1);

                    string polycontents = "Created new poly " + Polygons.Count + ": ";
                    foreach (int w in newPoly)
                    {
                        polycontents += w + ", ";
                    }
                    Debug.Log(polycontents);
                    polycontents = "Old poly " + cutPoly + " now contains: ";
                    foreach (int w in Polygons[cutPoly])
                    {
                        polycontents += w + ", ";
                    }
                    Debug.Log(polycontents);

                    Polygons.Add(newPoly);
                    //g.all_nodes.Add(new GraphNode(idnum, Poly));
                    //idnum++;
                }
                //await Task.Delay(1000);



            }
        }

        //For each polygon created...
        for(int p = 0; p < Polygons.Count; p++)
        {
            string printPoly = "Polygon " + p + ": ";
            //Parse the contained keys into a true list of walls...
            List<Wall> TruePoly = new List<Wall>();
            for(int i = 0; i < Polygons[p].Count; i++)
            {
                int w = Polygons[p][i];
                bool flip = false;

                if(w < 0)
                {
                    w *= -1;
                    flip = true;
                }

                //Debug.Log(w);
                printPoly += w + ", ";
                if (flip)
                {
                    TruePoly.Add(FlipWall(Canon[w]));
                }
                else
                {
                    TruePoly.Add(Canon[w]);
                }
                
            }
            Debug.Log(printPoly);

            /*for(int i = 0; i < TruePoly.Count; i++) 
            {
                
            }*/

            //And add it to the graph.
            if(TruePoly.Count > 2)
            {
                //g.all_nodes.Add(new GraphNode(p, TruePoly));
            }
            
        }
        
        //Debug, draws Canon walls via rays
        for (int i = 0; i < Walls.Count; i++)
        {
            Debug.DrawRay(Canon[i].start, Canon[i].direction * Canon[i].length, Color.yellow, 5);
        }
        return g;
    }

    //Detects reflex angles. Imperfect, doesn't account for interior walls properly.
    //Results in over-cutting, which is preferable to the inverse.
    public bool IsReflex(Wall wall, Wall next, bool fan)
    {
        int co = 1;
        if (fan) {
            //co = -1;
        }
        return (co * Vector3.SignedAngle(wall.direction, next.direction, Vector3.up) > 0);
    }

    //Helper function which returns the index of the wall a given amount of jumps
    //Ahead of or behind the current wall
    public int GetWallIndex(int current, int jumps, int length)
    {
        int target = current + jumps;
        while (target >= length)
        {
            target -= length;
        }
        while (target < 0)
        {
            target += length;
        }

        return target;
    }

    public Wall NextWall(Wall wall, List<Wall> scope) {
        List<Wall> candidates = new List<Wall>();
        float bestAngle = 180;
        Wall best = null;
        foreach(Wall w in scope)
        {
            if((wall.end - w.start).magnitude < Mathf.Epsilon && Vector3.SignedAngle(w.direction, wall.direction, Vector3.up) < bestAngle)
            {
                best = w;
                bestAngle = Vector3.SignedAngle(w.direction, wall.direction, Vector3.up);

            }
        }
        return best;
    }

    //Given a wall, grabs it's index in the canon list
    public int GetMapWallIndex(Wall wall, List<Wall> walls)
    {

        for (int i = 0; i < walls.Count; i++)
        {
            if (wall.Same(walls[i]))
            {
                return i;
            }
        }

        Debug.LogError("Wall not in Walls!");
        return -1;

    }

    //Given a wall, grabs it's index in the canon list
    public int GetCanonWallIndex(Wall wall, List<Wall> canon) { 
        
        for (int i = 0; i < canon.Count; i++) {
            if (wall.Same(canon[i])) 
            { 
                return i;
            }
        }

        Debug.LogError("Wall not in canon!");
        return -1;

    }

    public Wall FlipWall(Wall wall) 
    { 
        return new Wall(wall.end, wall.start);
    }

    public bool IsValid(Wall wall, List<Wall> existing)
    {
        //Debug.Log("found potential new wall");
        for (int i = 0; i < existing.Count; i++)
        {
            if (wall.Crosses(existing[i]) && !SharesPoint(wall, existing[i]))
            {

                //Debug.Log("new wall crosses wall " + i + "!");
                return false;
            }
            if (wall.Same(existing[i]))
            {
                //Debug.Log("new wall is the same as wall " + i + "!");
                return false;
            }
        }
        return true;
    }

    public bool SharesPoint(Wall newWall, Wall oldWall){
        if((oldWall.start - newWall.start).magnitude < Mathf.Epsilon){
            return true;
        }
        if ((oldWall.end - newWall.start).magnitude < Mathf.Epsilon)
        {
            return true;
        }
        if ((oldWall.start - newWall.end).magnitude < Mathf.Epsilon)
        {
            return true;
        }
        if ((oldWall.end - newWall.end).magnitude < Mathf.Epsilon)
        {
            return true;
        }
        return false;
    }

    List<Wall> outline;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventBus.OnSetMap += SetMap;
    }

    // Update is called once per frame
    void Update()
    {
       

    }

    public void SetMap(List<Wall> outline)
    {
        Graph navmesh = MakeNavMesh(outline);
        /*MakeNavMesh(outline);
        Graph navmesh = new Graph();
        navmesh.all_nodes = new List<GraphNode>();*/
        if (navmesh != null)
        {
            Debug.Log("got navmesh: " + navmesh.all_nodes.Count);
            EventBus.SetGraph(navmesh);
        }
    }

    


    
}
