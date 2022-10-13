﻿using R2API.Networking;
using RoR2Randomizer.Networking.BossRandomizer;
using RoR2Randomizer.Networking.CharacterReplacements;
using RoR2Randomizer.Networking.EffectRandomizer;
using RoR2Randomizer.Networking.ExplicitSpawnRandomizer;
#if !DISABLE_HOLDOUT_ZONE_RANDOMIZER
using RoR2Randomizer.Networking.HoldoutZoneRandomizer;
#endif
using RoR2Randomizer.Networking.ProjectileRandomizer;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoR2Randomizer.Networking
{
    public static class CustomNetworkMessageManager
    {
        public static void RegisterMessages()
        {
            NetworkingAPI.RegisterMessageType<SyncBossReplacementCharacter>();
            NetworkingAPI.RegisterMessageType<SyncExplicitSpawnReplacement>();

            NetworkingAPI.RegisterMessageType<SyncProjectileReplacements>();
            NetworkingAPI.RegisterMessageType<SyncCharacterMasterReplacements>();
            NetworkingAPI.RegisterMessageType<SyncEffectReplacements>();
#if !DISABLE_HOLDOUT_ZONE_RANDOMIZER
            NetworkingAPI.RegisterMessageType<SyncHoldoutZoneReplacements>();
#endif

#if DEBUG
            NetworkingAPI.RegisterMessageType<Debug.SyncConsoleLog>();
#endif
        }
    }
}
