using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;


namespace BladesOfBellevue {
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

        #region Skins and Disguises

        /// <summary>
        /// Index of currently selected skin.
        /// </summary>
        [HideInInspector]
        [SyncVar(hook = "OnSkinChange")]
        public int currentSkin = 0;

        /// <summary>
        /// Array of available skins.
        /// </summary>
        public GameObject[] skins;

        #endregion

        #region Player Interaction

        List<Player> pingedPlayers;
        List<Player> overlapPlayers;
        public Player clickedPlayer;

        public GameObject targetPlayer;
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
        //continously check for input on desktop platforms
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        protected void Update()
        {
            //skip further calls for remote clients
            if (!isLocalPlayer)
            {
                //keep turret rotation updated for all clients
                OnHeadRotation(0, headRotation);
                return;
            }

            CallUpdateDistrictVisuals();

            if (Input.GetMouseButtonDown(1))
            {
                PingPlayers();
            }

            #region New Movement Code

            if (Input.GetMouseButtonDown(0))
            {
                UpdatePathToClick();
            }

            MoveCharacter();

            #endregion
        }
#endif
        #endregion

        #region Player Interaction

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
                        RightClickPlayer(PickClosestPlayer(protoOverlapPlayers));
                    }
                    else
                    {
                        List<Player> otherPlayers = players.Intersect(pingedPlayers).ToList();
                        if (otherPlayers.Count > 0)
                        {
                            overlapPlayers = new List<Player>(otherPlayers);
                            pingedPlayers = new List<Player>(players);
                            RightClickPlayer(PickClosestPlayer(otherPlayers));
                        }
                        else
                        {
                            overlapPlayers = new List<Player>(players);
                            pingedPlayers = new List<Player>(players);
                            RightClickPlayer(PickClosestPlayer(players));
                        }
                    }
                }
            }
        }

        protected void RightClickPlayer(Player player)
        {
            clickedPlayer = player;
            player.TurnOnInteractionMenu();
        }

        protected Player PickClosestPlayer(List<Player> players)
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

        #endregion



        // pick talk player - networked

        // pick target player - networked

        #endregion

        #region Pathing and Movement

        protected void UpdatePathToClick()
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hits.Count() > 0)
            {
                Node selectedNode = null;
                bool hitSelected = false;
                foreach (var hit in hits)
                {
                    if (hit.collider != null)
                    {
                        if (hit.collider.tag == "Node" || hit.collider.tag == "TeleporterHelper")
                        {
                            Node node;
                            if (hit.collider.tag == "TeleporterHelper")
                            {
                                node = hit.collider.gameObject.GetComponent<TeleporterNodeHelper>().myNode;
                                if ((node.transform.position - transform.position).magnitude <= 1)
                                {
                                    teleporting = true;
                                    teleportTime = Time.time + teleportLength;
                                    teleportBeginning = node;
                                    teleportDestination = node.teleporterDestinationNode;
                                    break;
                                }
                            }
                            else
                            {
                                node = hit.collider.gameObject.GetComponent<Node>();
                            }
                            if (node != null)
                            {
                                if (node.district == currentDistrict)
                                {
                                    if (!hitSelected)
                                    {
                                        selectedNode = node;
                                        hitSelected = true;
                                    }
                                    else
                                    {
                                        if (hit.collider.transform.position.y > selectedNode.transform.position.y)
                                        {
                                            selectedNode = node;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (hitSelected)
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

        #endregion

        #region Teleporting

        [Command]
        private void CmdChangeDistrict(District district)
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
                    player.gameObject.transform.Find("Mario").gameObject.SetActive(true);
                }
                else
                {
                    player.gameObject.transform.Find("Mario").gameObject.SetActive(false);
                }
            }
        }

        public override void Teleport()
        {
            base.Teleport();
            CmdChangeDistrict(teleportDestination.district);
        }

        #endregion

        #region Iteracting with Players

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

        public void OnSkinChange(int oldSkin, int newSkin)
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

    }
}


