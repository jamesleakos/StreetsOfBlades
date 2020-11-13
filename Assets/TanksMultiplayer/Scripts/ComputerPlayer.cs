using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;

namespace BladesOfBellevue
{
    public class BotMemory
    {
        public HumanPlayer humanPlayerActor;
        public Player.CitizenColor citizenColor;
        public Player.CitizenType citizenType;
        public DistrictType district;
        public float timeOccured;
        public enum MemoryType
        {
            murder,
            kickedWaif,
            changedClothes
        }
        public MemoryType memoryType;
        public Player.CitizenColor citizenColorOriginal;
        public Player.CitizenType citizenTypeOriginal;
    }
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

        #endregion

        #region Player Actions and Memory
        
        public List<BotMemory> memories = new List<BotMemory>();
        public List<GameObject> memoryBubbles = new List<GameObject>();
        public GameObject memoryBubblePrefab;
        public List<HumanPlayer> overhearingPlayers = new List<HumanPlayer>();

        public float overhearDistance = 10.0f;

        #endregion

        [ServerCallback]
        protected override void Start()
        {
            base.Start();
            ChooseNewPath();
            nextTalkTime = Time.time + Random.Range(minNextTalk, maxNextTalk);
            nextStandTime = Time.time + Random.Range(minNextStand, maxNextStand);
        }
        // Update is called once per frame

