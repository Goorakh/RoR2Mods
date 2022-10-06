﻿using MonoMod.Cil;
using RoR2;
using RoR2Randomizer.RandomizerController.Buff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Networking;

namespace RoR2Randomizer.Patches.BuffRandomizer
{
    [PatchClass]
    public static class BuffIndexPatch
    {
        static void Apply()
        {
            On.RoR2.CharacterBody.SetBuffCount += CharacterBody_SetBuffCount;

            IL.RoR2.CharacterBody.AddBuff_BuffIndex += replaceReadBuffCountFromArray;
            IL.RoR2.CharacterBody.RemoveBuff_BuffIndex += replaceReadBuffCountFromArray;

#if DEBUG
            On.RoR2.CharacterBody.RemoveBuff_BuffDef += CharacterBody_RemoveBuff_BuffDef;
#endif
        }

        static void Cleanup()
        {
            On.RoR2.CharacterBody.SetBuffCount -= CharacterBody_SetBuffCount;

            IL.RoR2.CharacterBody.AddBuff_BuffIndex -= replaceReadBuffCountFromArray;
            IL.RoR2.CharacterBody.RemoveBuff_BuffIndex -= replaceReadBuffCountFromArray;

#if DEBUG
            On.RoR2.CharacterBody.RemoveBuff_BuffDef -= CharacterBody_RemoveBuff_BuffDef;
#endif
        }

#if DEBUG
        static void CharacterBody_RemoveBuff_BuffDef(On.RoR2.CharacterBody.orig_RemoveBuff_BuffDef orig, CharacterBody self, BuffDef buffDef)
        {
            Log.Debug("CharacterBody_RemoveBuff_BuffDef on client stacktrace: " + new System.Diagnostics.StackTrace().ToString());
            orig(self, buffDef);
        }
#endif

        static void CharacterBody_SetBuffCount(On.RoR2.CharacterBody.orig_SetBuffCount orig, CharacterBody self, BuffIndex buffType, int newCount)
        {
            BuffRandomizerController.TryReplaceBuffIndex(ref buffType);

            orig(self, buffType, newCount);
        }

        static void replaceReadBuffCountFromArray(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            while (c.TryFindNext(out ILCursor[] cursors, x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.buffs)), 
                                                                x => x.MatchLdelemI4()))
            {
                ILCursor last = cursors[cursors.Length - 1];
                last.EmitDelegate((int buffIndex) =>
                {
                    BuffRandomizerController.TryReplaceBuffIndex(ref buffIndex);
                    return buffIndex;
                });

                // Make sure it does not match the same instructions again
                c.Index = last.Index + 1;
            }
        }
    }
}
