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

        // Menu Options
        public GameObject AmITargetedButton;
        public GameObject WhereAreTargetersButton;
        public GameObject ShowTargetersButton;
        public GameObject PictureOfSpyButton;
        public GameObject DistrictsOfSpiesButton;

        public void PopulateWithInfo (ComputerPlayer computerPlayer)
        {
            myCompPlayer = computerPlayer;
            DistricttOfSpiesResults = computerPlayer.DistricttOfSpiesResults;
            SeerResultText = computerPlayer.SeerResultText;
            AmITargetedButton = computerPlayer.AmITargetedButton;
            WhereAreTargetersButton = computerPlayer.WhereAreTargetersButton;
            ShowTargetersButton = computerPlayer.ShowTargetersButton;
            PictureOfSpyButton = computerPlayer.PictureOfSpyButton;
            DistrictsOfSpiesButton = computerPlayer.SeerShowTargetersMenu;
        }

        public void ClearAllSeerMenus ()
        {
            AmITargetedButton.SetActive(false);
            WhereAreTargetersButton.SetActive(false);
            ShowTargetersButton.SetActive(false);
            PictureOfSpyButton.SetActive(false);
            DistrictsOfSpiesButton.SetActive(false);
        }

        public void MainSeerMenu ()
        {
            ClearAllSeerMenus();
            AmITargetedButton.SetActive(true);
            PictureOfSpyButton.SetActive(true);
            DistrictsOfSpiesButton.SetActive(true);
        }

        public void ShowTargeterLocationMenu()
        {
            ClearAllSeerMenus();
            WhereAreTargetersButton.SetActive(true);
            PictureOfSpyButton.SetActive(true);
            DistrictsOfSpiesButton.SetActive(true);
        }

        public void ShowTargeterMenu()
        {
            ClearAllSeerMenus();
            ShowTargetersButton.SetActive(true);
            PictureOfSpyButton.SetActive(true);
            DistrictsOfSpiesButton.SetActive(true);
        }

        [Command]
        public void CmdRequestToBuyListOfDistrictsOfSpies (GameObject humanGO)
        {
            HumanPlayer humanPlayer = humanGO.GetComponent<HumanPlayer>();
            if (humanPlayer.goldAmount >= costOfDistrictsOfSpies)
            {
                humanPlayer.goldAmount -= costOfDistrictsOfSpies;
                TargetGiveListOfDistrictOfSpies(humanPlayer.gameObject.GetComponent<NetworkIdentity>().connectionToClient, humanGO);

                humanPlayer.RpcUpdateGold(humanPlayer.goldAmount);
            }
        }

        [TargetRpc]
        public void TargetGiveListOfDistrictOfSpies (NetworkConnection target, GameObject humanGO)
        {
            string result;
            var players = GameObject.FindObjectsOfType<HumanPlayer>().ToList();
            players.Remove(humanGO.GetComponent<HumanPlayer>());
            if (players.Count == 0)
            {
                result = "You are the only remaining player.";
                MainSeerMenu();
            }
            else
            {
                List<int> playersInZones = new List<int>();
                for (int i = 0; i < Enum.GetValues(typeof(DistrictType)).Length; i++) playersInZones.Add(0);
                result = "There are ";
                for (int i = 0; i < Enum.GetValues(typeof(DistrictType)).Length; i++)
                {
                    var count = players.FindAll(c => (int)c.currentDistrict == i);
                    if (count.Count() > 0) result = result + count.Count().ToString() + " players in " + ((DistrictType)count.Count()).ToString() + "; ";
                }
                ShowTargeterMenu();
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

                humanPlayer.RpcUpdateGold(humanPlayer.goldAmount);
            }

        }

        [TargetRpc]
        public void TargetGivePictureOfSpy(NetworkConnection target, GameObject humanGO)
        {
            string result;
            var players = GameObject.FindObjectsOfType<HumanPlayer>().ToList();
            players.Remove(humanGO.GetComponent<HumanPlayer>());
            // here is where we working
            if (players.Count == 0)
            {
                result = "You are the only remaining player.";
            }
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
                TargetTellIfTargeted(humanPlayer.gameObject.GetComponent<NetworkIdentity>().connectionToClient, humanGO);
            }

        }

        [TargetRpc]
        public void TargetTellIfTargeted(NetworkConnection target, GameObject humanGO)
        {
            string result;
            var players = GameObject.FindObjectsOfType<HumanPlayer>().ToList();
            players.Remove(humanGO.GetComponent<HumanPlayer>());
            // here is where we working
            if (players.Count <= 1)
            {
                result = "You are the only remaining player.";
                MainSeerMenu();
            }
            else
            {
                // get random selection from players
                var targeters = players.FindAll(p => p.targetPlayer == humanGO);
                if (targeters.Count() == 0)
                {
                    result = "No one is targeting you";
                    MainSeerMenu();
                }
                else if (targeters.Count() == 1) {
                    result = "You are targeted by one player.";
                    ShowTargeterLocationMenu();
                }
                else {
                    result = "You are targeted by " + targeters.Count().ToString() + " players.";
                    ShowTargeterLocationMenu();
                }
            }
            SeerResultText.text = result;
            DistricttOfSpiesResults.SetActive(true);
        }

        [Command]
        public void CmdRequestToBuyTargeterDisticts(GameObject humanGO)
        {
            HumanPlayer humanPlayer = humanGO.GetComponent<HumanPlayer>();
            if (humanPlayer.goldAmount >= costOfTellTargeterDistricts)
            {
                humanPlayer.goldAmount -= costOfTellTargeterDistricts;
                TargetTellTargeterDistricts(humanPlayer.gameObject.GetComponent<NetworkIdentity>().connectionToClient, humanGO);

                humanPlayer.RpcUpdateGold(humanPlayer.goldAmount);
            }
        }

        [TargetRpc]
        public void TargetTellTargeterDistricts(NetworkConnection target, GameObject humanGO)
        {
            string result;
            var players = GameObject.FindObjectsOfType<HumanPlayer>().ToList();
            players.Remove(humanGO.GetComponent<HumanPlayer>());
            var targeters = players.FindAll(p => p.targetPlayer == humanGO);
            // here is where we working
            if (targeters.Count <= 1)
            {
                result = "You are the only remaining player.";
                MainSeerMenu();
            }
            else
            {
                List<int> playersInZones = new List<int>();
                for (int i = 0; i < Enum.GetValues(typeof(DistrictType)).Length; i++) playersInZones.Add(0);
                result = "There are ";
                for (int i = 0; i < Enum.GetValues(typeof(DistrictType)).Length; i++)
                {
                    var count = targeters.FindAll(c => (int)c.currentDistrict == i);
                    count.Remove(humanGO.GetComponent<HumanPlayer>());
                    if (count.Count() > 0) result = result + count.Count().ToString() + " targeters in " + ((DistrictType)count.Count()).ToString() + "; ";
                }
                // here we are setting menus i think
                if (targeters.FindAll(c => c.currentDistrict == humanGO.GetComponent<HumanPlayer>().currentDistrict).Count() > 0) ShowTargeterMenu();
                else MainSeerMenu();
            }

            SeerResultText.text = result;
            DistricttOfSpiesResults.SetActive(true);
        }

        [Command]
        public void CmdRequestToBuyTargeterIndicators(GameObject humanGO)
        {
            HumanPlayer humanPlayer = humanGO.GetComponent<HumanPlayer>();
            if (humanPlayer.goldAmount >= costOfShowTargeters)
            {
                humanPlayer.goldAmount -= costOfShowTargeters;
                TargetShowTargeters(humanPlayer.gameObject.GetComponent<NetworkIdentity>().connectionToClient, humanGO);

                humanPlayer.RpcUpdateGold(humanPlayer.goldAmount);
            }

        }

        [TargetRpc]
        public void TargetShowTargeters(NetworkConnection target, GameObject humanGO)
        {
            string result;
            var players = GameObject.FindObjectsOfType<HumanPlayer>().ToList();
            players.Remove(humanGO.GetComponent<HumanPlayer>());
            var targeters = players.FindAll(p => p.targetPlayer == humanGO);
            // here is where we working
            if (targeters.Count <= 1) result = "You are the only remaining player.";
            else
            {
                result = "Targeters exposed";
                foreach (var targeter in targeters)
                {
                    targeter.TargetTurnOnSpyMarker(humanGO.GetComponent<HumanPlayer>().gameObject.GetComponent<NetworkIdentity>().connectionToClient);
                }
            }
            MainSeerMenu();
            SeerResultText.text = result;
            DistricttOfSpiesResults.SetActive(true);
        }
    }
}

