using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;
using System.Xml;
using System.Reflection;

namespace AnimalArmourFix
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("mehni.rimworld.animalarmour.main");

            harmony.Patch(AccessTools.Method(typeof(Recipe_RemoveHediff), "ApplyOnPawn"),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(RemoveHediff_Prefix)), null, null);

            harmony.Patch(AccessTools.Method(typeof(Recipe_RemoveHediff), "ApplyOnPawn"), null,
                new HarmonyMethod(typeof(HarmonyPatches), nameof(RemoveHediff_Postfix)), null);

            harmony.Patch(AccessTools.Method(typeof(Recipe_InstallImplant), "ApplyOnPawn"),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(SpawnHediff_Prefix)), null, null);
        }

        //store the thing to remove in a __state
        private static void RemoveHediff_Prefix(Recipe_RemoveHediff __instance, ref Pawn pawn, ref BodyPartRecord part, out Hediff __state)
        {
            BodyPartRecord affectedPart = part;
            Hediff hediff = pawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == __instance.recipe.removesHediff && x.Part == affectedPart && x.Visible);
            __state = hediff;
        }

        //retrieve the state (getting the hediff in the postfix when it is already removed is mighty difficult)
        //spawn the state.
        private static void RemoveHediff_Postfix(Recipe_RemoveHediff __instance, ref Pawn pawn, ref BodyPartRecord part, ref Pawn billDoer, Hediff __state)
        {
            Hediff hediff = __state;
            if (hediff != null && hediff.def.defName.Contains("AnimalArmor") && hediff.def.spawnThingOnRemoved != null)
            {
                GenSpawn.Spawn(hediff.def.spawnThingOnRemoved, billDoer.Position, billDoer.Map);
            }
        }

        private static void SpawnHediff_Prefix(Recipe_InstallImplant __instance, ref Pawn pawn, ref BodyPartRecord part, ref Pawn billDoer, ref List<Thing> ingredients, ref Bill bill)
        {
            if (!pawn.RaceProps.Humanlike)
            {
                if (__instance.recipe.addsHediff != null)
                {
                    if (__instance.recipe.addsHediff.defName.Contains("AnimalArmor"))
                    {
                        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                        for (int i = hediffs.Count - 1; i >= 0; i--)
                        {
                            Hediff hediff = hediffs[i];
                            if (hediff.Part == part && hediff.def.defName.Contains("AnimalArmor") && hediff.def.defName != __instance.recipe.addsHediff.defName)
                            {
                                Hediff hediff2 = hediffs[i];
                                pawn.health.RemoveHediff(hediff2);
                                GenSpawn.Spawn(hediff2.def.spawnThingOnRemoved, billDoer.Position, billDoer.Map);
                            }
                        }

                        // I wrote this beautiful reflection method, but vanilla fucks it up and spawns in animal legs like a pinata.
                        //MethodInfo methodInfo = typeof(Medicine).Assembly.CreateInstance("RimWorld.MedicalRecipesUtility").GetType().GetMethod("SpawnThingsFromHediffs");
                        //methodInfo.Invoke(null, new object[] { pawn, part, billDoer.Position, billDoer.Map });
                        return;
                    }
                }
            }
        }
    }
}