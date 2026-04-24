using System;
using System.Collections.Generic;
using System.Linq;

namespace HvergiToolkit.Libs
{
    public static class AffinityHelper
    {
        public const int Minced = 32;
        public const int Chopped = 16;
        public const int CookingContainer = 75; // Frying Pan / Cauldron
        public const int SaltID = 99;

        public static readonly string[] CookingStationNames = { "Normal Oven", "Rare Oven", "Supreme Oven", "Fantastic Oven", "Normal Forge", "Rare Forge", "Supreme Forge", "Fantastic Forge" };
        public static readonly int[] CookingStations = { 40, 41, 42, 43, 42, 43, 44, 45 };

        public static readonly string[] MeatNames = { "Minced Bear", "Minced Beef", "Minced Canine", "Minced Feline", "Minced Fowl", "Minced Game", "Minced Horse", "Minced Humanoid", "Minced Insect", "Minced Lamb", "Minced Pork", "Minced Seafood", "Minced Snake", "Minced Tough" };
        public static readonly int[] Meats = { 16, 17, 18, 19, 21, 22, 23, 25, 26, 27, 28, 29, 30, 31 };

        public static readonly string[] CheeseNames = { "None", "Buffalo Cheese", "Cheese", "Feta Cheese", "Goat Cheese" };
        public static readonly int[] Cheeses = { 0, 87, 84, 86, 85 };

        public static readonly string[] LargeVegNames = { "Chopped Pumpkin", "Chopped Cabbage" };
        public static readonly int[] LargeVeggies = { 45, 42 };

        public static readonly string[] MedVegNames = { "Chopped Carrot", "Chopped Cucumber", "Chopped Potato", "Chopped Tomato" };
        public static readonly int[] MedVeggies = { 41, 17, 47, 43 };

        public static readonly string[] SmallVegNames = { "Chopped Corn", "Chopped Garlic", "Chopped Pea", "Chopped Pea Pod", "Chopped Onion" };
        public static readonly int[] SmallVeggies = { 44, 92, 58, 46, 91 };

        public static readonly string[] HerbNames = { "Chopped Thyme", "Ground Paprika", "Chopped Parsley", "Chopped Sage", "Ground Ginger", "Ground Cumin", "Ground Tumeric", "Chopped Belladonna", "Chopped Loveage", "Chopped Rosemary", "Chopped Mint", "Chopped Basil", "Chopped Oregano", "Ground Fennel Seed" };
        public static readonly int[] Herbs = { 96, 51, 94, 90, 49, 48, 52, 97, 89, 99, 38, 95, 93, 59 };

