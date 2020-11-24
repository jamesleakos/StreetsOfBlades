using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;
using TMPro;


namespace BladesOfBellevue
{
    public class SeerBehavior : NetworkBehaviour
    {
        public ComputerPlayer myCompPlayer;
        public int costOfDistrictsOfSpies = 200;
        public int costOfPictureOfSpy = 500;
        public int costOfTellIfTargeted = 20;
        public int costOfTellTargeterDistricts = 50;
        public int costOfShowTargeters = 200;

        public void ActionTriggerTest ()
        {

        }

        public void PopulateWithInfo (ComputerPlayer computerPlayer)
        {
            myCompPlayer = computerPlayer;
        }

        [Command]
        public void CmdRequestToBuyListOfDistrictsOfSpies (GameObject humanGO)
        {
            HumanPlayer humanPlayer = humanGO.GetComponent<HumanPlayer>();
            if (humanPlayer.goldAmount >= costOfDistrictsOfSpies)
            {
                humanPlayer.goldAmount -= costOfDistrictsOfSpies;
                TargetGiveListOfDistrictOfSpies(humanPlayer.gameObject.GetComponent<NetworkIdentity>().connectionToClient);

                humanPlayer.RpcSpendGold(humanPlayer.goldAmount);
            }
        }

        [TargetRpc]
        public void TargetGiveListOfDistrictOfSpies (NetworkConnection target)
        {
            string result;
            var players = GameObject.FindObjectsOfType<HumanPlayer>().ToList();
            // here is where we working
            if (players.Count <= 1) result = "You are the only remaining player.";
            else
            {
                List<int> playersInZones = new List<int>();
                for (int i = 0; i < Enum.GetValues(typeof(DistrictType)).Length; i++) playersInZones.Add(0);

            }




        }


        [Command]
        public void CmdRequestToBuyPictureOfSpy(GameObject humanGO)
        {
            HumanPlayer humanPlayer = humanGO.GetComponent<HumanPlayer>();
            if (humanPlayer.goldAmount >= costOfPictureOfSpy)
            {
                humanPlayer.goldAmount -= costOfPictureOfSpy;
                TargetGivePictureOfSpy(humanPlayer.gameObject.GetComponent<NetworkIdentity>().connectionToClient);
            }

        }

        [TargetRpc]
        public void TargetGivePictureOfSpy(NetworkConnection target)
        {
            // create speech bubble with the appropriate info inside
            // CREATE THIS BUBBLES IN PREFABS NOW
        }

        [Command]
        public void CmdRequestToBuyTellIfTargeted(GameObject humanGO)
        {
            HumanPlayer humanPlayer = humanGO.GetComponent<HumanPlayer>();
            if (humanPlayer.goldAmount >= costOfTellIfTargeted)
            {
                humanPlayer.goldAmount -= costOfTellIfTargeted;
                TargetTellIfTargeted(humanPlayer.gameObject.GetComponent<NetworkIdentity>().connectionToClient);
            }

        }

        [TargetRpc]
        public void TargetTellIfTargeted(NetworkConnection target)
        {
            // create speech bubble with the appropriate info inside
            // CREATE THIS BUBBLES IN PREFABS NOW
        }
    }
}

