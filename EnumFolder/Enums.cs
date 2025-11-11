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
}
public enum Direction
{
    Left,
    Right,
    Up,
    Down,

}
public enum Position
{
    TopLeft, TopMiddle, TopRight,
    MiddleLeft, Middle, MiddleRight,
    BottomLeft, BottomMiddle, BottomRight
}
public enum ChargeType
{
    Travel,
    Exit,
    Combat
}
