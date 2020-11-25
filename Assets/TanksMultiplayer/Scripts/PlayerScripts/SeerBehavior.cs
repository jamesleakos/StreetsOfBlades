using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;
using TMPro;


namespace StreetsOfTheSicario
{
    public class SeerBehavior : NetworkBehaviour
    {

        // Computer player ref
        public ComputerPlayer myCompPlayer;

        // costs 
        public int costOfDistrictsOfSpies = 200;
        public int costOfPictureOfSpy = 500;
        public int costOfTellIfTargeted = 20;
        public int costOfTellTargeterDistricts = 50;
        public int costOfShowTargeters = 200;

        // UI 
        public GameObject DistricttOfSpiesResults;
        public TextMeshProUGUI SeerResultText;

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
                TargetGiveListOfDistrictOfSpies(humanPlayer.gameObject.GetComponent<NetworkIdentity>().connectionToClient, humanGO);

                humanPlayer.RpcSpendGold(humanPlayer.goldAmount);
            }
        }

        [TargetRpc]
        public void TargetGiveListOfDistrictOfSpies (NetworkConnection target, GameObject humanGO)
        {
            string result;
            var players = GameObject.FindObjectsOfType<HumanPlayer>().ToList();
            // here is where we working
            if (players.Count <= 1) result = "You are the only remaining player.";
            else
            {
                List<int> playersInZones = new List<int>();
                for (int i = 0; i < Enum.GetValues(typeof(DistrictType)).Length; i++) playersInZones.Add(0);
                result = "There are ";
                for (int i = 0; i < Enum.GetValues(typeof(DistrictType)).Length; i++)
                {
                    var count = players.FindAll(c => (int)c.currentDistrict == i);
                    count.Remove(humanGO.GetComponent<HumanPlayer>());
                    if (count.Count() > 0) result = result + count.Count().ToString() + " players in " + ((DistrictType)count.Count()).ToString() + "; ";
                }
            }
            SeerResultText.text = result;
            DistricttOfSpiesResults.SetActive(true);
        }


        [Command]
        public void CmdRequestToBuyPictureOfSpy(GameObject humanGO)
        {
            HumanPlayer humanPlayer = humanGO.GetComponent<HumanPlayer>();
            if (humanPlayer.goldAmount >= costOfPictureOfSpy)
            {
                humanPlayer.goldAmount -= costOfPictureOfSpy;
                TargetGivePictureOfSpy(humanPlayer.gameObject.GetComponent<NetworkIdentity>().connectionToClient, humanGO);
            }

        }

        [TargetRpc]
        public void TargetGivePictureOfSpy(NetworkConnection target, GameObject humanGO)
        {
            string result;
            var players = GameObject.FindObjectsOfType<HumanPlayer>().ToList();
            players.Remove(humanGO.GetComponent<HumanPlayer>());
            // here is where we working
            if (players.Count <= 1) result = "You are the only remaining player.";
            else
            {
                // get random selection from players
                var pick = players[UnityEngine.Random.Range(0, players.Count())];
                result = "A " + pick.citizenColor.ToString() + " " + pick.citizenType.ToString() + " is a spy";
            }
            SeerResultText.text = result;
            DistricttOfSpiesResults.SetActive(true);
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

