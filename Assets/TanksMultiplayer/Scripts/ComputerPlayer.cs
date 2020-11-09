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
        #region Standing and Talking Among Bot Variables

        private bool talkingToOtherBot;
        private bool standingByMyself;

        // time vars
        private float nextTalkTime;
        public float minNextTalk;
        public float maxNextTalk;
        private float endTalkTime;
        public float minTalkLength;
        public float maxTalkLength;

        private float nextStandTime;
        public float minNextStand;
        public float maxNextStand;
        private float endStandTime;
        public float minStandLength;
        public float maxStandLength;

        // lots of other stuff inc
        public float eavesdropDistance;

        #endregion

        [ServerCallback]
        protected override void Start()
        {
            base.Start();
            ChooseNewPath();
            nextTalkTime = Random.Range(minNextTalk, maxNextTalk);
            nextStandTime = Random.Range(minNextStand, maxNextStand);
        }
        // Update is called once per frame

        protected override void Update()
        {
            base.Update();

            if (!isServer) return;

            if (talkingToOtherBot && Time.time > endTalkTime) EndTalkToOtherBot();
            if (standingByMyself && Time.time > endStandTime) EndStandingByMyself();

            if (Time.time > nextStandTime)
            {
                ChangePlayerBehavior(PlayerBehaviorState.standing);
                nextStandTime = Random.Range(minNextStand, maxNextStand);
                endStandTime = Random.Range(minStandLength, maxStandLength);
                standingByMyself = true;
            }

            MoveCharacter();
            if (path.Count() == 0 || (transform.position - finalGoalPos).magnitude <= 0.1)
            {
                ChooseNewPath();
            }
        }

        #region Player Interaction

        // OVERRIDES
        [ServerCallback]
        public override bool AskToStopToTalk(Player player)
        {
            ChangePlayerBehavior(PlayerBehaviorState.talking);
            TargetSetTalkMenuOn(player.gameObject.GetComponent<NetworkIdentity>().connectionToClient);
            return true;
        }

        [TargetRpc]
        private void TargetSetTalkMenuOn(NetworkConnection target)
        {
            SetTalkMenuOn();
        }

        [ServerCallback]
        private void GetScared()
        {
            if (talkingToOtherBot)
            {
                EndTalkToOtherBot();
                EndStandingByMyself();
            }
        }

        #endregion

        #region Talking to other bots

        [ServerCallback]
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!isServer) return;

            if (collision.tag == "Player")
            {
                ComputerPlayer computerPlayer = collision.gameObject.GetComponent<ComputerPlayer>();
                if (computerPlayer != null)
                {
                    if (Time.time > nextTalkTime)
                    {
                        nextTalkTime = Time.time + Random.Range(minNextTalk, maxNextTalk);
                        endTalkTime = Time.time + Random.Range(minTalkLength, maxTalkLength);

                        ChangePlayerBehavior(PlayerBehaviorState.talking);
                        talkingToOtherBot = true;

                        var currentScale = aliveBody.transform.localScale;
                        var newScale = new Vector3(
                                        (gameObject.transform.position.x - computerPlayer.gameObject.transform.position.x > 0 ? -1 : 1) * currentScale.x,
                                        1 * currentScale.y,
                                        1 * currentScale.z);
                        RpcSetTalkingScale(newScale);

                        computerPlayer.GetTalkedToByOtherBot(endTalkTime, this);
                    }
                }
            }
        }

        [ServerCallback]
        public void GetTalkedToByOtherBot (float endTalkTime, ComputerPlayer computerPlayer)
        {
            this.endTalkTime = endTalkTime;
            nextTalkTime = Time.time + Random.Range(minNextTalk, maxNextTalk);

            ChangePlayerBehavior(PlayerBehaviorState.talking);
            talkingToOtherBot = true;

            var currentScale = aliveBody.transform.localScale;
            var newScale = new Vector3(
                            (gameObject.transform.position.x - computerPlayer.gameObject.transform.position.x > 0 ? -1 : 1) * currentScale.x,
                            1 * currentScale.y,
                            1 * currentScale.z);
            RpcSetTalkingScale(newScale);
        }

        [ClientRpc]
        private void RpcSetTalkingScale(Vector3 newScale)
        {
            aliveBody.transform.localScale = newScale;
        }

        [ServerCallback]
        private void EndTalkToOtherBot()
        {
            talkingToOtherBot = false;
            ChangePlayerBehavior(PlayerBehaviorState.moving);
        }

        [ServerCallback]
        private void EndStandingByMyself()
        {
            standingByMyself = false;
            ChangePlayerBehavior(PlayerBehaviorState.moving);
        }
        #endregion

        #region Nodes and Pathing

        [ServerCallback]
        public override void ReachedNextNode()
        {
            base.ReachedNextNode();
            if (path.Count() == 0)
            {
                ChooseNewPath();
            }
        }

        [ServerCallback]
        public Node ChooseGoalNode()
        {
            List<Node> nodes = GameObject.FindObjectsOfType<Node>().ToList();
            List<Node> filteredNodes = nodes.FindAll(x => x.nodeType != Node.NodeType.portal);

            int adminWeight;
            int marketWeight;
            int monasteryWeight;
            int upscaleWeight;
            int otherWeight;

            if (citizenType == CitizenType.farmer)
            {
                adminWeight = 1;
                marketWeight = 5;
                monasteryWeight = 3;
                upscaleWeight = 2;
                otherWeight = 1;
            }
            else if (citizenType == CitizenType.merchant)
            {
                adminWeight = 3;
                marketWeight = 5;
                monasteryWeight = 2;
                upscaleWeight = 4;
                otherWeight = 1;
            }
            else if (citizenType == CitizenType.monk)
            {
                adminWeight = 1;
                marketWeight = 1;
                monasteryWeight = 5;
                upscaleWeight = 1;
                otherWeight = 1;
            }
            else if (citizenType == CitizenType.noble)
            {
                adminWeight = 5;
                marketWeight = 2;
                monasteryWeight = 3;
                upscaleWeight = 4;
                otherWeight = 1;
            }
            else if (citizenType == CitizenType.trader)
            {
                adminWeight = 2;
                marketWeight = 5;
                monasteryWeight = 5;
                upscaleWeight = 1;
                otherWeight = 1;
            }
            else
            {
                adminWeight = 1;
                marketWeight = 1;
                monasteryWeight = 1;
                upscaleWeight = 1;
                otherWeight = 1;
            }

            List<int> weights = new List<int>();
            foreach (var node in filteredNodes)
            {
                if (node.district == DistrictType.administrative) weights.Add(adminWeight);
                else if (node.district == DistrictType.market) weights.Add(marketWeight);
                else if (node.district == DistrictType.monastery) weights.Add(monasteryWeight);
                else if (node.district == DistrictType.upscale) weights.Add(upscaleWeight);
                else weights.Add(otherWeight);
            }

            List<int> cumulativeWeights = new List<int>();
            int cw = 0;
            foreach (int weight in weights)
            {
                cw = cw + weight;
                cumulativeWeights.Add(cw);
            }
            int pick = Random.Range(0, cw);
            int interval = cumulativeWeights.SkipWhile(p => p < pick).First();
            int indexOfInterval = cumulativeWeights.IndexOf(interval);
            Node newNode = filteredNodes[indexOfInterval];

            // Here we can place logic for choosing Node by type of Character

            return newNode;
        }

        [ServerCallback]
        private void ChooseNewPath()
        {
            Node newNode = ChooseGoalNode();

            path = pathManager.FindShortestPath(newNode.gameObject.transform.position);
            if (path.Count > 0)
            {
                RestartNodeLists();
            }
        }

        #endregion

        #region Teleporting

        private void ChangeDistrict(DistrictType district)
        {
            currentDistrict = district;
        }

        public override void OnTeleport (Node node)
        {
            base.OnTeleport(node);
        }

        #endregion        
    }
}
