using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class GraphVisualizer : MonoBehaviour
{
    public GameObject NodeVisualizer;
    public List<GameObject> visualizers;
    public Graph graph;
    bool visible;

    void Clear()
    {
        foreach (var go in visualizers)
        {
            Destroy(go);
        }
        visualizers.Clear();
    }

    async void Show()
    {
        foreach (var n in graph.all_nodes)
        {
            DrawNode(n);
            await Task.Delay(2000);
            //Task.Delay(2000).ContinueWith(t => DrawNode(n));
        }
    }

    void DrawNode(GraphNode n)
    {
        GameObject nv = Instantiate(NodeVisualizer, Vector3.zero, Quaternion.identity);
        GraphNodeVisualizer gnv = nv.GetComponent<GraphNodeVisualizer>();
        gnv.SetGraphNode(n);
        visualizers.Add(nv);
    }

    public void ShowGraph(Graph g)
    {
        graph = g;
        if (visible)
        {
            Clear();
            Show();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        visible = true;
        visualizers = new List<GameObject>();
        EventBus.OnSetGraph += ShowGraph;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Toggle()
    {
        visible = !visible;
        if (visible)
        {
            Show();
        }
        else
        {
            Clear();
        }
    }
}
