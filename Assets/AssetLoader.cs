using Heroes_UnWelcomed.Encounters;
using Heroes_UnWelcomed.Libraries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Assets
{
    public static class AssetLoader
    {
        public static void LoadFonts()
        {
            AssetManager.LoadFont("Default", "Font/Default");
        }
        public static void LoadAllTextures()
        {
            AssetManager.LoadTexture("TurtleAttack", "Heroes/Turtle/Turtle_Attack_SS");
            AssetManager.LoadTexture("TurtleAttackVE", "Heroes/Turtle/Turtle_Attack_VE_SS");
            AssetManager.LoadTexture("TurtleWalk", "Heroes/Turtle/Turtle_Walk_SS");
            AssetManager.LoadTexture("TurtleIdle", "Heroes/Turtle/Turtle_Idle_SS");
            AssetManager.LoadTexture("TurtleMoveStartTrans", "Heroes/Turtle/Turtle_Move_Start_Trans_SS");

            AssetManager.LoadTexture("EmptyCell", "Cells/Empty_Cell");
            AssetManager.LoadTexture("EmptyCellMask", "Cells/Empty_Cell_Mask");

            AssetManager.LoadTexture("CombatCategoryIcon", "Encounter/CategoryIcons/CombatIcon");
            AssetManager.LoadTexture("TrapCategoryIcon", "Encounter/CategoryIcons/TrapIcon");

            AssetManager.LoadTexture("GoblinIcon", "Encounter/Goblin/GoblinIcon");
            AssetManager.LoadTexture("VampireIcon", "Encounter/Vampire/VampireIcon");
            AssetManager.LoadTexture("OrcIcon", "Encounter/Orc/OrcIcon");
        }
        public static void LoadCellAnimations()
        {
            List<string> allKeys = new List<string>();
            for (int i = 1; ; i++)
            {
                string key = $"EmptyCell{i}";
                string path = $"Cells/EmptyCellAnimation/EmptyCell{i}";
                if (!AssetManager.TryLoadRawTexture(key, path))
                {
                    break;
                }
                allKeys.Add(key);
            }
            AnimationLibrary.AddAnimationFrame("EmptyCell", AnimationType.Idle, allKeys, DrawPosition.Cell);
        }
        public static void LoadShaders()
        {
           // AssetManager.LoadEffect("ScrollWrap", "Shaders/ScrollWrap");

        }
        public static void LoadCombatEncounters()
        {
            var list = EncounterLibrary.ReturnAllEncounterNames();
            foreach (var encounter in list)
            {

                LoadAttackTextures(encounter, DrawPosition.Combatant);
                LoadBlockTextures(encounter, DrawPosition.Combatant);
                LoadDeathTextures(encounter, DrawPosition.Combatant);
                LoadIdleTextures(encounter, DrawPosition.Combatant);
                LoadPrepareTextures(encounter, DrawPosition.Combatant);
            }

        }
        public static void LoadHallwayEncounter()
        {
            var list = new List<string>();
            for (int i = 1; ; i++)
            {
                string key = $"Hallway{i}";
                string path = $"Encounter/Hallway/{key}";
                if (!AssetManager.TryLoadRawTexture(key, path))
                {
                    break;
                }
                list.Add(key);
            }
            AnimationLibrary.AddAnimationFrame("Hallway", AnimationType.Idle, list, DrawPosition.Hallway);
        }
        private static void LoadAnimationTextures(string encounter, AnimationType animName, DrawPosition drawPos )
        {
            List<string> allKeys = new List<string>();
            for (int i = 1; ; i++)
            {
                string key = $"{encounter}{animName}{i}";
                string path = $"Encounter/{encounter}/{key}";
                if (!AssetManager.TryLoadRawTexture(key, path))
                {
                    break;
                }
                allKeys.Add(key);
            }
            AnimationLibrary.AddAnimationFrame(encounter, animName, allKeys, drawPos);
        }
        private static void LoadAttackTextures(string encounter, DrawPosition drawPos)
            => LoadAnimationTextures(encounter, AnimationType.Attack, drawPos);
        private static void LoadBlockTextures(string encounter, DrawPosition drawPos)
            => LoadAnimationTextures(encounter, AnimationType.Block, drawPos);

        private static void LoadDeathTextures(string encounter, DrawPosition drawPos)
            => LoadAnimationTextures(encounter, AnimationType.Death, drawPos);

        private static void LoadIdleTextures(string encounter, DrawPosition drawPos)
            => LoadAnimationTextures(encounter, AnimationType.Idle, drawPos);

        private static void LoadPrepareTextures(string encounter, DrawPosition drawPos)
            => LoadAnimationTextures(encounter, AnimationType.Prepare, drawPos);


        public static void LoadSpeedControlTextures()
        {
            AssetManager.LoadTexture("Pause", "SpeedControl/Pause");
            AssetManager.LoadTexture("Play", "SpeedControl/Play");
            AssetManager.LoadTexture("2xSpeed", "SpeedControl/2xSpeed");
            AssetManager.LoadTexture("3xSpeed", "SpeedControl/3xSpeed");
        }
    }
}
