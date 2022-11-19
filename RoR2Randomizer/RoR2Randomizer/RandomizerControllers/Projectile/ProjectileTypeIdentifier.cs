﻿using RoR2;
using RoR2.Projectile;
using RoR2Randomizer.Networking.ProjectileRandomizer.Orbs;
using RoR2Randomizer.RandomizerControllers.Projectile.BulletAttackHandling;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2Randomizer.RandomizerControllers.Projectile
{
    public readonly struct ProjectileTypeIdentifier : IEquatable<ProjectileTypeIdentifier>
    {
        public static readonly ProjectileTypeIdentifier Invalid = new ProjectileTypeIdentifier(ProjectileType.Invalid, -1);

        public readonly ProjectileType Type;
        public readonly int Index;

        public readonly bool IsValid => Type != ProjectileType.Invalid && Index != -1;

        public ProjectileTypeIdentifier(ProjectileType type, int index)
        {
            Type = type;
            Index = index;
        }

        public ProjectileTypeIdentifier(NetworkReader reader)
        {
            Type = (ProjectileType)reader.ReadPackedIndex32();
            Index = reader.ReadPackedIndex32();
        }

        public static ProjectileTypeIdentifier FromProjectilePrefab(GameObject prefab)
        {
            return new ProjectileTypeIdentifier(ProjectileType.OrdinaryProjectile, ProjectileCatalog.GetProjectileIndex(prefab));
        }

        public readonly void Serialize(NetworkWriter writer)
        {
            writer.WritePackedIndex32((int)Type);
            writer.WritePackedIndex32(Index);
        }

        public readonly void Fire(Vector3 origin, Quaternion rotation, float damage, float force, bool isCrit, GenericFireProjectileArgs genericArgs)
        {
            const string LOG_PREFIX = $"{nameof(ProjectileTypeIdentifier)}.{nameof(Fire)} ";

            genericArgs.ModifyArgs(ref origin);

#if DEBUG
            Log.Debug($"Firing {Type} in direction {rotation.eulerAngles}");
#endif

            //if (damageType.HasValue && damageType.Value == DamageType.Generic)
            //    damageType = null;

            Vector3 direction = (rotation * Vector3.forward).normalized;

            switch (Type)
            {
                case ProjectileType.OrdinaryProjectile:
                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        projectilePrefab = ProjectileCatalog.GetProjectilePrefab(Index),
                        crit = isCrit,
                        damage = damage,
                        force = force,
                        owner = genericArgs.Owner,
                        position = origin,
                        rotation = rotation,
                        target = genericArgs.TargetGO,
                        //damageTypeOverride = damageType
                    });
                    break;
                case ProjectileType.Bullet:
                    BulletAttackIdentifier bulletIdentifier = BulletAttackCatalog.Instance.GetIdentifier(Index);
                    if (!bulletIdentifier.IsValid)
                    {
                        Log.Warning(LOG_PREFIX + $"invalid bullet attack at index {Index}");
                        break;
                    }

                    BulletAttack bulletAttack = bulletIdentifier.CreateInstance();
                    bulletAttack.aimVector = direction;
                    bulletAttack.bulletCount = 1;
                    bulletAttack.damage = damage;
                    bulletAttack.force = force;
                    bulletAttack.hitEffectPrefab = EffectCatalog.GetEffectDef(bulletIdentifier.HitEffectIndex)?.prefab;
                    bulletAttack.isCrit = isCrit;
                    bulletAttack.origin = origin;
                    bulletAttack.owner = genericArgs.Owner;
                    bulletAttack.weapon = genericArgs.Weapon;
                    bulletAttack.muzzleName = genericArgs.MuzzleName;
                    bulletAttack.tracerEffectPrefab = EffectCatalog.GetEffectDef(bulletIdentifier.TracerEffectIndex)?.prefab;
                    
                    //if (damageType.HasValue)
                    //    bulletAttack.damageType |= damageType.Value;
                    
                    bulletAttack.Fire();
                    break;
                case ProjectileType.DamageOrb:
                case ProjectileType.LightningOrb:
                    new SpawnRandomizedOrbMessage(this, origin, rotation, damage, force, isCrit, genericArgs).SpawnOrSendMessage();
                    break;
                default:
                    Log.Warning(LOG_PREFIX + $"unhandled type {Type}");
                    break;
            }
        }

        public readonly bool Equals(ProjectileTypeIdentifier other)
        {
            return Type == other.Type && Index == other.Index;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is ProjectileTypeIdentifier other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            int hashCode = 686506176;
            hashCode = (hashCode * -1521134295) + Type.GetHashCode();
            hashCode = (hashCode * -1521134295) + Index.GetHashCode();
            return hashCode;
        }

        public override readonly string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{Type}: {Index}");

            if (Type == ProjectileType.OrdinaryProjectile)
            {
                sb.Append($" ({ProjectileCatalog.GetProjectilePrefab(Index)?.name ?? "null"})");
            }

            return sb.ToString();
        }

        public static bool operator ==(ProjectileTypeIdentifier a, ProjectileTypeIdentifier b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ProjectileTypeIdentifier a, ProjectileTypeIdentifier b)
        {
            return !a.Equals(b);
        }
    }
}
