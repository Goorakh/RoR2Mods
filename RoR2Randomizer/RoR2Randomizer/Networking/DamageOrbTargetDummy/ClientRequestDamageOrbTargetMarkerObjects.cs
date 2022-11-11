﻿using R2API.Networking.Interfaces;
using RoR2;
using RoR2Randomizer.Networking.Generic;
using RoR2Randomizer.RandomizerControllers.Projectile.DamageOrbHandling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity;
using UnityEngine;
using UnityEngine.Networking;
using UnityModdingUtility;

namespace RoR2Randomizer.Networking.DamageOrbTargetDummy
{
    public sealed class ClientRequestDamageOrbTargetMarkerObjects : NetworkMessageBase
    {
        uint _newObjectCount;
        NetworkUserId _requesterID;

        public ClientRequestDamageOrbTargetMarkerObjects()
        {
        }

        public ClientRequestDamageOrbTargetMarkerObjects(uint amount, NetworkUserId requesterID)
        {
            _newObjectCount = amount;
            _requesterID = requesterID;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt32(_newObjectCount);
            GeneratedNetworkCode._WriteNetworkUserId_None(writer, _requesterID);
        }

        public override void Deserialize(NetworkReader reader)
        {
            _newObjectCount = reader.ReadPackedUInt32();
            _requesterID = GeneratedNetworkCode._ReadNetworkUserId_None(reader);
        }

        public override void OnReceived()
        {
            const string LOG_PREFIX = $"{nameof(ClientRequestDamageOrbTargetMarkerObjects)}.{nameof(OnReceived)} ";

            if (!NetworkServer.active)
            {
                Log.Error(LOG_PREFIX + "called on client");
                return;
            }

            NetworkConnection requesterConnection = null;
            if (_requesterID.HasValidValue())
            {
                NetworkUser requesterUser = NetworkUser.readOnlyInstancesList.SingleOrDefault(user => user.id.value == _requesterID.value);
                if (requesterUser)
                {
                    requesterConnection = requesterUser.connectionToClient;
                }
                else
                {
                    Log.Warning(LOG_PREFIX + $"could not find {nameof(NetworkUser)} with id {_requesterID}");
                }
            }
            else
            {
                Log.Warning(LOG_PREFIX + "no valid requester ID");
            }

            if (requesterConnection == null)
            {
                Log.Warning(LOG_PREFIX + "unable to find requester connection");
                return;
            }

            IEnumerator waitForConnectionReadyAndCreateObjects()
            {
#if DEBUG
                float timeStarted = Time.time;
#endif

                while (requesterConnection != null && !requesterConnection.isReady)
                {
                    yield return 0;
                }

                if (requesterConnection == null)
                {
#if DEBUG
                    Log.Warning(LOG_PREFIX + "connection null in coroutine");
#endif
                    yield break;
                }

#if DEBUG
                Log.Debug(LOG_PREFIX + $"waited {Time.time - timeStarted:F2} seconds for connection ready");
#endif

                GameObject[] objects = new GameObject[_newObjectCount];
                for (int i = 0; i < _newObjectCount; i++)
                {
                    DamageOrbTargetDummyObjectMarker instantiated = DamageOrbTargetDummyObjectMarker.InstantiateNew();

                    objects[i] = instantiated.gameObject;

                    NetworkServer.SpawnWithClientAuthority(instantiated.gameObject, requesterConnection);
                }

                new Reply(objects).SendTo(requesterConnection);
            }

            Main.Instance.StartCoroutine(waitForConnectionReadyAndCreateObjects());
        }

        public sealed class Reply : NetworkMessageBase
        {
            public delegate void OnReceiveDelegate(DamageOrbTargetDummyObjectMarker[] newTargetObjects);
            public static event OnReceiveDelegate OnReceive;

            NetworkInstanceId[] _objectIDs;

            public Reply()
            {
                _objectIDs = Array.Empty<NetworkInstanceId>();
            }

            public Reply(GameObject[] objects)
            {
                _objectIDs = Array.ConvertAll(objects, static o => o.GetComponent<NetworkIdentity>().netId);
            }

            public override void Serialize(NetworkWriter writer)
            {
                writer.WritePackedUInt32((uint)_objectIDs.Length);
                foreach (NetworkInstanceId objectID in _objectIDs)
                {
                    writer.Write(objectID);
                }
            }

            public override void Deserialize(NetworkReader reader)
            {
                uint length = reader.ReadPackedUInt32();
                _objectIDs = new NetworkInstanceId[length];
                for (uint i = 0; i < length; i++)
                {
                    _objectIDs[i] = reader.ReadNetworkId();
                }
            }

            static IEnumerator waitForAllObjectsResolvedAndInvokeEvent(NetworkInstanceId[] objectIDs)
            {
                CoroutineOut<GameObject> resolvedObject = new CoroutineOut<GameObject>();

                DamageOrbTargetDummyObjectMarker[] resolvedTargetObjects = new DamageOrbTargetDummyObjectMarker[objectIDs.Length];
                for (int i = 0; i < resolvedTargetObjects.Length; i++)
                {
                    yield return SyncGameObjectReference.WaitForObjectResolved(objectIDs[i], null, resolvedObject);

                    if (resolvedObject.Result)
                    {
                        GameObject.DontDestroyOnLoad(resolvedObject.Result);
                        DamageOrbTargetDummyObjectMarker marker = resolvedObject.Result.GetComponent<DamageOrbTargetDummyObjectMarker>();
                        marker.IsAvailableToLocalPlayer = true;
                        resolvedTargetObjects[i] = marker;
                    }
                }

                OnReceive?.Invoke(resolvedTargetObjects);
            }

            public override void OnReceived()
            {
                const string LOG_PREFIX = $"{nameof(ClientRequestDamageOrbTargetMarkerObjects)}+{nameof(Reply)}.{nameof(OnReceived)} ";

#if DEBUG
                Log.Debug(LOG_PREFIX);
#endif

                Main.Instance.StartCoroutine(waitForAllObjectsResolvedAndInvokeEvent(_objectIDs));
            }
        }
    }
}
