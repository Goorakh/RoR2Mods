﻿using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2Randomizer.Extensions;
using RoR2Randomizer.RandomizerController.Boss;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RoR2Randomizer.Patches.Fixes.Skills.EntityStates.NewtMonster
{
    public static class KickFromShop
    {
        public static void Apply()
        {
            IL.EntityStates.NewtMonster.KickFromShop.FixedUpdate += KickFromShop_FixedUpdate;
        }

        public static void Cleanup()
        {
            IL.EntityStates.NewtMonster.KickFromShop.FixedUpdate -= KickFromShop_FixedUpdate;
        }

        static void KickFromShop_FixedUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Fix NullRef in fixedupdate if "KickOutOfShop" does not exist in the scene
            if (c.TryGotoNext(x => x.MatchLdstr("KickOutOfShop"),
                              x => x.MatchCallvirt<Transform>(nameof(Transform.Find)),
                              x => x.MatchCallvirt(AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject)))))
            {
                c.Index += 2; // Move to before get_gameObject call

                Shared.Try_get_gameObject(c);
            }

            // Prevent Newt from becoming immune to damage if it's replacing Mithrix
            int localIndex = -1;
            if (c.TryGotoNext(x => x.MatchCallvirt(SymbolExtensions.GetMethodInfo<Component>(_ => _.GetComponent<HurtBoxGroup>())),
                              x => x.MatchStloc(out localIndex),
                              x => x.MatchLdloc(out int tmpLocIndex) && tmpLocIndex == localIndex,
                              x => x.MatchImplicitConversion<UnityEngine.Object, bool>()))
            {
                c.Index += 4; // Move before BrFalse

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((global::EntityStates.NewtMonster.KickFromShop instance) =>
                {
                    CharacterBody body = instance.characterBody;
                    if (body)
                    {
                        GameObject master = body.masterObject;
                        if (master)
                        {
                            return !BossRandomizerController.Mithrix.IsReplacedPartOfMithrixFight(master);
                        }
                    }

                    return true;
                });

                c.Emit(OpCodes.And);
            }
        }
    }
}
