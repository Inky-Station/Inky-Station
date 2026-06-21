// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Medical.Common.Targeting;

/// <summary>
/// Represents a bitfield enum of possible target parts.
/// </summary>
/// <remarks>
/// To get all body parts as an Array, use static
/// method SharedTargetingSystem.GetValidParts.
/// </remarks>
[Flags]
public enum TargetBodyPart : ushort
{
    Head = 1,
    Chest = 1 << 1,
    // Groin = 1 << 2,      // inkymed start, if some chud ever changes something here i would need to fuck with upstreaming, i need to know what was nuked
    LeftArm = 1 << 2,       // was 3
    // LeftHand = 1 << 4,
    RightArm = 1 << 3,      // was 5
    // RightHand = 1 << 6,
    LeftLeg = 1 << 4,       // was 7
    // LeftFoot = 1 << 8,
    RightLeg = 1 << 5,      // was 9
    // RightFoot = 1 << 10,
    Tail = 1 << 6,          // was 11
    Wings = 1 << 7,         // inkymed end, was 12

    // Hands = LeftHand | RightHand,   inkymed
    Arms = LeftArm | RightArm,
    Legs = LeftLeg | RightLeg,
    // Feet = LeftFoot | RightFoot,    inkymed
    FullArms = Arms,                // inkymed - nuked Hands
    FullLegs = Legs,                // inkymed - nuked Feet
    BodyMiddle = Chest | FullArms,  // inkymed - nuked Groin
    FullLegsGroin = FullLegs,       // inkymed - nuked Groin

    All = Head | Chest | LeftArm | RightArm | LeftLeg | RightLeg | Tail | Wings, // inkymed - nuked Groin; LeftHand; RightHand; LeftFoot; RightFoot;
    Other = Tail | Wings,

    Vital = Head | Chest,
}

/// <summary>
/// Used by part cycling via scrolling
/// </summary>
public enum TargetBodyPartNonFlag : byte
{
    Head = 0,
    Chest,
    Groin,
    LeftArm,
    LeftHand,
    RightArm,
    RightHand,
    LeftLeg,
    LeftFoot,
    RightLeg,
    RightFoot,
    Tail,
    Wings,

    Max = RightFoot, // Wings and Tail do not work
}
