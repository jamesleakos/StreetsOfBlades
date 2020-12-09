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
    public class NobleBehavior : NetworkBehaviour
    {
        // Computer player ref
        public ComputerPlayer myCompPlayer;

        public int deathPayOff;

        public void PopulateWithInfo(ComputerPlayer computerPlayer)
        {
            myCompPlayer = computerPlayer;
        }

        [Server]
        public void PayKiller (HumanPlayer humanPlayer)
        {
            humanPlayer.goldAmount += deathPayOff;

            humanPlayer.RpcUpdateGold(humanPlayer.goldAmount);
        }
    }
}

