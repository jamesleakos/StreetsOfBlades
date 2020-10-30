using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;

namespace BladesOfBellevue
{
    public class PathManager : MonoBehaviour
    {
        public List<Node> pathList;
        private Player player;
        Node currentNode;
        Node endNode;

        void Awake()
        {
        }

        void Start()
        {
            player = gameObject.GetComponent<Player>();
        }

        public List<Node> FindShortestPath(Vector3 destination)
        {
            //List<Node> pathList = new List<Node>();
            pathList = new List<Node>();

            currentNode = FindClosestNode(gameObject.transform.position);
            endNode = FindClosestNode(destination);
            if (currentNode == null || endNode == null || currentNode == endNode)
                return pathList;
            var openList = new SortedList<float, Node>();
            var closedList = new List<Node>();
            openList.Add(0, currentNode);
            currentNode.previous = null;
            currentNode.cost = 0f;
            while (openList.Count > 0)
            {
                currentNode = openList.Values[0];
                openList.RemoveAt(0);
                var cost = currentNode.cost;
                closedList.Add(currentNode);
                if (currentNode == endNode)
                {
                    break;
                }
                foreach (var neighbor in currentNode.neighbors)
                {
                    if (neighbor)
                    {
                        if (closedList.Contains(neighbor) || openList.ContainsValue(neighbor))
                            continue;
                        neighbor.previous = currentNode;
                        if (neighbor.nodeType == Node.NodeType.portal && currentNode.nodeType == Node.NodeType.portal)
                        {
                            neighbor.cost = 0;
                        }
                        else
                        {
                            neighbor.cost = cost + (neighbor.transform.position - currentNode.transform.position).magnitude + 0.5f;
                        }
                        var distanceToTarget = (neighbor.transform.position - endNode.transform.position).magnitude;
                        float key = neighbor.cost + distanceToTarget;
                        while (openList.ContainsKey(key))
                        {
                            key = key + 0.001f;
                        }
                        openList.Add(key, neighbor);
                    }
                }
            }
            if (currentNode == endNode)
            {
                while (currentNode.previous != null)
                {
                    pathList.Insert(0, currentNode);
                    currentNode = currentNode.previous;
                }
            }

            return pathList;
        }

        public Node FindClosestNode(Vector3 target)
        {
            Node closest = null;
            float closestDist = Mathf.Infinity;
            foreach (Node node in FindObjectsOfType<Node>())
            {
                var dist = (node.gameObject.transform.position - target).magnitude;
                if (dist < closestDist)
                {
                    closest = node;
                    closestDist = dist;
                }
            }
            if (closest != null)
            {
                return closest;
            }
            return null;
        }

        void OnDrawGizmos()
        {
            if (Application.isPlaying && pathList.Count > 0)
            {
                Gizmos.color = Color.red;

                for (int i = 0; i < pathList.Count - 1; i++)
                {
                    Gizmos.DrawLine(pathList[i].transform.position, pathList[i + 1].transform.position);
                }
            }
        }

    }
}

