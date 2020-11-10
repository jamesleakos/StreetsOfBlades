using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;


namespace BladesOfBellevue {

    public class PlayerInterction
    {

    }
    public class HumanPlayer : Player
    {
        #region Variables

        #region Name and Team

        /// <summary>
        /// Player name synced across the network.
        /// </summary>
        [HideInInspector]
        [SyncVar]
        public string myName;

        /// <summary>
        /// Team value assigned by the server.
        /// </summary>
		[HideInInspector]
        [SyncVar]
        public int teamIndex;

        #endregion

        #region Inventory
        [Header("Human Player - Inventory Menu")]

        // GOLD
        [HideInInspector]
        [SyncVar]
        public int goldAmount = 1000;

        // Items of Power

        #endregion

        #region Clothes and Disguises

        [Header("Human Player - Clothes Menu")]
        public GameObject clothesMenu;
        private bool personalMenuOn;

        private bool merchantClothesAvailable;
        private bool traderClothesAvailable;
        private bool farmerClothesAvailable;
        private bool monkClothesAvailable;

        #endregion

        #region Player Interaction
        [Header("Human Player - Kill Distance")]
        public float killDistance = 3.0f;

        private List<Player> pingedPlayers = new List<Player>();
        private List<Player> overlapPlayers = new List<Player>();

        private Player clickedPlayer;
        [HideInInspector]
        [SyncVar]
        public GameObject targetPlayer;
        [HideInInspector]
        [SyncVar]
        public GameObject talkPlayer;

        #endregion

        #region Camera

        /// <summary>
        /// Reference to the camera following component.
        /// </summary>
        [HideInInspector]
        public FollowTarget camFollow;

        // from navigation test

        private Camera mainCamera;

        #endregion

        #region Sounds

        /// <summary>
        /// Clip to play when assassinating.
        /// </summary>
        public AudioClip assassinationClip;

        #endregion

        #endregion

        #region Start etc.

        /// <summary>
        /// Initialize synced values on every client.
        /// </summary>
        public override void OnStartClient()
        {
            //get corresponding team and colorize renderers in team color
            Team team = GameManager.GetInstance().teams[teamIndex];
            // probably need to assign some color or something here


            // old code

            //for(int i = 0; i < renderers.Length; i++)
            //    renderers[i].material = team.material;

            //set name in label
            //label.text = myName;
        }

        /// <summary>
        /// Initialize camera and input for this local client.
        /// This is being called after OnStartClient.
        /// </summary>
        public override void OnStartLocalPlayer()
        {
            //initialized already on host migration
            if (GameManager.GetInstance().localPlayer != null)
                return;

            //set a global reference to the local player
            GameManager.GetInstance().localPlayer = this;

            // start navigation test
            mainCamera = Camera.main;
            // end navigation test

            CmdChangeDistrict(currentDistrict);

            List<Player> allPlayers = GameObject.FindObjectsOfType<Player>().ToList();
            foreach (var player in allPlayers)
            {
                player.ClearAllMenus();
                player.aliveBody.SetActive(true);
                player.deadBody.SetActive(false);
            }

            //initialize input controls for mobile devices
            //[0]=left joystick for movement, [1]=right joystick for shooting
#if !UNITY_STANDALONE && !UNITY_WEBGL
            GameManager.GetInstance().ui.controls[0].onDrag += Move;
            GameManager.GetInstance().ui.controls[0].onDragEnd += MoveEnd;

            GameManager.GetInstance().ui.controls[1].onDragBegin += ShootBegin;
            GameManager.GetInstance().ui.controls[1].onDrag += RotateTurret;
            GameManager.GetInstance().ui.controls[1].onDrag += Shoot;
#endif
        }

        #endregion

        #region Update

        #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL

        protected override void Update()
        {
            base.Update();

            ServerCheckForPlayerInteraction();

            //skip further calls for remote clients
            if (!isLocalPlayer)
            {
                //keep turret rotation updated for all clients
                OnHeadRotation(0, headRotation);
                return;
            }

            // THIS WILL ONLY BE CALLED FROM CLIENT

            CallUpdateDistrictVisuals();

            PlayerInputs();

            MoveCharacter();
        }

        #endif

