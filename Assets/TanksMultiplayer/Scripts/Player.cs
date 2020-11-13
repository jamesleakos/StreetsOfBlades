
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using TMPro;

namespace BladesOfBellevue
{          
    /// <summary>
    /// Networked player class implementing movement control and shooting.
	/// Contains both server and client logic in an authoritative approach.
    /// </summary> 
    /// 

	public class Player : NetworkBehaviour
    {

        #region Variables

        #region Scale and Appearance
        [Header("Body Parts")]

        /// <summary>
        /// Body part to rotate with look direction.
        /// </summary>
        public Transform head;

        public GameObject aliveBody;
        public GameObject deadBody;

        [HideInInspector]
        [SyncVar(hook = "OnHeadRotation")]
        public int headRotation;

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

        #region Character Type

        [Header("Citizen Attributes")]

        public TextMeshProUGUI nameText;

        public enum CitizenType
        {
            waif,
            merchant,
            trader,
            noble,
            farmer,
            seer,
            monk,
            beggar,
            spy
        }

        public CitizenType citizenType;

        public enum CitizenColor
        {
            blue,
            red,
            green
        }

        public CitizenColor citizenColor;

        #endregion

        #region Interaction Buttons and Systems

        [Space]
        [Header("Interaction Objects")]

        [SerializeField]
        private GameObject talkSelectedCircle;

        [SerializeField]
        private GameObject targetSelectedCircle;

        [SerializeField]
        private GameObject canKillCircle;

        [SerializeField]
        private GameObject rightClickMenu;

        [SerializeField]
        private GameObject talkMenu;
        #endregion

        #region Interaction

        public float talkDistance = 1.5f;

        protected bool canKill;

        #endregion

        #region Behavior State

        public enum PlayerBehaviorState
        {
            moving,
            standing,
            talking,
            dead
        }
        [HideInInspector]
        [SyncVar]
        public PlayerBehaviorState playerBehaviorState;

        #endregion

        #region Pathing and Movement
        
        // pathing
        protected PathManager pathManager;
        [HideInInspector]
        public List<Node> path = new List<Node>();

        // movement
        protected Vector3 nextGoalPos;
        protected Vector3 finalGoalPos;
        protected Node finalGoalNode;
        [HideInInspector]
        public int currentGoalNode;

        [Header("Movement")]
        public float walkSpeed = 2;
        public float runSpeed = 5;

        [HideInInspector]
        [SyncVar]
        public bool amRunning = false;

        #endregion

        [HideInInspector]
        [SyncVar]
        public GameObject talkingPlayer;

        #region Teleporting
        [HideInInspector]
        public bool teleporting = false;

        [HideInInspector]
        [SyncVar]
        public DistrictType currentDistrict;

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
            ClearAllMenus();
            aliveBody.SetActive(true);
            deadBody.SetActive(false);
        }

        protected virtual void Update()
        {
            CheckForAnimationState();
        }

        private void CheckForAnimationState ()
        {
            if (false)
            {
                RpcSwitchAnimationState();
            }
        }

        [ClientRpc]
        public void RpcSwitchAnimationState()
        {
            //Debug.Log("here we animate");
        }

        #endregion

        #region Pathing and Movement

        protected void MoveCharacter()
        {
            if (playerBehaviorState == PlayerBehaviorState.moving)
            {
                if (path.Count > 0)
                {
                    if ((transform.position - nextGoalPos).magnitude > 0.1 && (transform.position - finalGoalPos).magnitude > 0.1)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, nextGoalPos, (amRunning ? runSpeed : walkSpeed) * Time.deltaTime);
                    }
                    else
                    {
                        ReachedNextNode();
                    }
                }
            }
        }

        public virtual void OnTeleport (Node node)
        {
            currentDistrict = node.district;
        }

        public void RestartNodeLists()
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

        public virtual void ReachedNextNode()
        {
            if (path.Count > 0)
            {
                if ((transform.position - finalGoalPos).magnitude < 0.1 || path.Count == 1)
                {
                    ClearPath();
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
        public virtual void ClearPath ()
        {
            path.Clear();
        }

        #endregion

        #region Player Interaction
     
        // VIRTUAL METHODS

        public virtual bool AskToStopToTalk(Player player)
        {
            return false;
        }

        public virtual void DismissFromTalking()
        {
            talkingPlayer = null;
            ChangePlayerBehavior(PlayerBehaviorState.moving);
        }

        public virtual void ChangePlayerBehavior(PlayerBehaviorState behavState)
        {
            playerBehaviorState = behavState;
        }

        [TargetRpc]
        public void TargetSetCanKillCircle(NetworkConnection target)
        {
            SetCanKillCircle();
        }

        [TargetRpc]
        public void TargetSetTargetSelectedCircle(NetworkConnection target)
        {
            SetTargetSelectedCircle();
        }

        public virtual void GetKilled ()
        {
            ChangePlayerBehavior(PlayerBehaviorState.dead);            
        }

        [ClientRpc]
        public virtual void RpcGetKilled()
        {
            ClearAllMenus();
            aliveBody.SetActive(false);
            deadBody.SetActive(true);
        }

        #endregion

        #region Interaction Visuals

        public virtual void ClearAllMenus ()
        {
            talkSelectedCircle.SetActive(false);
            targetSelectedCircle.SetActive(false);
            canKillCircle.SetActive(false);
            rightClickMenu.SetActive(false);
            talkMenu.SetActive(false);
        }

        public void SetInteractionMenuOn()
        {
            ClearAllMenus();
            rightClickMenu.SetActive(true);
        }

        public void SetTalkMenuOn()
        {
            ClearAllMenus();
            talkMenu.gameObject.SetActive(true);
        }

        public void SetTargetSelectedCircle()
        {
            ClearAllMenus();
            targetSelectedCircle.SetActive(true);
        }

        public void SetTalkSelectedCircle()
        {
            ClearAllMenus();
            talkSelectedCircle.SetActive(true);
        }

        public void SetCanKillCircle()
        {
            ClearAllMenus();
            canKillCircle.SetActive(true);
        }

        #endregion

        #region Dying and Respawning

        [Server]
        public virtual void GetKilled(HumanPlayer player)
        {

        }

        #endregion

        #region Appearance and Scale

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
            if (playerBehaviorState == PlayerBehaviorState.dead)
            {
                Gizmos.color = Color.red;
            } else
            {
                Gizmos.color = Color.green;
            }

            Gizmos.DrawWireCube(gameObject.transform.position, gameObject.transform.lossyScale);
        }

    }
}
