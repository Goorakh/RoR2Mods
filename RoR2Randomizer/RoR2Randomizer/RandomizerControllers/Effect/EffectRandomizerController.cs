﻿using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2Randomizer.Configuration;
using RoR2Randomizer.Networking.EffectRandomizer;
using RoR2Randomizer.Utility;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2Randomizer.RandomizerControllers.Effect
{
    [RandomizerController]
    public sealed class EffectRandomizerController : MonoBehaviour
    {
        static readonly RunSpecific<bool> _hasRecievedEffectReplacementsFromServer = new RunSpecific<bool>();
        static readonly RunSpecific<ReplacementDictionary<EffectIndex>> _effectReplacements = new RunSpecific<ReplacementDictionary<EffectIndex>>((out ReplacementDictionary<EffectIndex> result) =>
        {
            if (NetworkServer.active && ConfigManager.Misc.EffectRandomizerEnabled)
            {
                result = ReplacementDictionary<EffectIndex>.CreateFrom(Enumerable.Range(0, EffectCatalog.effectCount).Select(i => (EffectIndex)i));

                SyncEffectReplacements.SendToClients(result);

                return true;
            }

            result = null;
            return false;
        });

        static bool shouldBeEnabled => ((NetworkServer.active && ConfigManager.Misc.EffectRandomizerEnabled) || (NetworkClient.active && _hasRecievedEffectReplacementsFromServer)) && _effectReplacements.HasValue;

        void setEffectReplacementsFromServerEvent(ReplacementDictionary<EffectIndex> replacements)
        {
            _effectReplacements.Value = replacements;
            _hasRecievedEffectReplacementsFromServer.Value = _effectReplacements.HasValue;
        }

        void Awake()
        {
            SyncEffectReplacements.OnCompleteMessageReceived += setEffectReplacementsFromServerEvent;
        }

        void OnDestroy()
        {
            _hasRecievedEffectReplacementsFromServer.Dispose();
            _effectReplacements.Dispose();

            SyncEffectReplacements.OnCompleteMessageReceived -= setEffectReplacementsFromServerEvent;
        }

        public static void TryReplaceEffectIndex(ref EffectIndex index)
        {
            if (shouldBeEnabled && _effectReplacements.Value.TryGetReplacement(index, out EffectIndex replacement))
            {
#if DEBUG
                Log.Debug($"Effect randomizer: replaced effect {EffectCatalog.GetEffectDef(index).prefabName} ({(int)index}) -> {EffectCatalog.GetEffectDef(replacement).prefabName} ({(int)replacement})");
                
#endif
                index = replacement;
            }
        }
    }
}