        private void ServerCheckForPlayerInteraction()
        {
            if (!isServer) return;

            if (targetPlayer != null)
            {
                if ((targetPlayer.transform.position - gameObject.transform.position).magnitude < killDistance && currentDistrict == targetPlayer.GetComponent<Player>().currentDistrict)
                {
                    if (!canKill) targetPlayer.GetComponent<Player>().TargetSetCanKillCircle(gameObject.GetComponent<NetworkIdentity>().connectionToClient);
                    canKill = true;
                }
                else
                {
                    if (canKill) targetPlayer.GetComponent<Player>().TargetSetTargetSelectedCircle(gameObject.GetComponent<NetworkIdentity>().connectionToClient);
                    canKill = false;
                }
            }

            if (talkPlayer != null && talkPlayer.GetComponent<Player>().playerBehaviorState != PlayerBehaviorState.talking)
            {
                if ((talkPlayer.transform.position - gameObject.transform.position).magnitude < talkDistance && currentDistrict == talkPlayer.GetComponent<Player>().currentDistrict)
                {
                    bool result = talkPlayer.GetComponent<Player>().AskToStopToTalk(this);
                    if (result)
                    {
                        Debug.Log("Stop aggreed to");
                        ChangePlayerBehavior(PlayerBehaviorState.talking);
                    }
                }
            }
        }