        protected override void Update()
        {
            base.Update();

            if (!isServer) return;

            if (talkingToOtherBot)
            {
                if (Time.time > endTalkTime) {
                    EndTalkToOtherBot();
                } else
                {
                    List<HumanPlayer> humanPlayers = FindObjectsOfType<HumanPlayer>().ToList();
                    List<HumanPlayer> nearPlayers = humanPlayers.FindAll(c => (c.gameObject.transform.position - gameObject.transform.position).magnitude < overhearDistance);
                    foreach (var np in nearPlayers)
                    {
                        if (!overhearingPlayers.Contains(np))
                        {
                            Debug.Log("Adding player to overhearing players and triggering create memories");
                            overhearingPlayers.Add(np);
                            TargetTalkAboutMemories(np.gameObject.GetComponent<NetworkIdentity>().connectionToClient);
                        }
                    }

                    var playersToRemove = new List<HumanPlayer>();
                    foreach (var op in overhearingPlayers)
                    {
                        if (!nearPlayers.Contains(op))
                        {
                            playersToRemove.Add(op);
                            TargetClearMemoryBubbles(op.gameObject.GetComponent<NetworkIdentity>().connectionToClient);
                        }
                    }
                    foreach (var ptr in playersToRemove)
                    {
                        overhearingPlayers.Remove(ptr);
                    }
                    playersToRemove.Clear();
                }
            }
            if (standingByMyself && Time.time > endStandTime) EndStandingByMyself();

            if (Time.time > nextStandTime)
            {
                ChangePlayerBehavior(PlayerBehaviorState.standing);
                nextStandTime = Time.time + Random.Range(minNextStand, maxNextStand);
                endStandTime = Time.time + Random.Range(minStandLength, maxStandLength);
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
            Debug.Log("Set Talk menu on");
            SetTalkMenuOn();
            TalkAboutMemories();
        }

        [TargetRpc]
        private void TargetTalkAboutMemories(NetworkConnection target)
        {
            TalkAboutMemories();
        }

        [Client]
        private void TalkAboutMemories ()
        {
            int count = 0;
            foreach (var memory in memories)
            {
                Debug.Log("creating memory number " + count.ToString());
                GameObject memBubble = Instantiate(memoryBubblePrefab);
                memBubble.GetComponent<MemoryBubble>().PopulateWithInfo(memory);
                memBubble.transform.position = gameObject.transform.position + new Vector3(0, 2 + (memBubble.transform.lossyScale.y + 0.5f) * count, 0);
                memoryBubbles.Add(memBubble);
                count++;
            }
        }
        [Client]
        public override void ClearAllMenus()
        {
            base.ClearAllMenus();
            ClearMemoryBubbles();
        }

        [TargetRpc]
        private void TargetClearMemoryBubbles(NetworkConnection target)
        {
            Debug.Log("TargetClearMemoryBubbles");
            ClearMemoryBubbles();
        }

        [ClientRpc]
        private void RpcClearMemoryBubbles()
        {
            Debug.Log("RpcClearMemoryBubbles");
            ClearMemoryBubbles();
        }

        [Client]
        private void ClearMemoryBubbles ()
        {
            Debug.Log("Clearing memory bubbles");
            foreach (var bub in memoryBubbles)
            {
                Destroy(bub);
            }
            memoryBubbles.Clear();
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

        public void AddMemory (HumanPlayer humanPlayerActor, BotMemory.MemoryType memoryType, CitizenColor citizenColorOriginal, CitizenType citizenTypeOriginal, float timeOccurred)
        {
            BotMemory newMemory = new BotMemory();
            newMemory.humanPlayerActor = humanPlayerActor;
            newMemory.citizenColor = humanPlayerActor.citizenColor;
            newMemory.citizenType = humanPlayerActor.citizenType;
            newMemory.district = humanPlayerActor.currentDistrict;
            newMemory.timeOccured = timeOccurred;
            newMemory.memoryType = memoryType;
            newMemory.citizenColorOriginal = citizenColorOriginal;
            newMemory.citizenTypeOriginal = citizenTypeOriginal;

            memories.Add(newMemory);
        }

        [Server]
        public void ReceiveEvent(HumanPlayer humanPlayerActor, BotMemory.MemoryType memoryType, CitizenColor citizenColorOriginal, CitizenType citizenTypeOriginal)
        {
            Debug.Log("Receive Event");
            //AddMemory(humanPlayerActor, memoryType, citizenColorOriginal, citizenTypeOriginal, Time.time);
            RpcAddMemory(humanPlayerActor.gameObject, memoryType, citizenColorOriginal, citizenTypeOriginal, Time.time);

            if (memoryType == BotMemory.MemoryType.murder)
            {
                GetScared();
            }
        }

        [ClientRpc]
        private void RpcAddMemory(GameObject humanPlayerActor, BotMemory.MemoryType memoryType, CitizenColor citizenColorOriginal, CitizenType citizenTypeOriginal, float timeOccurred)
        {
            Debug.Log("RpcAddMemory");
            AddMemory(humanPlayerActor.GetComponent<HumanPlayer>(), memoryType, citizenColorOriginal, citizenTypeOriginal, Time.time);
        }

        #endregion

        #region Talking to other bots and standing alone

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
                        Debug.Log("Talk to other bot");
                        nextTalkTime = Time.time + Random.Range(minNextTalk, maxNextTalk);
                        endTalkTime = Time.time + Random.Range(minTalkLength, maxTalkLength);

                        ChangePlayerBehavior(PlayerBehaviorState.talking);
                        talkingToOtherBot = true;

                        var currentScale = aliveBody.transform.localScale;
                        var newScale = new Vector3(
                                        (gameObject.transform.position.x - computerPlayer.gameObject.transform.position.x > 0 ? -1 : 1) * Mathf.Abs(currentScale.x),
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
            Debug.Log("Get talked to by other bot");
            this.endTalkTime = endTalkTime;
            nextTalkTime = Time.time + Random.Range(minNextTalk, maxNextTalk);

            ChangePlayerBehavior(PlayerBehaviorState.talking);
            talkingToOtherBot = true;

            var currentScale = aliveBody.transform.localScale;
            var newScale = new Vector3(
                            (gameObject.transform.position.x - computerPlayer.gameObject.transform.position.x > 0 ? -1 : 1) * Mathf.Abs(currentScale.x),
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

            RpcClearMemoryBubbles();
            overhearingPlayers.Clear();
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
