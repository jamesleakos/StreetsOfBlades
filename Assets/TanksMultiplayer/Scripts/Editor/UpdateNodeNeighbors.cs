using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace BladesOfBellevue
{
    public class UpdateNodeNeighbors : MonoBehaviour
    {
        [MenuItem("Nodes/FullNodeUpdate")]
        public static void FullNodeUpdate()
        {
            DestroyFillerNodes();
            CleanUpNodeNeighbors();
            UpdateNeighbors();
            FillInNodeSpace();
            UpdateNeighbors();
        }

        [MenuItem("Nodes/DeleteFillerNodes")]
        public static void DeleteFillerNodes()
        {
            DestroyFillerNodes();
            CleanNodeNeighbors();
        }

        [MenuItem("Nodes/CleanNodeNeighbors")]
        public static void CleanNodeNeighbors()
        {
            CleanUpNodeNeighbors();
        }        

        [MenuItem("Nodes/UpdateNeighbors")]
        public static void UpdateNeighborNodes()
        {
            UpdateNeighbors();
        }

        public static void CleanUpNodeNeighbors()
        {
            List<Node> nodes = FindObjectsOfType<Node>().ToList();

            foreach (var node in nodes)
            {
                List<int> nodesToRemove = new List<int>();
                
                for (var i = 0; i < node.neighbors.Count; i++)
                {
                    if (node.neighbors[i] == null)
                    {
                        nodesToRemove.Add(i);
                    }
                }

                EditorGUI.BeginChangeCheck();

                for (var i = nodesToRemove.Count - 1; i >= 0; i--)
                {
                    node.neighbors.RemoveAt(nodesToRemove[i]);
                }

                Undo.RegisterCompleteObjectUndo(node, "Cleaned Node Neighbors");
                Undo.FlushUndoRecordObjects();
                EditorUtility.SetDirty(node);
            }
        }

        public static void UpdateNeighbors()
        {
            List<Node> nodes = FindObjectsOfType<Node>().ToList();

            for (int j = 0; j < nodes.Count; j++)
            {
                Node node = nodes[j];
                List<int> nodesToRemove = new List<int>();

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

                    Undo.RegisterCompleteObjectUndo(neighbor, "Added Node To Neighbor");
                    Undo.FlushUndoRecordObjects();
                    EditorUtility.SetDirty(neighbor);
                }
            }
        }

        public static void DestroyFillerNodes()
        {
            List<Node> nodesToDestroy = FindObjectsOfType<Node>().ToList();
            for (var i = 0; i < nodesToDestroy.Count; i++)
            {
                if (nodesToDestroy[i].nodeType == Node.NodeType.filler)
                {
                    DestroyImmediate(nodesToDestroy[i].gameObject);
                }
            }

            GameObject[] nodeHolders;
            nodeHolders = GameObject.FindGameObjectsWithTag("NodeHolder");

            foreach (var nodeHolder in nodeHolders)
            {
                DestroyImmediate(nodeHolder);
            }
        }

        public static void FillInNodeSpace()
        {
            List<Node> junctionNodes = FindObjectsOfType<Node>().ToList();
            // recursive iterate through, add to list, etc.
            List<Node> completedNodes = new List<Node>();

            List<GameObject> districtNodeHolders = new List<GameObject>();
            for (int i = 0; i < System.Enum.GetNames(typeof(DistrictType)).Length; i++)
            {
                GameObject districtNodeHolder = new GameObject();
                districtNodeHolder.transform.name = System.Enum.GetName(typeof(DistrictType), i) + " NodeHolder";
                districtNodeHolder.tag = "NodeHolder";
                districtNodeHolders.Add(districtNodeHolder);
            }

            foreach (var node in junctionNodes)
            {
                foreach (var neighbor in node.neighbors)
                {
                    if (!completedNodes.Contains(neighbor) && !(node.nodeType == Node.NodeType.portal || neighbor.nodeType == Node.NodeType.portal))
                    {
                        Vector3 distanceBetweenNodes = node.transform.position - neighbor.transform.position;
                        float nodePrefabWidth = 1.0f;
                        int nodesNeeded = (int)Mathf.Ceil(distanceBetweenNodes.magnitude / nodePrefabWidth) - 1;
                        Object nodePrefab = AssetDatabase.LoadAssetAtPath("Assets/TanksMultiplayer/Prefabs/Node.prefab", typeof(GameObject));

                        // make nodeholder
                        GameObject nodeHolder = new GameObject();
                        nodeHolder.transform.name = node.transform.name + " - " + neighbor.transform.name + " FillerNodes";
                        nodeHolder.tag = "NodeHolder";

                        for (var i = 0; i < nodesNeeded; i++)
                        {
                            //Instantiate prefab if it exists
                            if (nodePrefab != null)
                            {
                                GameObject newNode = (GameObject)PrefabUtility.InstantiatePrefab(nodePrefab, nodeHolder.transform);
                                newNode.transform.position = neighbor.transform.position + (distanceBetweenNodes / ((float)nodesNeeded + 1.0f)) * (i + 1);
                                Node newNodeNode = newNode.GetComponent<Node>();
                                newNodeNode.neighbors = new List<Node>();
                                newNodeNode.neighbors.Add(node);
                                newNodeNode.neighbors.Add(neighbor);
                                newNodeNode.nodeType = Node.NodeType.filler;
                                newNodeNode.district = node.district;
                            }
                            else { Debug.Log("Node Prefab was null. Maybe update the path??"); }                            
                        }

                        nodeHolder.transform.parent = districtNodeHolders[(int)node.district].transform;
                    }
                }

                completedNodes.Add(node);
            }
        }
    }
}
