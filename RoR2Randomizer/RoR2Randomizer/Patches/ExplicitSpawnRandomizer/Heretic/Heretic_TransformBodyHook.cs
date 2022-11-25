﻿using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2Randomizer.Configuration;
using RoR2Randomizer.RandomizerControllers;
using RoR2Randomizer.RandomizerControllers.ExplicitSpawn;
using RoR2Randomizer.Utility;

namespace RoR2Randomizer.Patches.ExplicitSpawnRandomizer.Heretic
{
    [PatchClass]
    static class Heretic_TransformBodyHook
    {
        static void Apply()
        {
            IL.RoR2.CharacterMaster.OnInventoryChanged += CharacterMaster_OnInventoryChanged;
            On.RoR2.CharacterMaster.TransformBody += CharacterMaster_TransformBody;
        }

        static void Cleanup()
        {
            IL.RoR2.CharacterMaster.OnInventoryChanged -= CharacterMaster_OnInventoryChanged;
            On.RoR2.CharacterMaster.TransformBody -= CharacterMaster_TransformBody;
        }

        static void CharacterMaster_OnInventoryChanged(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ILCursor[] foundCursors;
            if (c.TryFindNext(out foundCursors,
                              x => x.MatchLdstr(Constants.BodyNames.HERETIC_NAME),
                              x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.TransformBody))))
            {
                foundCursors[1].EmitDelegate(static (string bodyName) =>
                {
                    if (ExplicitSpawnRandomizerController.IsActive)
                    {
                        if (ExplicitSpawnRandomizerController.TryGetReplacementBodyName(bodyName, out string replacementName))
                        {
                            return replacementName;
                        }
                    }

                    return bodyName;
                });
            }

            c.Index = 0;
            if (c.TryFindNext(out foundCursors,
                              x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.bodyIndex)),
                              x => x.MatchLdstr(Constants.BodyNames.HERETIC_NAME),
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => BodyCatalog.FindBodyIndex(default(string)))),
                              x => x.MatchBeq(out _)))
            {

                ILCursor cursor = foundCursors[3];

                ILLabel label = (ILLabel)cursor.Next.Operand;

                cursor.Index++;

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(static (CharacterMaster instance) =>
                {
                    if (ExplicitSpawnRandomizerController.IsActive && Caches.Masters.Heretic.isValid)
                    {
                        return instance.TryGetComponent(out ExplicitSpawnReplacementInfo replacementInfo) && replacementInfo.OriginalMasterIndex == Caches.Masters.Heretic;
                    }
                    else
                    {
                        return false;
                    }
                });

                cursor.Emit(OpCodes.Brtrue, label);
            }
        }

        static void CharacterMaster_TransformBody(On.RoR2.CharacterMaster.orig_TransformBody orig, CharacterMaster self, string bodyName)
        {
            if (ExplicitSpawnRandomizerController.IsActive && !self.GetComponent<ExplicitSpawnReplacementInfo>())
            {
                if (CharacterReplacements.IsAnyForcedCharacterModeEnabled ||
                    (ExplicitSpawnRandomizerController.TryGetOriginalBodyName(bodyName, out string originalBody) &&
                     originalBody == Constants.BodyNames.HERETIC_NAME))
                {
                    ExplicitSpawnRandomizerController.RegisterSpawnedReplacement(self.gameObject, Caches.Masters.Heretic);
                }
            }

            orig(self, bodyName);
        }
    }
}
