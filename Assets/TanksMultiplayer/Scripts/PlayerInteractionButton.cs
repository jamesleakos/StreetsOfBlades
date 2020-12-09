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
        askBeggarNetwork,
        changeClothes,
        dismissMemoryBubble,
        dismissBotInfo,
        barter,

        openSeerBarterMenu,
        paySeerToTellIfTargeted,
        paySeerToTellWhereTargetersAre,
        paySeerToShowTargeters,
        paySeerToShowPictureOfSpy,
        paySeerToListDistrictsOfSpies
    }

    public InteractionButtonType interactionButtonType;
}
