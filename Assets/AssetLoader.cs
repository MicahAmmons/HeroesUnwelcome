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
            // Static, one-off textures
            AssetManager.LoadTexture("TurtleAttack", "Heroes/Turtle/Turtle_Attack_SS");
            AssetManager.LoadTexture("TurtleAttackVE", "Heroes/Turtle/Turtle_Attack_VE_SS");
            AssetManager.LoadTexture("TurtleWalk", "Heroes/Turtle/Turtle_Walk_SS");
            AssetManager.LoadTexture("TurtleIdle", "Heroes/Turtle/Turtle_Idle_SS");
            AssetManager.LoadTexture("TurtleMoveStartTrans", "Heroes/Turtle/Turtle_Move_Start_Trans_SS");

            AssetManager.LoadTexture("EmptyCell", "Cells/Empty_Cell");
            AssetManager.LoadTexture("EmptyCellMask", "Cells/Empty_Cell_Mask");

            AssetManager.LoadTexture("CombatCategoryIcon", "Encounter/CategoryIcons/CombatIcon");
            AssetManager.LoadTexture("TrapCategoryIcon", "Encounter/CategoryIcons/TrapIcon");
            AssetManager.LoadTexture("SpawnInIcon", "Encounter/CategoryIcons/SpawnInIcon");

            AssetManager.LoadTexture("GoblinIcon", "Encounter/Goblin/GoblinIcon");
            AssetManager.LoadTexture("VampireIcon", "Encounter/Vampire/VampireIcon");
            AssetManager.LoadTexture("OrcIcon", "Encounter/Orc/OrcIcon");
        }
        public static void LoadCellAnimations()
        {
            LoadSequentialAnimation(
                ownerKey: "EmptyCell",
                animType: AnimationType.Idle,
                drawPos: DrawPosition.Cell,
                keyPrefix: "EmptyCell",
                pathPrefix: "Cells/EmptyCellAnimation/");
        }

        public static void LoadShaders()
        {
        }

        public static void LoadHeroTextures()
        {
            LoadHeroAnimation(AnimationType.Walk, DrawPosition.Hero);
            LoadHeroAnimation(AnimationType.Idle, DrawPosition.Hero);
        }

        public static void LoadCombatEncounters()
        {
            var encounterNames = EncounterLibrary.ReturnAllEncounterNames();

            foreach (var encounter in encounterNames)
            {
                LoadEncounterAnimation(encounter, AnimationType.Attack, DrawPosition.Combatant);
                LoadEncounterAnimation(encounter, AnimationType.Block, DrawPosition.Combatant);
                LoadEncounterAnimation(encounter, AnimationType.Death, DrawPosition.Combatant);
                LoadEncounterAnimation(encounter, AnimationType.Idle, DrawPosition.Combatant);
                LoadEncounterAnimation(encounter, AnimationType.Prepare, DrawPosition.Combatant);
            }
        }

        public static void LoadHallwayEncounter()
        {
            LoadSequentialAnimation(
                ownerKey: "Hallway",
                animType: AnimationType.Idle,
                drawPos: DrawPosition.Hallway,
                keyPrefix: "Hallway",
                pathPrefix: "Encounter/Hallway/");
        }

        public static void LoadSpeedControlTextures()
        {
            AssetManager.LoadTexture("Pause", "SpeedControl/Pause");
            AssetManager.LoadTexture("Play", "SpeedControl/Play");
            AssetManager.LoadTexture("2xSpeed", "SpeedControl/2xSpeed");
            AssetManager.LoadTexture("3xSpeed", "SpeedControl/3xSpeed");
        }

        private static void LoadSequentialAnimation(
            string ownerKey,
            AnimationType animType,
            DrawPosition drawPos,
            string keyPrefix,
            string pathPrefix)
        {
            var keys = new List<string>();

            for (int i = 1; ; i++)
            {
                string key = $"{keyPrefix}{i}";
                string path = $"{pathPrefix}{key}";

                if (!AssetManager.TryLoadRawTexture(key, path))
                    break;

                keys.Add(key);
            }

            if (keys.Count > 0)
            {
                AnimationLibrary.AddAnimationFrame(ownerKey, animType, keys, drawPos);
            }
        }
        private static void LoadEncounterAnimation(string encounter, AnimationType animType, DrawPosition drawPos)
        {
            string keyPrefix = $"{encounter}{animType}";
            string pathPrefix = $"Encounter/{encounter}/";

            LoadSequentialAnimation(
                ownerKey: encounter,
                animType: animType,
                drawPos: drawPos,
                keyPrefix: keyPrefix,
                pathPrefix: pathPrefix);
        }
        private static void LoadHeroAnimation(AnimationType animType, DrawPosition drawPos)
        {
            string animName = animType.ToString(); 
            string keyPrefix = animName;
            string pathPrefix = $"Heroes/{animName}Animation/";

            LoadSequentialAnimation(
                ownerKey: "Hero",
                animType: animType,
                drawPos: drawPos,
                keyPrefix: keyPrefix,
                pathPrefix: pathPrefix);
        }
    }
}