        public static readonly Dictionary<string, int> SkillIDs = new Dictionary<string, int>
        {
            {"Aggressive Fighting", 109}, {"Alchemy", 25}, {"Animal Husbandry", 130}, {"Animal Taming", 129}, {"Archaeology", 120},
            {"Archery", 16}, {"Armour Smithing", 22}, {"Axes", 11}, {"Baking", 62}, {"Beverages", 63},
            {"Blacksmithing", 59}, {"Blades Smithing", 54}, {"Body", 1}, {"Body Control", 3}, {"Body Stamina", 4}, {"Body Strength", 5},
            {"Botanizing", 122}, {"Bowyery", 103}, {"Butchering", 77}, {"Butchering knife", 73}, {"Carpentry", 78},
            {"Cartography", 135}, {"Carving Knife", 72}, {"Catapults", 128}, {"Chain Armour Smithing", 56}, {"Channeling", 118},
            {"Climbing", 123}, {"Cloth Tailoring", 52}, {"Clubs", 14}, {"Coal-Making", 97}, {"Cooking", 19},
            {"Dairy Food Making", 60}, {"Defensive Fighting", 110}, {"Digging", 44}, {"Exorcism", 119}, {"Farming", 33},
            {"Fighting", 28}, {"Fine Carpentry", 102}, {"Firemaking", 79}, {"First Aid", 112}, {"Fishing", 94},
            {"Fletching", 104}, {"Foraging", 121}, {"Forestry", 38}, {"Gardening", 36}, {"Halberd", 70}, {"Hammer", 91},
            {"Hammers", 15}, {"Hatchet", 50}, {"Healing", 29}, {"Hot Food Cooking", 61}, {"Huge Axe", 88}, {"Huge Club", 75},
            {"Jewelry Smithing", 101}, {"Knives", 12}, {"Large Axe", 87}, {"Large Maul", 65}, {"Large Metal Shield", 85},
            {"Large Wooden Shield", 83}, {"Leatherworking", 51}, {"Lock picking", 125}, {"Locksmithing", 95},
            {"Long bow", 132}, {"Long Spear", 69}, {"Longsword", 64}, {"Masonry", 53}, {"Mauls", 13}, {"Meditating", 37},
            {"Medium Bow", 133}, {"Medium Maul", 66}, {"Medium Metal Shield", 86}, {"Medium Wooden Shield", 82},
            {"Metallurgy", 99}, {"Milling", 98}, {"Mind", 0}, {"Mind Logic", 6}, {"Mind Speed", 7}, {"Mining", 43},
            {"Miscellaneous Items", 23}, {"Natural Substances", 100}, {"Nature", 26}, {"Normal Fighting", 111},
            {"Papyrusmaking", 34}, {"Paving", 92}, {"Pickaxe", 49}, {"Plate Armour Smithing", 57}, {"Polearms", 17},
            {"Pottery", 45}, {"Prayer", 117}, {"Preaching", 116}, {"Prospecting", 93}, {"Puppeteering", 106},
            {"Rake", 46}, {"Religion", 30}, {"Repairing", 96}, {"Restoration", 137}, {"Ropemaking", 47}, {"Saw", 76},
            {"Scythe", 47}, {"Sermon Warden", 116}, {"Settlement Planner", 137}, {"Shield Bashing", 114},
            {"Shield Smithing", 58}, {"Shields", 24}, {"Ship Building", 134}, {"Short Bow", 131}, {"Shortsword", 89},
            {"Sickle", 48}, {"Small Axe", 49}, {"Small Maul", 67}, {"Small Metal Shield", 84}, {"Small Wooden Shield", 81},
            {"Smithing", 21}, {"Soul", 2}, {"Soul Depth", 8}, {"Soul Strength", 9}, {"Staff", 71}, {"Stealing", 126},
            {"Stone Chisel", 74}, {"Stone Cutting", 124}, {"Swords", 10}, {"Tailoring", 18}, {"Taunting", 113},
            {"Thatching", 35}, {"Thievery", 31}, {"Toy Making", 107}, {"Toys", 27}, {"Tracking", 80}, {"Traps", 127},
            {"Trebuchets", 136}, {"Two Handed Sword", 90}, {"War Machines", 32}, {"Warhammer", 68},
            {"Weapon Heads Smithing", 55}, {"Weapon Smithing", 20}, {"Weaponless Fighting", 108}, {"Woodcutting", 48},
            {"Yoyo", 105}
        };

        // Moonshine Data
        public static readonly Dictionary<int, string> GrainNames = new Dictionary<int, string> { { 23, "Barley or Rye" }, { 25, "Oat or Wheat" } };
        public static readonly Dictionary<int, string> WaterNames = new Dictionary<int, string> { { 6, "Water" }, { 16, "Saltwater" } };
        public static readonly Dictionary<int, string> VegNames = new Dictionary<int, string> { { 42, "Cabbage" }, { 41, "Carrot" }, { 44, "Corn" }, { 17, "Cucumber" }, { 45, "Lettuce or Pumpkin" }, { 91, "Onion" }, { 46, "Pea Pods" }, { 58, "Peas" }, { 47, "Potato" }, { 43, "Tomato" } };

