using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.InputSystem;

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

        //Master list of actual exterior and interior walls. Append-only.
        //A wall's index in this list functions as a sort of ID number.
        List<Wall> walls = new List<Wall>();

        //A list of int lists, each representing a polygon. Each integer entry coresponds to a wall entry in the master list.
        List<List<int>> Polygons = new List<List<int>>();

        //Create the first polygon, and stock it with edge wall IDs
        //and the canon list with the actual perimeter walls.
        Polygons.Add(new List<int>());
        for (int i = 0; i < outline.Count; i++)
        {
            walls.Add(outline[i]);
            Polygons[0].Add(i);
        }

        //Traverse along the list of walls, searching for reflex angles and cutting appropriately
        //Much of this code is probably unnecessary, left over from when I was still troubleshooting cuts
        for (int i = 0; i < walls.Count; i++)
        {

            bool fanning = false;
            if((walls[i].start - walls[GetWallIndex(i, 1, walls.Count)].start).magnitude < Mathf.Epsilon)
            {
                fanning = true;
            }

            //If we've arrived at a reflex angle...
            if (IsReflex(walls[i], walls[GetWallIndex(i, 1, walls.Count)], fanning))
            {
                Debug.Log("Vertex at the end of wall " + i + " is a reflex angle!");
                //GameObject marker = (GameObject)Instantiate(markerprefab, walls[i].end, Quaternion.identity);
                //marker.transform.localScale = new Vector3(10, 10, 10);
                //marker.GetComponent<BreadcrumbController>().lifetime = 20;

                Vector3 candidate = new Vector3(0, 0, 0);
                float bestAngle = 180f;
                bool foundCandidate = false;
                
                for(int j = 2; j < walls.Count; j++)
                {
                    Wall cur = walls[GetWallIndex(i, j, walls.Count)];
                    
                    float a = Vector3.SignedAngle(walls[i].direction, (cur.end - walls[GetWallIndex(i, 1, walls.Count)].start).normalized, Vector3.up);
                    if(a <= 0 && Mathf.Abs(a) < bestAngle && IsValid(new Wall(walls[GetWallIndex(i, 1, walls.Count)].start, cur.end), walls))//todo: check if proposed wall intersects/is a duplicate
                    {
                        Debug.DrawRay(walls[i].end, walls[i].direction * 10, Color.green, 1000);
                        Debug.DrawRay(walls[i].end, (cur.end - walls[i].end), Color.blue, 1000);
                        Debug.Log("Found new best candidate after" + j + "jumps of angle " + a);
                        candidate = cur.end;
                        bestAngle = a;
                        foundCandidate = true;
                    }
                }

                if (foundCandidate)
                {
                    //Instantiate(markerprefab, candidate, Quaternion.identity);
                    
                    List<Wall> Poly = new List<Wall>();
                    int jumps = 1;
                    for (int j = 1; j < walls.Count; j++)
                    {
                        jumps++;
                        Wall cur = walls[GetWallIndex(i, j, walls.Count)];
                        Poly.Add(cur);
                        if ((cur.end - candidate).magnitude < Mathf.Epsilon)
                        {
                            break;
                        }
                    }
                    walls.Insert(GetWallIndex(i, 1, walls.Count), new Wall(candidate, walls[GetWallIndex(i, 1, walls.Count)].start));
                    Poly.Add(walls[GetWallIndex(i, 1, walls.Count)]);
                    //Poly.Add(new Wall(candidate, walls[i].end));
                    //g.all_nodes.Add(new GraphNode(idnum, Poly));
                    //idnum++;
                }



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
                printPoly += Polygons[p][i] + ", ";
                TruePoly.Add(walls[Polygons[p][i]]);
            }

            //And add it to the graph.
            //g.all_nodes.Add(new GraphNode(p, TruePoly));
        }
        
        //Debug, draws canon walls via rays
        for (int i = 0; i < walls.Count; i++)
        {
            Debug.DrawRay(walls[i].start, walls[i].direction * walls[i].length, Color.yellow, 3000);
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

    public bool IsValid(Wall wall, List<Wall> existing)
    {
        Debug.Log("found potential new wall");
        for (int i = 0; i < existing.Count; i++)
        {
            if (wall.Crosses(existing[i]) && !SharesPoint(wall, existing[i]))
            {

                Debug.Log("new wall crosses wall " + i + "!");
                return false;
            }
            if (wall.Same(existing[i]))
            {
                Debug.Log("new wall is the same as wall " + i + "!");
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
        if (navmesh != null)
        {
            Debug.Log("got navmesh: " + navmesh.all_nodes.Count);
            EventBus.SetGraph(navmesh);
        }
    }

    


    
}
