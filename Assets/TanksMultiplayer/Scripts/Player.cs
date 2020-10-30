
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;

namespace BladesOfBellevue
{          
    /// <summary>
    /// Networked player class implementing movement control and shooting.
	/// Contains both server and client logic in an authoritative approach.
    /// </summary> 
	public class Player : NetworkBehaviour
    {

        #region Variables

        #region Head Rotation

        [HideInInspector]
        [SyncVar(hook = "OnHeadRotation")]
        public int headRotation;

        /// <summary>
        /// Body part to rotate with look direction.
        /// </summary>
        public Transform head;

        //limit for sending turret rotation updates
        protected float sendRate = 0.1f;

        //timestamp when next rotate update should happen
        protected float nextRotate;

        #endregion

        #region Last Killed Me

        /// <summary>
        /// Last player gameobject that killed this one.
        /// </summary>
        [HideInInspector]
        public GameObject killedBy;

        //reference to this rigidbody
        #pragma warning disable 0649
        protected Rigidbody rb;
        #pragma warning restore 0649

        #endregion

        #region Pathing and Movement

        // pathing
        protected PathManager pathManager;
        public List<Node> path;

        // movement
        protected Vector3 nextGoalPos;
        protected Vector3 finalGoalPos;
        protected Node finalGoalNode;
        public int currentGoalNode;

        public float moveSpeed = 1;

        #endregion

        #region Teleporting

        public bool teleporting = false;
        public bool teleported = false;
        public float teleportLength = 0.5f;
        public float teleportTime;
        public Node teleportBeginning;
        public Node teleportDestination;

        [SyncVar]
        public District currentDistrict;

        #endregion

        #endregion

        #region Start, Awake, etc. 

        //called before SyncVar updates
        void Awake()
        {
            //saving maximum health value
            //before it gets overwritten by the network
            //maxHealth = health;



        }

        protected virtual void Start()
        {
            pathManager = gameObject.transform.GetComponent<PathManager>();
            rb = GetComponent<Rigidbody>();
        }

        #endregion

        #region Pathing and Movement

        protected void RestartNodeLists()
        {
            currentGoalNode = 0;
            nextGoalPos = path[0].transform.position;
            finalGoalNode = path[path.Count - 1];
            finalGoalPos = path[path.Count - 1].transform.position;
        }

        public bool CompareLists(List<Node> list1, List<Node> list2)
        {
            var firstNotSecond = list1.Except(list2).ToList();
            var secondNotFirst = list2.Except(list1).ToList();

            return (firstNotSecond.Count == 0 && secondNotFirst.Count == 0);
        }

        protected void MoveCharacter()
        {
            if (!teleporting)
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
            else
            {
                if (!teleported)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        teleportBeginning.teleporterNodeHelper.transform.position,
                        moveSpeed * Time.deltaTime);
                    if (Time.time > teleportTime)
                    {
                        Teleport();
                    }
                }
                else
                {
                    transform.position = Vector3.MoveTowards(
                                            transform.position,
                                            teleportDestination.transform.position,
                                            moveSpeed * Time.deltaTime);
                    if ((transform.position - teleportDestination.transform.position).magnitude < 0.1)
                    {
                        ReachedNextNode();
                        teleporting = false;
                        teleported = false;
                    }
                }

            }
        }

        protected virtual void ReachedNextNode()
        {
            if (path.Count > 0)
            {
                if (path[currentGoalNode].nodeType == Node.NodeType.portal && !teleporting)
                {
                    teleporting = true;
                    teleportTime = Time.time + teleportLength;
                    teleportBeginning = path[currentGoalNode];
                    teleportDestination = path[currentGoalNode].teleporterDestinationNode;
                }
                if ((transform.position - finalGoalPos).magnitude < 0.1)
                {
                    path.Clear();
                }
                else
                {
                    if (currentGoalNode < path.Count - 1)
                    {
                        currentGoalNode++;
                        nextGoalPos = path[currentGoalNode].transform.position;
                    }
                }
            }            
        }

        #endregion

        #region Teleporting

        public virtual void Teleport()
        {
            transform.position = teleportDestination.teleporterNodeHelper.transform.position;
            teleported = true;
        }

        #endregion

        #region Stopping and Talking

        //public void 

        #endregion

        #region Dying and Respawning

        [Server]
        public virtual void GetKilled(HumanPlayer player)
        {

        }

        #endregion

        #region Rotating Head

        //rotates turret to the direction passed in
        protected void RotateHead(Vector2 direction = default(Vector2))
        {
            //don't rotate without values
            if (direction == Vector2.zero)
                return;

            //get rotation value as angle out of the direction we received
            //int newRotation = (int)(Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)).eulerAngles.y + camFollow.camTransform.eulerAngles.y);
            int newRotation = (int)(Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)).eulerAngles.y);

            //limit rotation value send rate to server:
            //only send every 'sendRate' seconds and skip minor incremental changes
            if (Time.time >= nextRotate && (Mathf.Abs(Mathf.DeltaAngle(headRotation, newRotation)) > 5))
            {
                //set next update timestamp and send to server
                nextRotate = Time.time + sendRate;
                headRotation = newRotation;
                if (isClient) CmdRotateHead(newRotation);
            }

            head.rotation = Quaternion.Euler(0, newRotation, 0);
        }

        //Command telling the server the updated turret rotation
        [Command]
        protected void CmdRotateHead(int value)
        {
            headRotation = value;
        }

        //hook for updating turret rotation locally
        protected void OnHeadRotation(int oldValue, int newValue)
        {
            //ignore value updates for our own player,
            //so we can update the rotation server-independent
            if (isLocalPlayer) return;

            headRotation = newValue;
            head.rotation = Quaternion.Euler(0, headRotation, 0);
        }

        #endregion

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireCube(gameObject.transform.position, gameObject.transform.lossyScale);
        }

    }
}
