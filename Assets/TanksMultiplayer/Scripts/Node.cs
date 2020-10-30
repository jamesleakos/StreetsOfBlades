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

        public District district;

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

        public TeleporterNodeHelper teleporterNodeHelper;

        void Awake()
        {
        }
        void Start()
        {
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

