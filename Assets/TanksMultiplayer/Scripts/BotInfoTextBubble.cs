using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StreetsOfTheSicario
{
    [RequireComponent(typeof(BoxCollider2D), typeof(PlayerInteractionButton))]
    public class BotInfoTextBubble : MonoBehaviour
    {
        GameObject humanPlayer;
        ComputerPlayer myCompPlayer;
        public float disappearDistance = 25.0f;

        public void PopulateWithInfo (GameObject humanPlayer, ComputerPlayer cp)
        {
            this.humanPlayer = humanPlayer;
            myCompPlayer = cp;
        }

        public void Update()
        {
            if ((humanPlayer.transform.position - gameObject.transform.position).magnitude < disappearDistance)
            {
                gameObject.SetActive(false);
            }

            if (myCompPlayer.playerBehaviorState != Player.PlayerBehaviorState.talking)
            {
                gameObject.SetActive(false);
            }
        }
    }
}

