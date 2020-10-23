using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class UpdateNodeNeighbors : MonoBehaviour
{    
    [MenuItem("Tools/UpdateNodeNeighbors")]
    public static void Execute()
    {
        List<Node> nodes = new List<Node>();
        nodes = FindObjectsOfType<Node>().ToList();

        for (int j = 0; j < nodes.Count; j++)
        {
            Node node = nodes[j];
            List<int> nodesToRemove = new List<int>();

            //if (node.nodeType != Node.NodeType.filler)
            if (true)
            {
                for (int i = 0; i < node.neighbors.Count; i++)
                {
                    Node neighbor = node.neighbors[i];
                    if (neighbor != null)
                    {
                        if (!neighbor.neighbors.Contains(node))
                        {
                            neighbor.neighbors.Add(node);
                        }
                    }
                    else
                    {
                        nodesToRemove.Add(i);
                    }
                }

                for (int i = nodesToRemove.Count - 1; i >= 0; i--)
                {
                    nodesToRemove.RemoveAt(i);
                }
            }            
        }
    }
}