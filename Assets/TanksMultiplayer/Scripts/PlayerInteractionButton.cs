using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionButton : MonoBehaviour
{
    public enum InteractionButtonType
    {
        target,
        talk,
        dismiss,
        news,
        payWaifToFollow,
        payMonkToShout,
        payBeggarToShout
    }

    public InteractionButtonType interactionButtonType;
}