        public static readonly int[][] MoonshineRecipes = new int[][]
        {
            new[] {25, 6, 235, 17, 16}, new[] {23, 6, 376, 17, 16}, new[] {23, 16, 188, 58, 16}, new[] {23, 6, 94, 41, 0}, new[] {23, 6, 94, 42, 0},
            new[] {23, 6, 94, 43, 0}, new[] {23, 6, 47, 91, 0}, new[] {23, 6, 94, 45, 0}, new[] {23, 6, 94, 46, 0}, new[] {23, 6, 94, 47, 0},
            new[] {23, 6, 235, 45, 0}, new[] {23, 6, 235, 46, 0}, new[] {23, 6, 235, 47, 0}, new[] {23, 6, 376, 45, 0}, new[] {23, 6, 376, 46, 0},
            new[] {23, 6, 376, 47, 0}, new[] {23, 16, 47, 91, 0}, new[] {23, 16, 94, 45, 0}, new[] {23, 16, 94, 46, 0}, new[] {23, 6, 94, 41, 16},
            new[] {23, 6, 94, 42, 16}, new[] {23, 6, 94, 43, 16}, new[] {23, 6, 47, 91, 16}, new[] {23, 6, 94, 45, 16}, new[] {23, 6, 94, 46, 16},
            new[] {23, 6, 94, 47, 16}, new[] {23, 6, 141, 17, 0}, new[] {23, 6, 235, 46, 16}, new[] {23, 6, 235, 47, 16}, new[] {23, 6, 282, 17, 0},
            new[] {23, 6, 376, 46, 16}, new[] {23, 6, 376, 47, 16}, new[] {23, 6, 423, 17, 0}, new[] {23, 16, 94, 45, 16}, new[] {23, 16, 94, 46, 16},
            new[] {23, 16, 94, 47, 16}, new[] {23, 6, 94, 58, 16}, new[] {23, 16, 235, 46, 16}, new[] {23, 16, 235, 47, 16}, new[] {23, 6, 235, 58, 16},
            new[] {23, 16, 376, 46, 16}, new[] {23, 16, 376, 47, 16}, new[] {23, 6, 141, 17, 16}, new[] {25, 16, 376, 47, 16}, new[] {25, 6, 141, 17, 16},
            new[] {23, 6, 282, 17, 16}, new[] {23, 16, 94, 58, 16}, new[] {25, 6, 282, 17, 16}, new[] {23, 6, 423, 17, 16}, new[] {23, 16, 235, 58, 16},
            new[] {23, 6, 141, 41, 0}, new[] {23, 6, 141, 42, 0}, new[] {23, 6, 141, 43, 0}, new[] {23, 6, 94, 91, 0}, new[] {23, 6, 141, 45, 0},
            new[] {23, 6, 141, 46, 0}, new[] {23, 6, 141, 47, 0}, new[] {23, 6, 282, 45, 0}, new[] {23, 6, 282, 46, 0}, new[] {23, 6, 282, 47, 0},
            new[] {23, 6, 423, 45, 0}, new[] {23, 6, 423, 46, 0}, new[] {23, 6, 423, 47, 0}, new[] {23, 16, 94, 91, 0}, new[] {23, 16, 141, 45, 0},
            new[] {23, 16, 141, 46, 0}, new[] {23, 6, 141, 41, 16}, new[] {23, 6, 141, 42, 16}, new[] {23, 6, 141, 43, 16}, new[] {23, 6, 94, 91, 16},
            new[] {23, 6, 47, 17, 0}, new[] {23, 6, 141, 46, 16}, new[] {23, 6, 141, 47, 16}, new[] {23, 6, 188, 17, 0}, new[] {23, 6, 282, 46, 16},
            new[] {23, 6, 282, 47, 16}, new[] {23, 6, 329, 17, 0}, new[] {23, 6, 423, 46, 16}, new[] {23, 6, 423, 47, 16}, new[] {23, 16, 94, 91, 16},
            new[] {23, 16, 47, 17, 0}, new[] {23, 16, 141, 46, 16}, new[] {23, 16, 141, 47, 16}, new[] {23, 6, 141, 58, 16}, new[] {23, 16, 282, 46, 16},
            new[] {23, 16, 282, 47, 16}, new[] {23, 6, 47, 17, 16}, new[] {23, 16, 423, 46, 16}, new[] {23, 16, 423, 47, 16}, new[] {23, 6, 188, 17, 16},
            new[] {25, 16, 423, 47, 16}, new[] {25, 6, 188, 17, 16}, new[] {23, 16, 141, 58, 16}, new[] {23, 6, 235, 17, 0}, new[] {23, 6, 282, 41, 0},
            new[] {23, 6, 282, 42, 0}, new[] {23, 6, 282, 43, 0}, new[] {23, 6, 141, 91, 0}, new[] {25, 6, 235, 17, 0}, new[] {23, 16, 188, 17, 16},
            new[] {23, 6, 329, 17, 16}, new[] {23, 16, 235, 58, 16}, new[] {23, 6, 376, 17, 0}, new[] {23, 6, 282, 41, 16}, new[] {23, 6, 282, 42, 16},
            new[] {23, 6, 282, 43, 16}, new[] {23, 6, 141, 91, 16}, new[] {23, 6, 94, 17, 0}, new[] {23, 6, 282, 46, 0}, new[] {23, 6, 282, 47, 0},
            new[] {23, 6, 188, 41, 0}, new[] {23, 6, 188, 42, 0}, new[] {23, 6, 188, 43, 0}, new[] {23, 6, 141, 58, 16}, new[] {23, 6, 188, 45, 0},
            new[] {23, 6, 188, 46, 0}, new[] {23, 6, 188, 47, 0}, new[] {23, 6, 329, 45, 0}, new[] {23, 6, 329, 46, 0}, new[] {23, 6, 329, 47, 0},
            new[] {23, 6, 423, 17, 0}, new[] {23, 6, 423, 41, 0}, new[] {23, 6, 423, 42, 0}, new[] {23, 6, 423, 43, 0}, new[] {23, 6, 188, 91, 0},
            new[] {23, 16, 282, 41, 16}, new[] {23, 16, 282, 42, 16}, new[] {23, 16, 282, 43, 16}, new[] {23, 16, 141, 91, 16}, new[] {23, 6, 188, 41, 16},
            new[] {23, 6, 188, 42, 16}, new[] {23, 6, 188, 43, 16}, new[] {23, 6, 141, 91, 16}, new[] {23, 6, 94, 17, 16}, new[] {23, 6, 235, 41, 16},
            new[] {23, 6, 235, 42, 16}, new[] {23, 6, 235, 43, 16}, new[] {23, 6, 141, 17, 0}, new[] {23, 16, 188, 41, 16}, new[] {23, 16, 188, 42, 16},
            new[] {23, 16, 188, 43, 16}, new[] {23, 16, 141, 91, 16}, new[] {23, 6, 235, 46, 16}, new[] {23, 6, 235, 47, 16}
        };

