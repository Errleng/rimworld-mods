using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimCheats
{
    [StaticConstructorOnStartup]
    internal class DefPatches
    {
        static HashSet<RecipeDef> patchedRecipes;

        static DefPatches()
        {
            patchedRecipes = new HashSet<RecipeDef>();
            //PatchRecipeProducts();
        }

        public static void PatchRecipeProducts()
        {
            foreach (var def in DefDatabase<RecipeDef>.AllDefs)
            {
                if (patchedRecipes.Contains(def))
                {
                    continue;
                }
                foreach (var product in def.products)
                {
                    if (product.thingDef.HasComp(typeof(CompQuality)))
                    {
                        // halve costs
                        foreach (var ingredient in def.ingredients)
                        {
                            var count = ingredient.GetBaseCount();
                            ingredient.SetBaseCount((int)Math.Ceiling(count / 2));
                        }
                    }
                    else
                    {
                        // double products
                        product.count *= 2;
                    }
                }
                patchedRecipes.Add(def);
            }
        }
    }
}
