using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.EnumFolder
{

}
public enum AnimationType
{
    Idle,
    Walk,
    Attack,
    Block,
    Prepare,
    Death
}
public enum Direction
{
    Left,
    Right,
    Up,
    Down,

}
public enum DrawPosition
{
    Combatant,
    Hallway,
    Cell,
}
public enum ChargeType
{
    Travel,
    Exit,
    Combat
}
public enum EncounterType
{
    None,
    Combat,
    Trap,
    Puzzle,
    LockedDoor,
    Hallway,
    Exit
}
