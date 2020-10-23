using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{

    public enum NodeType
    {
        filler,
        junction,
        portal
    }

    public NodeType nodeType;

    // some lists
    public List<Node> neighbors;

    // methods for PathManager
    public Node previous {
        get;
        set;
    }
    public float cost {
        get;
        set;
    }

    // Teleporter Code
    public Node teleporterDestinationNode;
    private List<Player> receivedPlayers = new List<Player>();

    void Awake()
    {
    }
    void Start()
    {
    }

    private void OnTriggerEnter2D(Collider2D c)
    {
        if (c.tag == "Player" && nodeType == NodeType.portal)
        {
            Player player = c.transform.gameObject.GetComponent<Player>();
            if (!receivedPlayers.Contains(player))
            {
                teleporterDestinationNode.receivedPlayers.Add(player);
                player.GettingTeleported(this);
                player.gameObject.transform.position = teleporterDestinationNode.gameObject.transform.position;
            } else
            {
                receivedPlayers.Remove(player);
            }            
        }
    }

    private void OnTriggerExit2D(Collider2D c)
    {

    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(gameObject.transform.position, gameObject.transform.lossyScale);

        if (neighbors == null)
            return;
        Gizmos.color = Color.red;
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null)
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
    }
}
