﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Assets
{
    public static class AssetLoader
    {

        public static void LoadAllTextures()
        {
            AssetManager.LoadTexture("TurtleAttack", "Heroes/Turtle/Turtle_Attack_SS");
            AssetManager.LoadTexture("TurtleAttackVE", "Heroes/Turtle/Turtle_Attack_VE_SS");
            AssetManager.LoadTexture("TurtleWalk", "Heroes/Turtle/Turtle_Walk_SS");
            AssetManager.LoadTexture("TurtleIdle", "Heroes/Turtle/Turtle_Idle_SS");
            AssetManager.LoadTexture("TurtleMoveStartTrans", "Heroes/Turtle/Turtle_Move_Start_Trans_SS");
        }
    }
}
