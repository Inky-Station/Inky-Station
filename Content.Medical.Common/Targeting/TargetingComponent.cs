// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Medical.Common.Targeting;

/// <summary>
/// Controls entity limb targeting for actions.
/// </summary>
///
///
/// THIS FILE WAS HEAVILY EDITED BY INKY IF YOURE FUTURE ME DOING AN UPSTREAM PLEASE PROCEED TO KILL YOURSELF
///                                                                  -lucifer
///
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TargetingComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public TargetBodyPart Target = TargetBodyPart.Chest;

    public TargetBodyPartNonFlag TargetNonFlag => Target switch
    {
        TargetBodyPart.Head => TargetBodyPartNonFlag.Head,
        TargetBodyPart.Chest => TargetBodyPartNonFlag.Chest,
        // TargetBodyPart.Groin => TargetBodyPartNonFlag.Groin,         //inkymed
        TargetBodyPart.LeftArm => TargetBodyPartNonFlag.LeftArm,
        // TargetBodyPart.LeftHand => TargetBodyPartNonFlag.LeftHand,   //inkymed
        TargetBodyPart.RightArm => TargetBodyPartNonFlag.RightArm,
        // TargetBodyPart.RightHand => TargetBodyPartNonFlag.RightHand, //inkymed
        TargetBodyPart.LeftLeg => TargetBodyPartNonFlag.LeftLeg,
        // TargetBodyPart.LeftFoot => TargetBodyPartNonFlag.LeftFoot,   //inkymed
        TargetBodyPart.RightLeg => TargetBodyPartNonFlag.RightLeg,
        // TargetBodyPart.RightFoot => TargetBodyPartNonFlag.RightFoot, //inkymed
        TargetBodyPart.Tail => TargetBodyPartNonFlag.Tail,
        TargetBodyPart.Wings => TargetBodyPartNonFlag.Wings,
        _ => TargetBodyPartNonFlag.Chest,
    };

    /// <summary>
    /// What odds are there for every part targeted to be hit?
    /// </summary>
    [DataField]
    public Dictionary<TargetBodyPart, Dictionary<TargetBodyPart, float>> TargetOdds = new()
    {
        {
            TargetBodyPart.Head, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.Head, 0.3f },
                { TargetBodyPart.Chest, 0.7f },
            }
        },
        {
            TargetBodyPart.Chest, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.Chest, 1f }, // If you change this, suicide system won't work properly. So I won't even be able to ask you to kill yourself for doing this.
            }
        },
        /*{ //inkymed
            TargetBodyPart.Groin, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.Groin, 0.4f },
                { TargetBodyPart.Chest, 0.6f },
            }
        }, */
        {
            TargetBodyPart.RightArm, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightArm, 0.25f },
                { TargetBodyPart.Chest, 0.6f },
                /*
                { TargetBodyPart.Groin, 0.1f },
                { TargetBodyPart.RightHand, 0.05f },
                */  //inkymed
            }
        },
        {
            TargetBodyPart.LeftArm, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftArm, 0.25f },
                { TargetBodyPart.Chest, 0.6f },
                /*
                { TargetBodyPart.Groin, 0.1f },
                { TargetBodyPart.LeftHand, 0.05f },
                */  //inkymed
            }
        },
        /*{
            TargetBodyPart.RightHand, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightHand, 0.2f },
                { TargetBodyPart.Chest, 0.6f },
                { TargetBodyPart.Groin, 0.1f },
                { TargetBodyPart.RightArm, 0.1f },
            }
        },

        {
            TargetBodyPart.LeftHand, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftHand, 0.2f },
                { TargetBodyPart.Chest, 0.6f },
                { TargetBodyPart.Groin, 0.1f },
                { TargetBodyPart.LeftArm, 0.1f },
            }
        },
        */  //inkymed
        {
            TargetBodyPart.RightLeg, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightLeg, 0.25f },
                { TargetBodyPart.Chest, 0.6f },
                /*
                { TargetBodyPart.Groin, 0.1f },
                { TargetBodyPart.RightFoot, 0.05f },
                */  //inkymed
            }
        },
        {
            TargetBodyPart.LeftLeg, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftLeg, 0.25f },
                { TargetBodyPart.Chest, 0.6f },
                /*
                { TargetBodyPart.Groin, 0.1f },
                { TargetBodyPart.LeftFoot, 0.05f },
                */ //inkymed
            }
        },
        /*{
            TargetBodyPart.RightFoot, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightFoot, 0.2f },
                { TargetBodyPart.Chest, 0.6f },
                { TargetBodyPart.Groin, 0.1f },
                { TargetBodyPart.RightLeg, 0.1f },
            }
        },
        {
            TargetBodyPart.LeftFoot, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftFoot, 0.2f },
                { TargetBodyPart.Chest, 0.6f },
                { TargetBodyPart.Groin, 0.1f },
                { TargetBodyPart.LeftLeg, 0.1f },
            }
        },
        */  //inkymed
    };

    /// <summary>
    /// What noise does the entity play when swapping targets?
    /// </summary>
    [DataField]
    public string SwapSound = "/Audio/Effects/toggleoncombat.ogg";
}
