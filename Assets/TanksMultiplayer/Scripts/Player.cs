using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Player : MonoBehaviour
{
    private Camera mainCamera;

    // pathing
    private PathManager pathManager;
    public List<Node> path;

    // movement
    private Vector3 nextGoalPos;
    private Vector3 finalGoalPos;
    private Node finalGoalNode;
    private int currentGoalNode;

    public float moveSpeed = 1;

    // teleportation
    public bool teleported = false;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        pathManager = gameObject.transform.GetComponent<PathManager>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            UpdatePathToClick();            
        }

        MoveCharacter();
    }

    void UpdatePathToClick ()
    {
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

        if (hit.collider != null)
        {
            if (hit.collider.tag == "Node")
            {
                Node goalNode = hit.collider.gameObject.GetComponent<Node>();
                Node currentNode = pathManager.FindClosestNode(transform.position);

                if (CompareLists(goalNode.neighbors,currentNode.neighbors))
                {
                    path.Clear();
                    path.Add(goalNode);
                    RestartNodeLists();
                }
                else
                {
                    path = pathManager.FindShortestPath(hit.collider.transform.position);
                    if (path.Count > 0)
                    {
                        RestartNodeLists();
                    }
                }           
            }
        }
    }

    void RestartNodeLists ()
    {
        currentGoalNode = 0;
        nextGoalPos = path[0].transform.position;
        finalGoalNode = path[path.Count - 1];
        finalGoalPos = path[path.Count - 1].transform.position;
    }

    public bool CompareLists (List<Node> list1, List<Node> list2)
    {
        var firstNotSecond = list1.Except(list2).ToList();
        var secondNotFirst = list2.Except(list1).ToList();

        return (firstNotSecond.Count == 0 && secondNotFirst.Count == 0);
    }

    void MoveCharacter()
    {
        if (path.Count > 0)
        {
            if ((transform.position - nextGoalPos).magnitude > 0.1 && (transform.position - finalGoalPos).magnitude > 0.1)
            {
                transform.position = Vector3.MoveTowards(transform.position, nextGoalPos, moveSpeed * Time.deltaTime);
            }
            else
            {
                ReachedNextNode();
            }
        }        
    }

    public void GettingTeleported (Node teleporter)
    {
        if (teleporter == finalGoalNode)
        {
            path.Clear();
        } else {
            ReachedNextNode();
        }
    }

    private void ReachedNextNode()
    {
        if ((transform.position - finalGoalPos).magnitude < 0.1)
        {
            path.Clear();
        } else
        {
            if (currentGoalNode < path.Count - 1)
            {
                currentGoalNode++;
                nextGoalPos = path[currentGoalNode].transform.position;
            }
        }
    }
    

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        Gizmos.DrawWireCube(gameObject.transform.position, gameObject.transform.lossyScale);
    }
}
