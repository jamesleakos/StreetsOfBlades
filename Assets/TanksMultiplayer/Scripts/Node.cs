using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Mirror;
using System.Linq;


namespace BladesOfBellevue {
    public class Node : MonoBehaviour
    {
        public enum NodeType
        {
            filler,
            junction,
            portal
        }

        public NodeType nodeType;

        public DistrictType district;

        // some lists
        public List<Node> neighbors;

        // Teleporter Code
        public TeleporterNodeHelper teleporterNodeHelper;
        [HideInInspector]
        public List<Player> teleportingPlayers = new List<Player>();

        // methods for PathManager
        public Node previous {
            get;
            set;
        }
        public float cost {
            get;
            set;
        }
        
        void Awake()
        {
        }
        void Start()
        {
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.tag == "Player" && nodeType == NodeType.portal)
            {
                Player player = col.gameObject.GetComponent<Player>();

                if (teleportingPlayers.Contains(player))
                {
                    HumanPlayer humanPlayer = player.gameObject.GetComponent<HumanPlayer>();
                    if (humanPlayer != null)
                    {
                        humanPlayer.path.Clear();
                        humanPlayer.path.Add(neighbors.Find(x => x.nodeType == NodeType.junction));
                        humanPlayer.RestartNodeLists();
                    }
                } else
                {
                    if (player.path[player.currentGoalNode] != this) return;

                    player.teleporting = true;
                    var teleporterDestinationNode = neighbors.Find(x => x.nodeType == NodeType.portal);
                    teleporterDestinationNode.teleportingPlayers.Add(player);
                    player.ReachedNextNode();
                    player.gameObject.transform.position = teleporterDestinationNode.gameObject.transform.position;
                    player.OnTeleport(teleporterDestinationNode);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D col)
        {
            if (col.tag == "Player")
            {
                Player player = col.gameObject.GetComponent<Player>();
                if (teleportingPlayers.Contains(player))
                {
                    teleportingPlayers.Remove(player);
                    player.teleporting = false;
                }
            }
        }

        void OnDrawGizmos()
        {
            if (nodeType != NodeType.filler)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.red;
                //Handles.Label(transform.position + new Vector3(-2, 1.5f, 0), transform.name, style);
            }
            
            if (nodeType == NodeType.filler)
            {
                Gizmos.color = Color.green;
            } else if (nodeType == NodeType.junction)
            {
                Gizmos.color = Color.red;
            } else
            {
                Gizmos.color = Color.blue;
            }
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
}

