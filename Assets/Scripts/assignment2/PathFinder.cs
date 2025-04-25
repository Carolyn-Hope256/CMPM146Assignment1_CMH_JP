using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using NUnit.Framework.Internal.Commands;
using Unity.VisualScripting.FullSerializer;
using System;
public class AStarEntry
{

    float cost = 0;
    float heuristic = 0;
    AStarEntry previousEntry = null;
    Wall wallTraversed = null;
    GraphNode node = null;
    public AStarEntry(GraphNode newnode,float newcost, float newheuristic, AStarEntry prevEntry,Wall prevWall){
        this.node = newnode;
        this.cost = newcost;
        this.heuristic = newheuristic;
        this.previousEntry = prevEntry;
        this.wallTraversed = prevWall;
    }
    public float return_total_cost(){
        return this.cost + heuristic;
    }
    public float return_cost(){
        return this.cost;
    }
    public float return_heuristic(){
        return this.heuristic;
    }
    public GraphNode return_node(){
        return this.node;
    }
}
public class PathFinder : MonoBehaviour
{
    // Assignment 2: Implement AStar
    //
    // DO NOT CHANGE THIS SIGNATURE (parameter types + return type)
    // AStar will be given the start node, destination node and the target position, and should return 
    // a path as a list of positions the agent has to traverse to reach its destination, as well as the
    // number of nodes that were expanded to find this path
    // The last entry of the path will be the target position, and you can also use it to calculate the heuristic
    // value of nodes you add to your search frontier; the number of expanded nodes tells us if your search was
    // efficient
    //
    // Take a look at StandaloneTests.cs for some test cases
    public static (List<Vector3>, int) AStar(GraphNode start, GraphNode destination, Vector3 target)
    {
        Debug.Log(start.GetID());
        Debug.Log(destination.GetID());
        // Implement A* here
        List<Vector3> path = new List<Vector3>() {};
        var startEntry = new AStarEntry(start,0,get_distance(start.GetCenter(),destination.GetCenter()),null,null);
        List<AStarEntry> priorityQueue = new List<AStarEntry>(){ startEntry };
        // return path and number of nodes expanded
        Debug.Log(priorityQueue[0].return_node().GetID());
        var counter = 100;
        while (priorityQueue[0].return_node().GetID() != destination.GetID()){
            counter --;
            if (counter <= 0){
                Debug.Log("force stopped");
                break;
            }
            //expand first element
            var firstelement = priorityQueue[0];
            var neighbours = firstelement.return_node().GetNeighbors();
            for (var i = 0; i < neighbours.Count; i++){
                Debug.Log("mainloop");

                
                //create AStarEntry
                var newnode = neighbours[i].GetNode();
                var newEntry = new AStarEntry(
                    newnode,
                    firstelement.return_cost() + get_distance(firstelement.return_node().GetCenter(),newnode.GetCenter()),
                    get_distance(newnode.GetCenter(),destination.GetCenter()),
                    firstelement,
                    neighbours[i].GetWall()
                    );
                //check if entry is already in list
                var needToBreak = false;
                for(var j = 0; j < priorityQueue.Count;j++){
                    if (priorityQueue[j].return_node().GetID() == newEntry.return_node().GetID()){
                        //if the new entry is more valuable, kick the old entry out and reinser the new one
                        if (newEntry.return_total_cost() < priorityQueue[j].return_total_cost()){
                            priorityQueue.RemoveAt(j);
                            needToBreak = false;
                        }
                        else{
                            needToBreak = true;
                        }
                        break;
                    }
                }
                if (needToBreak){
                    break;
                }
                //if entry isnt in list
                //put entry into list
                // var index = 0;
                // while (true){
                //     if(index >= priorityQueue.Count){
                //         priorityQueue.Insert(index,newEntry);
                //         break;
                //     }
                //     //check if new entry is larger than the entry in list
                //     if(newEntry.return_total_cost() <= priorityQueue[index].return_total_cost()){
                //         if (Math.Abs(newEntry.return_total_cost() - priorityQueue[index].return_total_cost()) <=  0.1){
                //             if (newEntry.return_heuristic() < priorityQueue[index].return_heuristic()){
                //                 priorityQueue.Insert(index,newEntry);
                //             }else{
                //                 Debug.Log("whoops!");
                //                 priorityQueue.Insert(index + 1,newEntry);
                //             }
                //         }else{
                //             priorityQueue.Insert(index,newEntry);
                //         }
                //         break;
                //     }
                //     index += 1;
                //     Debug.Log("sortloop");
                // }
                var hasPut = false;
                for (var ind = 0; ind < priorityQueue.Count; ind++){
                    
                    if(newEntry.return_total_cost() <= priorityQueue[ind].return_total_cost()){
                        //it newentry more valuable or equal
                        if (Math.Abs(newEntry.return_total_cost() - priorityQueue[ind].return_total_cost()) <=  0.1){
                            //they are equal
                            if (newEntry.return_heuristic() < priorityQueue[ind].return_heuristic()){
                                //newentry has more valuable heuristic
                                priorityQueue.Insert(ind,newEntry);
                                hasPut = true;
                                break;
                            }
                            //else dont break, check next element
                        }else{
                            priorityQueue.Insert(ind,newEntry);
                            hasPut = true;
                            break;
                        }
                    }
                }
                if (hasPut == false)
                {
                    priorityQueue.Add(newEntry);
                }

                
            }
            //first item is destination now
            
        }
        Debug.Log("found item!");
        Debug.Log(priorityQueue.Count);
        return (path, 0);

    }

    public Graph graph;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public static float get_distance(Vector3 a, Vector3 b){
        return Vector3.Distance(a,b);
    }

    void Start()
    {
        EventBus.OnTarget += PathFind;
        EventBus.OnSetGraph += SetGraph;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetGraph(Graph g)
    {
        graph = g;
    }

    // entry point
    public void PathFind(Vector3 target)
    {
        if (graph == null) return;

        // find start and destination nodes in graph
        GraphNode start = null;
        GraphNode destination = null;
        foreach (var n in graph.all_nodes)
        {
            if (Util.PointInPolygon(transform.position, n.GetPolygon()))
            {
                start = n;
            }
            if (Util.PointInPolygon(target, n.GetPolygon()))
            {
                destination = n;
            }
        }
        if (destination != null)
        {
            // only find path if destination is inside graph
            EventBus.ShowTarget(target);
            (List<Vector3> path, int expanded) = PathFinder.AStar(start, destination, target);

            Debug.Log("found path of length " + path.Count + " expanded " + expanded + " nodes, out of: " + graph.all_nodes.Count);
            EventBus.SetPath(path);
        }
        

    }

    

 
}
