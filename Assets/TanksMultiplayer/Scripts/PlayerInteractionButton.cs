using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionButton : MonoBehaviour
{
    public enum InteractionButtonType
    {
        assassinate,
        target,
        talk,
        dismiss,
        news,
        payWaifToFollow,
        payMonkToShout,
        payBeggarToShout,
        changeClothes,
        dismissMemoryBubble
    }

    public InteractionButtonType interactionButtonType;
}