        private struct MealData
        {
            public int StationIdx;
            public int MeatIdx;
            public int CheeseIdx;
            public int LargeVegIdx;
            public int Med1, Med2, Med3;
            public int Herb1, Herb2, Herb3;
            public bool HasSalt;
        }

        private static List<MealData>[] _listOfMeals = new List<MealData>[138];

        static AffinityHelper()
        {
            for (int i = 0; i < 138; i++) _listOfMeals[i] = new List<MealData>();
            PrecalculateMeals();
        }

        private static int PositiveMod(int n, int m)
        {
            return (n % m + m) % m;
        }

        private static void PrecalculateMeals()
        {
            int smallVegSum = SmallVeggies.Sum() + (SmallVeggies.Length * Chopped);

            for (int iStation = 0; iStation < CookingStations.Length; iStation++)
            {
                int stationVal = CookingStations[iStation] + CookingContainer + smallVegSum;
                for (int iMeat = 0; iMeat < Meats.Length; iMeat++)
                {
                    int meatVal = stationVal + Meats[iMeat] + Minced;
                    for (int iCheese = 0; iCheese < Cheeses.Length; iCheese++)
                    {
                        int cheeseVal = meatVal + Cheeses[iCheese];
                        for (int iLarge = 0; iLarge < LargeVeggies.Length; iLarge++)
                        {
                            int largeVal = cheeseVal + LargeVeggies[iLarge] + Chopped;
                            int[][] medRanges = { new[] { 0, 1, 2 }, new[] { 0, 1, 3 }, new[] { 0, 2, 3 }, new[] { 1, 2, 3 } };
                            foreach (var range in medRanges)
                            {
                                int medVal = largeVal;
                                foreach (int idx in range) medVal += MedVeggies[idx] + Chopped;

                                for (int h1 = 0; h1 < Herbs.Length - 2; h1++)
                                {
                                    for (int h2 = h1 + 1; h2 < Herbs.Length - 1; h2++)
                                    {
                                        for (int h3 = h2 + 1; h3 < Herbs.Length; h3++)
                                        {
                                            int finalBaseVal = medVal + Herbs[h1] + Chopped + Herbs[h2] + Chopped + Herbs[h3] + Chopped;
                                            
                                            // No Salt
                                            int idNoSalt = PositiveMod(finalBaseVal, 138);
                                            _listOfMeals[idNoSalt].Add(new MealData { StationIdx = iStation, MeatIdx = iMeat, CheeseIdx = iCheese, LargeVegIdx = iLarge, Med1 = range[0], Med2 = range[1], Med3 = range[2], Herb1 = h1, Herb2 = h2, Herb3 = h3, HasSalt = false });

                                            // With Salt (ID -99)
                                            int idWithSalt = PositiveMod(finalBaseVal + SaltID, 138);
                                            _listOfMeals[idWithSalt].Add(new MealData { StationIdx = iStation, MeatIdx = iMeat, CheeseIdx = iCheese, LargeVegIdx = iLarge, Med1 = range[0], Med2 = range[1], Med3 = range[2], Herb1 = h1, Herb2 = h2, Herb3 = h3, HasSalt = true });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static List<string> GetMealsForAffinity(int testMealSkillID, int wantedSkillID, int preferredStationIdx = -1, int preferredCheeseIdx = -1, bool includeSalt = false, int preferredMeatIdx = -1, HashSet<int> excludedHerbIndices = null)
        {
            int sTest = 175;
            int sWanted = PositiveMod(wantedSkillID - testMealSkillID + sTest, 138);

            var results = new List<string>();

            foreach (var r in _listOfMeals[sWanted])
            {
                if (preferredStationIdx != -1 && r.StationIdx != preferredStationIdx) continue;
                if (preferredCheeseIdx != -1 && r.CheeseIdx != preferredCheeseIdx) continue;
                if (r.HasSalt != includeSalt) continue;
                if (preferredMeatIdx != -1 && r.MeatIdx != preferredMeatIdx) continue;
                
                // Exclusion logic: If any herb in the recipe is excluded, skip it
                if (excludedHerbIndices != null && excludedHerbIndices.Count > 0)
                {
                    if (excludedHerbIndices.Contains(r.Herb1) || 
                        excludedHerbIndices.Contains(r.Herb2) || 
                        excludedHerbIndices.Contains(r.Herb3))
                    {
                        continue;
                    }
                }

                string desc = $"[b]Cooking Station:[/b] {CookingStationNames[r.StationIdx]}\n";
                desc += $"[b]Cooking Container:[/b] Frying Pan\n";
                desc += $"[b]Meat:[/b] {MeatNames[r.MeatIdx]}\n";
                if (r.CheeseIdx > 0) desc += $"[b]Cheese:[/b] {CheeseNames[r.CheeseIdx]}\n";
                
                string allVeggies = $"{LargeVegNames[r.LargeVegIdx]}, {MedVegNames[r.Med1]}, {MedVegNames[r.Med2]}, {MedVegNames[r.Med3]}, {string.Join(", ", SmallVegNames)}";
                desc += $"[b]Veggies:[/b] {allVeggies}\n";
                
                desc += $"[b]Herbs & Spices:[/b] {HerbNames[r.Herb1]}, {HerbNames[r.Herb2]}, {HerbNames[r.Herb3]}";
                if (r.HasSalt) desc += ", Salt";
                
                results.Add(desc);
                if (results.Count >= 20) break;
            }
            return results;
        }

        public static string GetMoonshineForAffinity(int testMealSkillID, int wantedSkillID)
        {
            int sTest = 175;
            int playerID = PositiveMod(wantedSkillID - testMealSkillID + sTest, 138);

            if (playerID >= MoonshineRecipes.Length) return "No moonshine recipe found for this ID.";

            var r = MoonshineRecipes[playerID];
            string grain = GrainNames.ContainsKey(r[0]) ? GrainNames[r[0]] : "Unknown Grain";
            string water = WaterNames.ContainsKey(r[1]) ? WaterNames[r[1]] : "Unknown Water";
            string veg = VegNames.ContainsKey(r[3]) ? VegNames[r[3]] : "Unknown Veg";
            string state = r[4] == 16 ? "Chopped " : "";

            return $"[b]Drink From Barrel: {playerID}[/b]\nNormal Oven, Cauldron, {grain}, {water}, {r[2] / 47} sugar, {state}{veg}.";
        }

        public static int GetMakerAffinityForID(int makerTestMealSkillID, int targetID)
        {
            int sTest = 175;
            return PositiveMod(targetID - sTest + makerTestMealSkillID, 138);
        }

        public static string GetSkillNameByID(int skillID)
        {
            foreach (var kvp in SkillIDs)
            {
                if (kvp.Value == skillID) return kvp.Key;
            }
            return "Unknown Skill";
        }
    }
}
