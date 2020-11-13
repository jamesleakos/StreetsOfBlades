using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace BladesOfBellevue
{
    [RequireComponent(typeof(BoxCollider2D), typeof(PlayerInteractionButton))]
    public class MemoryBubble : MonoBehaviour
    {
        public TextMeshProUGUI memoryText;

        public void PopulateWithInfo(BotMemory memory)
        {
            switch (memory.memoryType)
            {
                case BotMemory.MemoryType.murder:
                    memoryText.text = "Murder";
                    break;
                case BotMemory.MemoryType.changedClothes:
                    memoryText.text = "changedClothes";
                    break;
                case BotMemory.MemoryType.kickedWaif:
                    memoryText.text = "kickedWaif";
                    break;
                default:
                    break;
            }
        }
    }
}

