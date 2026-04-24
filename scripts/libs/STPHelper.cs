using System;
using System.Collections.Generic;

namespace HvergiToolkit.Libs
{
    public static class STPHelper
    {
        public const double MaxLevel = 99.99999615;

        public static readonly Dictionary<string, double> SkillDifficulty = new Dictionary<string, double>
        {
            {"Mind", 300000.0},
            {"Body", 300000.0},
            {"Soul", 300000.0},
            {"Body control", 150000.0},
            {"Body stamina", 200000.0},
            {"Body strength", 200000.0},
            {"Mind logic", 200000.0},
            {"Mind speed", 200000.0},
            {"Soul depth", 200000.0},
            {"Soul strength", 200000.0},
            {"Swords", 10000.0},
            {"Axes", 10000.0},
            {"Knives", 10000.0},
            {"Mauls", 10000.0},
            {"Clubs", 10000.0},
            {"Hammers", 10000.0},
            {"Archery", 3000.0},
            {"Polearms", 10000.0},
            {"Tailoring", 20000.0},
            {"Cooking", 20000.0},
            {"Smithing", 20000.0},
            {"Weapon smithing", 20000.0},
            {"Armour smithing", 20000.0},
            {"Miscellaneous items", 20000.0},
            {"Shields", 4000.0},
            {"Alchemy", 20000.0},
            {"Nature", 20000.0},
            {"Toys", 20000.0},
            {"Fighting", 20000.0},
            {"Healing", 20000.0},
            {"Religion", 20000.0},
            {"Thievery", 4000.0},
            {"War machines", 10000.0},
            {"Farming", 4000.0},
            {"Papyrusmaking", 4000.0},
            {"Thatching", 4000.0},
            {"Gardening", 4000.0},
            {"Meditating", 2000.0},
            {"Forestry", 4000.0},
            {"Rake", 7000.0},
            {"Scythe", 7000.0},
            {"Sickle", 7000.0},
            {"Small Axe", 7000.0},
            {"Mining", 8000.0},
            {"Digging", 3000.0},
            {"Pickaxe", 7000.0},
            {"Shovel", 7000.0},
            {"Pottery", 4000.0},
            {"Ropemaking", 4000.0},
            {"Woodcutting", 4000.0},
            {"Hatchet", 7000.0},
            {"Leatherworking", 4000.0},
            {"Cloth tailoring", 4000.0},
            {"Masonry", 4000.0},
            {"Blades smithing", 4000.0},
            {"Weapon heads smithing", 4000.0},
            {"Chain armour smithing", 4000.0},
            {"Plate armour smithing", 4000.0},
            {"Shield smithing", 4000.0},
            {"Blacksmithing", 4000.0},
            {"Dairy food making", 4000.0},
            {"Hot food cooking", 4000.0},
            {"Baking", 2000.0},
            {"Beverages", 4000.0},
            {"Longsword", 4000.0},
            {"Large maul", 4000.0},
            {"Medium maul", 4000.0},
            {"Small maul", 4000.0},
            {"Warhammer", 4000.0},
            {"Long spear", 4000.0},
            {"Halberd", 4000.0},
            {"Staff", 4000.0},
            {"Carving knife", 4000.0},
            {"Butchering knife", 4000.0},
            {"Stone chisel", 4000.0},
            {"Huge club", 4000.0},
            {"Saw", 3000.0},
            {"Butchering", 4000.0},
            {"Carpentry", 4000.0},
            {"Firemaking", 4000.0},
            {"Tracking", 2000.0},
            {"Small wooden shield", 3000.0},
            {"Medium wooden shield", 3000.0},
            {"Large wooden shield", 3000.0},
            {"Small metal shield", 3000.0},
            {"Large metal shield", 3000.0},
            {"Medium metal shield", 3000.0},
            {"Large axe", 4000.0},
            {"Huge axe", 4000.0},
            {"Shortsword", 4000.0},
            {"Two handed sword", 4000.0},
            {"Hammer", 4000.0},
            {"Paving", 4000.0},
            {"Prospecting", 2000.0},
            {"Fishing", 3000.0},
            {"Locksmithing", 4000.0},
            {"Repairing", 4000.0},
            {"Coal-making", 2000.0},
            {"Milling", 2000.0},
            {"Metallurgy", 4000.0},
            {"Natural substances", 4000.0},
            {"Jewelry smithing", 4000.0},
            {"Fine carpentry", 4000.0},
            {"Bowyery", 4000.0},
            {"Fletching", 4000.0},
            {"Yoyo", 7000.0},
            {"Puppeteering", 2000.0},
            {"Toy making", 4000.0},
            {"Weaponless fighting", 4000.0},
            {"Aggressive fighting", 4000.0},
            {"Defensive fighting", 4000.0},
            {"Normal fighting", 4000.0},
            {"First aid", 4000.0},
            {"Taunting", 3000.0},
            {"Shield bashing", 3000.0},
            {"Milking", 4000.0},
            {"Preaching", 2000.0},
            {"Prayer", 4000.0},
            {"Channeling", 4000.0},
            {"Exorcism", 2000.0},
            {"Archaeology", 4000.0},
            {"Foraging", 4000.0},
            {"Botanizing", 4000.0},
            {"Climbing", 4000.0},
            {"Stone cutting", 4000.0},
            {"Lock picking", 2000.0},
            {"Stealing", 2000.0},
            {"Traps", 4000.0},
            {"Catapults", 4000.0},
            {"Animal taming", 4000.0},
            {"Animal husbandry", 4000.0},
            {"Short bow", 4000.0},
            {"Long bow", 4000.0},
            {"Medium bow", 4000.0},
            {"Ship building", 7000.0},
            {"Ballistae", 2000.0},
            {"Trebuchets", 2000.0},
            {"Restoration", 4000.0}
        };

        public static double GetStpTick(double lvl, double diff, bool isCreation)
        {
            if (lvl <= 0) lvl = 0.000001; // Avoid division by zero
            double times = 10.0;
            double skillmod = 1.5;
            double multi = (100.0 - lvl) / (diff * lvl * lvl);
            if (isCreation)
            {
                multi *= 0.3;
            }
            multi *= times;
            multi *= skillmod;
            return Math.Min(1.0, multi * lvl);
        }

        public static long GetStpToLevel(double targetLevel, double diff, bool isCreation)
        {
            long ticks = 0;
            double currentLevel = 1.0;
            while (currentLevel < targetLevel && currentLevel < MaxLevel)
            {
                currentLevel += GetStpTick(currentLevel, diff, isCreation);
                ticks += 1;
            }
            return ticks;
        }

        public static long GetStpFromLevelToLevel(double startLevel, double targetLevel, double diff, bool isCreation)
        {
            long ticks = 0;
            double currentLevel = Math.Max(1.0, startLevel);
            while (currentLevel < targetLevel && currentLevel < MaxLevel)
            {
                currentLevel += GetStpTick(currentLevel, diff, isCreation);
                ticks += 1;
            }
            return ticks;
        }

        public static double GetLevelAfterTicks(double currentLevel, long ticks, double diff, bool isCreation)
        {
            double level = Math.Max(1.0, currentLevel);
            for (long i = 0; i < ticks; i++)
            {
                level += GetStpTick(level, diff, isCreation);
                if (level >= MaxLevel)
                {
                    return MaxLevel;
                }
            }
            return level;
        }
    }
}
