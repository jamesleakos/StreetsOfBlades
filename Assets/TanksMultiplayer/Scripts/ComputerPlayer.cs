using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;


namespace BladesOfBellevue
{
    public class ComputerPlayer : Player
    {
        [Server]
        protected override void Start()
        {
            base.Start();
            ChooseNewPath();
        }
        // Update is called once per frame

        void Update()
        {
            if (!isServer) return;

            MoveCharacter();
            if (path.Count() == 0 || (transform.position - finalGoalPos).magnitude <= 0.1)
            {
                ChooseNewPath();
            }
        }

        public override void ReachedNextNode()
        {
            base.ReachedNextNode();
            if (path.Count() == 0)
            {
                ChooseNewPath();
            }
        }       

        public Node ChooseGoalNode ()
        {
            List<Node> nodes = GameObject.FindObjectsOfType<Node>().ToList();
            List<Node> filteredNodes = nodes.FindAll(x => x.nodeType != Node.NodeType.portal);
            Node newNode = filteredNodes[Random.Range(0, filteredNodes.Count)];

            // Here we can place logic for choosing Node by type of Character

            return newNode;
        }

        private void ChooseNewPath()
        {
            Node newNode = ChooseGoalNode();

            path = pathManager.FindShortestPath(newNode.gameObject.transform.position);
            if (path.Count > 0)
            {
                RestartNodeLists();
            }
        }

        #region Teleporting

        private void ChangeDistrict(District district)
        {
            currentDistrict = district;
        }

        public override void OnTeleport (Node node)
        {
            base.OnTeleport(node);
        }

        #endregion

        #region Player Interaction


        // OVERRIDES

        public override bool AskToStopToTalk(Player player)
        {
            Debug.Log("AskToStopToTalk triggered");
            ChangePlayerBehavior(PlayerBehaviorState.standing);
            TargetSetTalkMenuOn(player.gameObject.GetComponent<NetworkIdentity>().connectionToClient);
            return true;
        }

        [TargetRpc]
        private void TargetSetTalkMenuOn (NetworkConnection target)
        {
            SetTalkMenuOn();
        }

        #endregion

        #region Death and Respawn

        [Server]
        public override void GetKilled(HumanPlayer player)
        {

        }

        //called on all clients on both player death and respawn
        //only difference is that on respawn, the client sends the request
        [ClientRpc]
        protected virtual void RpcRespawn(short senderId)
        {

        }

        #endregion


    }
}