        private void PlayerInputs()
        {
            if (Input.GetMouseButtonDown(1)) PingPlayers();

            if (Input.GetMouseButtonDown(0)) PingForLeftClickNodesAndButtons();

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Space)) CmdKillPlayer();

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (!amRunning) CmdSetRunning(true);
            }
            else
            {
                if (amRunning) CmdSetRunning(false);
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                if (personalMenuOn) SetPersonalMenu(false);
                else SetPersonalMenu(true);
            }
        }

        #endregion
        
        #region Selecting a player

        protected void PingPlayers()
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            List<Player> players = new List<Player>();

            if (hits.Count() > 0)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider != null)
                    {
                        if (hit.collider.tag == "Player")
                        {
                            players.Add(hit.collider.gameObject.GetComponent<Player>());
                        }
                    }
                }

                if (players.Count > 0)
                {
                    List<Player> protoOverlapPlayers = players.Intersect(overlapPlayers).ToList();
                    if (protoOverlapPlayers.Count > 0)
                    {
                        overlapPlayers = new List<Player>(protoOverlapPlayers);
                        pingedPlayers = new List<Player>(players);
                        // its important that the right click comes after the assigning these new lists so that we can remove the selected players from overlap
                        RightClickPlayer(PickClosestPlayerToMouse(protoOverlapPlayers));
                    }
                    else
                    {
                        List<Player> otherPlayers = players.Intersect(pingedPlayers).ToList();
                        if (otherPlayers.Count > 0)
                        {
                            overlapPlayers = new List<Player>(otherPlayers);
                            pingedPlayers = new List<Player>(players);
                            RightClickPlayer(PickClosestPlayerToMouse(otherPlayers));
                        }
                        else
                        {
                            overlapPlayers = new List<Player>(players);
                            pingedPlayers = new List<Player>(players);
                            RightClickPlayer(PickClosestPlayerToMouse(players));
                        }
                    }
                }
            } else
            {
                TurnOffClickedPlayerMenu();
            }
        }

        protected void TurnOffClickedPlayerMenu()
        {
            if (clickedPlayer != null)
            {
                clickedPlayer.ClearAllMenus();
            }
        }

        protected void RightClickPlayer(Player player)
        {
            TurnOffClickedPlayerMenu();
            SetPersonalMenu(false);
            if (player != gameObject.GetComponent<Player>())
            {
                clickedPlayer = player;
                player.SetInteractionMenuOn();
            } else
            {
                SetPersonalMenu(true);
            }
            
        }

        protected Player PickClosestPlayerToMouse(List<Player> players)
        {
            Player p = players[0];
            float f = (p.gameObject.transform.position - Input.mousePosition).magnitude;
            foreach (var player in players)
            {
                if ((player.gameObject.transform.position - Input.mousePosition).magnitude < f)
                {
                    p = player;
                    f = (player.gameObject.transform.position - Input.mousePosition).magnitude;
                }
            }

            overlapPlayers.Remove(p);

            return p;
        }

        protected void SetPersonalMenu (bool set)
        {
            clothesMenu.SetActive(set);
        }

        #endregion

        #region Killing Players

        [Command]
        private void CmdKillPlayer()
        {
            if ((targetPlayer.transform.position - gameObject.transform.position).magnitude < killDistance && currentDistrict == targetPlayer.GetComponent<Player>().currentDistrict)
            {
                Player killedPlayer = targetPlayer.GetComponent<Player>();
                killedPlayer.playerBehaviorState = PlayerBehaviorState.dead;
                killedPlayer.GetKilled();
                killedPlayer.RpcGetKilled();
                targetPlayer = null;
                Debug.Log("Player Killed");
            }
        }

        #endregion

        #region Changing Clothes

        private void ChangeClothes(CitizenType setCitizenType)
        {
            switch (setCitizenType)
            {
                case CitizenType.farmer:
                    if (farmerClothesAvailable) CmdChangeClothes(CitizenType.farmer);
                    break;
                case CitizenType.trader:
                    if (traderClothesAvailable) CmdChangeClothes(CitizenType.trader);
                    break;
                case CitizenType.monk:
                    if (monkClothesAvailable) CmdChangeClothes(CitizenType.monk);
                    break;
                case CitizenType.merchant:
                    if (merchantClothesAvailable) CmdChangeClothes(CitizenType.merchant);
                    break;
                default:
                    break;
            }
        }

        [Command]
        private void CmdChangeClothes(CitizenType setCitizenType)
        {
            switch (setCitizenType)
            {
                case CitizenType.farmer:
                    if (farmerClothesAvailable)
                    {
                        farmerClothesAvailable = false;
                        ChangeClothesServerHelper(setCitizenType);
                        RpcChangeClothes(citizenType, citizenColor);
                    }
                    break;
                case CitizenType.trader:
                    if (traderClothesAvailable)
                    {
                        traderClothesAvailable = false;
                        ChangeClothesServerHelper(setCitizenType);
                        RpcChangeClothes(citizenType, citizenColor);
                    }
                    break;
                case CitizenType.monk:
                    if (monkClothesAvailable)
                    {
                        monkClothesAvailable = false;
                        ChangeClothesServerHelper(setCitizenType);
                        RpcChangeClothes(citizenType, citizenColor);
                    }
                    break;
                case CitizenType.merchant:
                    if (merchantClothesAvailable)
                    {
                        merchantClothesAvailable = false;
                        ChangeClothesServerHelper(setCitizenType);
                        RpcChangeClothes(citizenType, citizenColor);
                    }
                    break;
                default:
                    break;
            }
        }

        [Server]
        private void ChangeClothesServerHelper(CitizenType setCitizenType)
        {
            citizenType = setCitizenType;
            citizenColor = (CitizenColor)Random.Range(0, System.Enum.GetNames(typeof(CitizenColor)).Length);
        }

        [ClientRpc]
        private void RpcChangeClothes(CitizenType setCitizenType, CitizenColor setCitizenColor)
        {
            switch (setCitizenType)
            {
                case CitizenType.farmer:
                    farmerClothesAvailable = false;
                    ChangeClothesClientHelper(setCitizenType, setCitizenColor);
                    break;
                case CitizenType.trader:
                    traderClothesAvailable = false;
                    ChangeClothesClientHelper(setCitizenType, setCitizenColor);
                    break;
                case CitizenType.monk:
                    monkClothesAvailable = false;
                    ChangeClothesClientHelper(setCitizenType, setCitizenColor);
                    break;
                case CitizenType.merchant:
                    merchantClothesAvailable = false;
                    ChangeClothesClientHelper(setCitizenType, setCitizenColor);
                    break;
                default:
                    break;
            }
        }

        [Client]
        private void ChangeClothesClientHelper(CitizenType setCitizenType, CitizenColor setCitizenColor)
        {
            citizenType = setCitizenType;
            citizenColor = setCitizenColor;

            nameText.text = setCitizenColor + " " + setCitizenType;
        }

        #endregion

        #region Buttons on Players

        protected void InteractionButtonSelection(List<PlayerInteractionButton> playerInteractionButtons)
        {
            PlayerInteractionButton buttonToClick = PickClosestButtonToMouse(playerInteractionButtons);

            if (buttonToClick.interactionButtonType == PlayerInteractionButton.InteractionButtonType.target)
            {
                if (targetPlayer != null)
                {
                    targetPlayer.GetComponent<Player>().ClearAllMenus();
                }
                CmdSetTarget(clickedPlayer.gameObject);
                clickedPlayer.SetTargetSelectedCircle();

            }
            else if (buttonToClick.interactionButtonType == PlayerInteractionButton.InteractionButtonType.talk)
            {
                CmdSetTalk(clickedPlayer.gameObject);
                clickedPlayer.SetTalkSelectedCircle();

            }
            else if (buttonToClick.interactionButtonType == PlayerInteractionButton.InteractionButtonType.dismiss)
            {
                if (talkPlayer != null)
                {
                    talkPlayer.GetComponent<Player>().ClearAllMenus();
                    CmdSetDismiss(talkPlayer.gameObject);
                }

            }
            else if (buttonToClick.interactionButtonType == PlayerInteractionButton.InteractionButtonType.assassinate)
            {
                CmdKillPlayer();
            }
            else if (buttonToClick.interactionButtonType == PlayerInteractionButton.InteractionButtonType.changeClothes)
            {
                var button = buttonToClick.GetComponent<PlayerChangeClothesButton>();
                ChangeClothes(button.citizenType);
            }
        }

        protected PlayerInteractionButton PickClosestButtonToMouse(List<PlayerInteractionButton> buttons)
        {
            PlayerInteractionButton b = buttons[0];
            float f = (b.gameObject.transform.position - Input.mousePosition).magnitude;
            foreach (var button in buttons)
            {
                if ((button.gameObject.transform.position - Input.mousePosition).magnitude < f)
                {
                    b = button;
                    f = (button.gameObject.transform.position - Input.mousePosition).magnitude;
                }
            }

            return b;
        }

        #endregion

        #region Target and Talk

        [Command]
        private void CmdSetTarget(GameObject p)
        {
            if (targetPlayer != null)
            {
                targetPlayer.GetComponent<Player>().targetingPlayer = null;
            }
            targetPlayer = p;
            p.GetComponent<Player>().targetingPlayer = gameObject;
        }

        [Command]
        private void CmdSetTalk(GameObject p)
        {
            talkPlayer = p;
            p.GetComponent<Player>().talkingPlayer = gameObject;
        }

        [Command]
        private void CmdSetDismiss(GameObject p)
        {
            ChangePlayerBehavior(PlayerBehaviorState.moving);
            talkPlayer = null;
            clickedPlayer = null;
            p.GetComponent<Player>().talkingPlayer = null;
            p.GetComponent<Player>().DismissFromTalking();
        }


        #endregion

        #region Old Interacting with Players



        //shoots a bullet in the direction passed in
        //we do not rely on the current turret rotation here, because we send the direction
        //along with the shot request to the server to absolutely ensure a synced shot position
        protected void AssassinatePlayer()
        {
            CmdAssassinate();
        }


        //Command creating a bullet on the server
        [Command]
        void CmdAssassinate()
        {




            //
            // THIS SEEMS LIKE USEFUL AND IMPORTANT CODE RIGHT HERE
            //

            //calculate center between shot position sent and current server position (factor 0.6f = 40% client, 60% server)
            //this is done to compensate network lag and smoothing it out between both client/server positions
            //Vector3 shotCenter = Vector3.Lerp(shotPos.position, new Vector3(xPos / 10f, shotPos.position.y, zPos / 10f), 0.6f);

            //spawn bullet using pooling, locally
            //GameObject obj = PoolManager.Spawn(bullets[currentBullet], shotCenter, turret.rotation);
            //Bullet blt = obj.GetComponent<Bullet>();
            //blt.owner = gameObject;

            //spawn bullet networked
            //NetworkServer.Spawn(obj, bullets[currentBullet].GetComponent<NetworkIdentity>().assetId);

            //send event to all clients for spawning effects
            if (assassinationClip)
                RpcOnAssassination();
        }

        //called on all clients after bullet spawn
        //spawn effects or sounds locally, if set
        [ClientRpc]
        protected void RpcOnAssassination()
        {
            // here we can play an assassination animation as well
            if (assassinationClip) AudioManager.Play3D(assassinationClip, head.position, 0.1f);
        }

        [Server]
        public override void GetKilled(HumanPlayer player)
        {
            //the game is already over so don't do anything
            if (GameManager.GetInstance().IsGameOver()) return;

            //get killer and increase score for that team
            HumanPlayer other = player;
            GameManager.GetInstance().AddScore(ScoreType.Kill, other.teamIndex);
            //the maximum score has been reached now
            if (GameManager.GetInstance().IsGameOver())
            {
                //tell all clients the winning team
                RpcGameOver(other.teamIndex);
                return;
            }

            //tell the dead player who killed him (owner of the bullet)
            short senderId = 0;
            senderId = (short)player.GetComponent<NetworkIdentity>().netId;

            RpcRespawn(senderId);
        }


        //called on all clients on both player death and respawn
        //only difference is that on respawn, the client sends the request
        [ClientRpc]
        protected virtual void RpcRespawn(short senderId)
        {
            //toggle visibility for player gameobject (on/off)
            gameObject.SetActive(!gameObject.activeInHierarchy);
            bool isActive = gameObject.activeInHierarchy;
            killedBy = null;

            //the player has been killed
            if (!isActive)
            {
                //find original sender game object (killedBy)
                GameObject senderObj = null;
                if (senderId > 0 && NetworkIdentity.spawned.ContainsKey((uint)senderId))
                {
                    senderObj = NetworkIdentity.spawned[(uint)senderId].gameObject;
                    if (senderObj != null) killedBy = senderObj;
                }

                //detect whether the current user was responsible for the kill, but not for suicide
                //yes, that's my kill: increase local kill counter
                if (this != GameManager.GetInstance().localPlayer && killedBy == GameManager.GetInstance().localPlayer.gameObject)
                {
                    GameManager.GetInstance().ui.killCounter[0].text = (int.Parse(GameManager.GetInstance().ui.killCounter[0].text) + 1).ToString();
                    GameManager.GetInstance().ui.killCounter[0].GetComponent<Animator>().Play("Animation");
                }

                //if (explosionFX)
                //{
                //spawn death particles locally using pooling and colorize them in the player's team color
                //GameObject particle = PoolManager.Spawn(explosionFX, transform.position, transform.rotation);
                //ParticleColor pColor = particle.GetComponent<ParticleColor>();
                //if(pColor) pColor.SetColor(GameManager.GetInstance().teams[teamIndex].material.color);
                //}

                //play sound clip on player death
                //if(explosionClip) AudioManager.Play3D(explosionClip, transform.position);
            }

            if (isServer)
            {
                //send player back to the team area, this will get overwritten by the exact position from the client itself later on
                //we just do this to avoid players "popping up" from the position they died and then teleporting to the team area instantly
                //this is manipulating the internal PhotonTransformView cache to update the networkPosition variable
                transform.position = GameManager.GetInstance().GetSpawnPosition(teamIndex);
            }

            //further changes only affect the local client
            if (!isLocalPlayer)
                return;

            //local player got respawned so reset states
            if (isActive == true)
                ResetPosition();
            else
            {
                //local player was killed, set camera to follow the killer
                if (killedBy != null) camFollow.target = killedBy.transform;
                //hide input controls and other HUD elements
                camFollow.HideMask(true);
                //display respawn window (only for local player)
                GameManager.GetInstance().DisplayDeath();
            }
        }


        /// <summary>
        /// Command telling the server that this client is ready for respawn.
        /// This is when the respawn delay is over or a video ad has been watched.
        /// </summary>
        [Command]
        public void CmdRespawn()
        {
            RpcRespawn((short)0);
        }

        public void OnClothesChange(int oldClothes, int newClothes)
        {

        }


        /// <summary>
        /// Repositions in team area and resets camera & input variables.
        /// This should only be called for the local player.
        /// </summary>
        public void ResetPosition()
        {
            //start following the local player again
            camFollow.target = head;
            camFollow.HideMask(false);

            //get team area and reposition it there
            transform.position = GameManager.GetInstance().GetSpawnPosition(teamIndex);

            //reset forces modified by input
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.rotation = Quaternion.identity;
            //reset input left over
            GameManager.GetInstance().ui.controls[0].OnEndDrag(null);
            GameManager.GetInstance().ui.controls[1].OnEndDrag(null);
        }


        /// <summary>
        /// Called on all clients on game end providing the winning team.
        /// This is when a target kill count or goal (e.g. flag captured) was achieved.
        /// </summary>
        [ClientRpc]
        public void RpcGameOver(int teamIndex)
        {
            //display game over window
            GameManager.GetInstance().DisplayGameOver(teamIndex);
        }

        #endregion

        #region Pathing and Movement

        protected void PingForLeftClickNodesAndButtons()
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hits.Count() > 0)
            {
                List<Node> nodesClicked = new List<Node>();
                List<TeleporterNodeHelper> teleporterNodeHelpers = new List<TeleporterNodeHelper>();
                List<PlayerInteractionButton> playerInteractionButtonsClicked = new List<PlayerInteractionButton>();

                foreach (var hit in hits)
                {
                    if (hit.collider != null)
                    {
                        if (hit.collider.tag == "Node")
                        {
                            nodesClicked.Add(hit.collider.gameObject.GetComponent<Node>());
                        }
                        else if (hit.collider.tag == "TeleporterHelper")
                        {
                            teleporterNodeHelpers.Add(hit.collider.gameObject.GetComponent<TeleporterNodeHelper>());
                        }
                        else if (hit.collider.tag == "PlayerInteractionButton")
                        {
                            playerInteractionButtonsClicked.Add(hit.collider.gameObject.GetComponent<PlayerInteractionButton>());
                        }
                    }
                }

                if (playerInteractionButtonsClicked.Count > 0)
                {
                    InteractionButtonSelection(playerInteractionButtonsClicked);
                }
                else
                {
                    if (teleporterNodeHelpers.Count > 0 && !teleporting)
                    {
                        foreach (var nodeHelper in teleporterNodeHelpers)
                        {
                            Node node = nodeHelper.gameObject.GetComponent<TeleporterNodeHelper>().myNode;
                            nodesClicked.Add(node);
                        }
                    }

                    if (nodesClicked.Count > 0)
                    {
                        Node selectedNode = null;
                        foreach (Node node in nodesClicked)
                        {
                            if (node.district == currentDistrict)
                            {
                                if (selectedNode == null)
                                {
                                    selectedNode = node;
                                }
                                else
                                {
                                    if (node.gameObject.transform.position.y > selectedNode.transform.position.y)
                                    {
                                        selectedNode = node;
                                    }
                                }
                            }
                        }

                        if (selectedNode != null)
                        {
                            Node goalNode = selectedNode;
                            Node currentNode = pathManager.FindClosestNode(transform.position);
                            if (CompareLists(goalNode.neighbors, currentNode.neighbors) && currentNode.nodeType == Node.NodeType.filler)
                            {
                                path.Clear();
                                path.Add(goalNode);
                                RestartNodeLists();
                            }
                            else
                            {
                                path = pathManager.FindShortestPath(selectedNode.gameObject.transform.position);
                                if (path.Count > 0)
                                {
                                    RestartNodeLists();
                                }
                            }
                        }
                    }
                }
            }
        }

        [Command]
        private void CmdSetRunning(bool setRunning)
        {
            amRunning = setRunning;
            RpcSetRunning(setRunning);
        }

        [ClientRpc]
        private void RpcSetRunning(bool setRunning)
        {
            amRunning = setRunning;
        }
        #endregion

        #region Teleporting

        public override void OnTeleport(Node node)
        {
            CmdChangeDistrict(node.district);
        }

        [Command]
        private void CmdChangeDistrict(DistrictType district)
        {
            currentDistrict = district;
        }

        private void CallUpdateDistrictVisuals()
        {
            if (!isLocalPlayer) return;

            List<DistrictVisuals> districtVisuals = GameObject.FindObjectsOfType<DistrictVisuals>().ToList();
            foreach (var dv in districtVisuals)
            {
                if (dv.district == currentDistrict)
                {
                    dv.gameObject.GetComponent<SpriteRenderer>().enabled = true;
                    mainCamera.transform.position = new Vector3(dv.gameObject.transform.position.x, dv.gameObject.transform.position.y, mainCamera.transform.position.z);
                }
                else
                {
                    dv.gameObject.GetComponent<SpriteRenderer>().enabled = false;
                }
            }

            List<Player> players = GameObject.FindObjectsOfType<Player>().ToList();
            foreach (var player in players)
            {
                if (player.currentDistrict == currentDistrict)
                {
                    player.gameObject.transform.Find("VisualComponents").gameObject.SetActive(true);
                }
                else
                {
                    player.gameObject.transform.Find("VisualComponents").gameObject.SetActive(false);
                }
            }
        }

        #endregion

    }
}


