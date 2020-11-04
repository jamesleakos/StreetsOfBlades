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
        [Server]
        void Update()
        {
            MoveCharacter();
            if (path.Count() == 0 || (transform.position - finalGoalPos).magnitude <= 0.1)
            {
                ChooseNewPath();
            }
        }

        protected override void ReachedNextNode()
        {
            base.ReachedNextNode();
            if (path.Count() == 0)
            {
                ChooseNewPath();
            }
        }       

        private void ChooseNewPath()
        {
            List<Node> nodes = GameObject.FindObjectsOfType<Node>().ToList();
            List<Node> filteredNodes = nodes.Where(x => x.nodeType == Node.NodeType.junction).ToList();
            Node newNode = filteredNodes[Random.Range(0, filteredNodes.Count)];

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

        public override void Teleport()
        {
            base.Teleport();
            ChangeDistrict(teleportDestination.district);
        }

        #endregion

        #region Player Interaction


        // OVERRIDES

        public override bool AskToStopToTalk(Player player)
        {
            ChangePlayerBehavior(PlayerBehaviorState.standing);
            TurnOnTalkMenu();
            return true;
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
