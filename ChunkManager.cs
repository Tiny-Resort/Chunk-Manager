using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace TR {

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class ChunkManager : BaseUnityPlugin {

        public const string pluginGuid = "tinyresort.dinkum.ChunkManager";
        public const string pluginName = "Chunk Manager";
        public const string pluginVersion = "0.1.0";

        public static ConfigEntry<int> ActiveChunkCount;
        public static List<ConfigEntry<Vector2>> ActiveChunksConfig = new List<ConfigEntry<Vector2>>();

        public static List<Vector2> ActiveChunks = new List<Vector2>();
        public static Vector3 currentPosition;
        
        public void Awake() {
            Harmony harmony = new Harmony(pluginGuid);
            Tools.Initialize(harmony);
            EditActiveChunks();
            
            MethodInfo OnTriggerEnter = AccessTools.Method(typeof(ChunkLoader), "OnTriggerEnter");
            MethodInfo OnTriggerEnterPrefix = AccessTools.Method(typeof(ChunkManager), "OnTriggerEnterPostfix");
            harmony.Patch(OnTriggerEnter, new HarmonyMethod(OnTriggerEnterPrefix));
            MethodInfo OnTriggerExit = AccessTools.Method(typeof(ChunkLoader), "OnTriggerExit");
            MethodInfo OnTriggerExitPrefix = AccessTools.Method(typeof(ChunkManager), "OnTriggerExitPrefix");
            harmony.Patch(OnTriggerExit, new HarmonyMethod(OnTriggerExitPrefix));
            
            MethodInfo updatePrefix = AccessTools.Method(typeof(ChunkManager), "updatePrefix");
            MethodInfo update = AccessTools.Method(typeof(CharInteract), "Update");
            harmony.Patch(update, new HarmonyMethod(updatePrefix));
        }

        public void EditActiveChunks() {
            Config.Remove(new ConfigDefinition("SavedData", "ActiveChunkCount"));
            ActiveChunkCount = Config.Bind("SavedData", "ActiveChunkCount", ActiveChunks.Count, "IGNORE SETTING. DO NOT SET MANUALLY.");
            for (var i = 0; i < ActiveChunksConfig.Count; i++) { Config.Remove(new ConfigDefinition("SavedData", "ActiveChunk" + i)); }
            for (var i = 0; i < ActiveChunks.Count; i++) { ActiveChunksConfig.Add(Config.Bind("SavedData", "ActiveChunk" + i, ActiveChunks[i], "IGNORE SETTING. DO NOT SET MANUALLY.")); }
            Config.Save();
        }

        [HarmonyPrefix]
        public static void updatePrefix(CharInteract __instance) {
            if (!__instance.isLocalPlayer) return;
            currentPosition = __instance.transform.position;
        }

        public void Update() {

            if (Input.GetKeyDown(KeyCode.F12)) {
                
                Chunk closest = null;
                float closestDist = 10000000;
                
                Debug.Log(WorldManager.manageWorld.chunksInUse.Count + " ACTIVE CHUNKS:");
                
                for (var i = 0; i < WorldManager.manageWorld.chunksInUse.Count; i++) {
                    var chunk = WorldManager.manageWorld.chunksInUse[i];
                    var chunkDist = Vector3.Distance(currentPosition, new Vector3(chunk.showingChunkX * 2, currentPosition.y, chunk.showingChunkY * 2));
                    if (chunkDist < closestDist) {
                        closest = chunk;
                        closestDist = chunkDist;
                    }
                    Debug.Log(chunk.showingChunkX + ", " + chunk.showingChunkY);
                }
                
                if (closest != null) {
                    Debug.Log("PLAYER POSITION: " + currentPosition.x + ", " + currentPosition.z);
                    Debug.Log("CLOSEST CHUNK: " + closest.showingChunkX + ", " + closest.showingChunkY);
                    foreach (var tile in closest.chunksTiles) {
                        var GO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        GO.transform.localScale = new Vector3(2, 0.5f, 2);
                        GO.transform.parent = tile.transform;
                        GO.transform.localPosition = new Vector3(0, 1f, 0);
                    }
                }
                
            }
            
            // giveBackChunks
            // also load when loading into game
            // add selection of chunks to keep active
            // add visual element

        }

        [HarmonyPostfix]
        private static void OnTriggerEnterPostfix(ChunkLoader __instance, Collider other) {
            if (!__instance.myChunk) return;
            Debug.Log("Entering New Chunk: " + __instance.myChunk.showingChunkX + ", " + __instance.myChunk.showingChunkY);
        }

        [HarmonyPrefix]
        private static void OnTriggerExitPrefix(ChunkLoader __instance, Collider other) {
            if (!__instance.myChunk) return;
            Debug.Log("Exiting Chunk: " + __instance.myChunk.showingChunkX + ", " + __instance.myChunk.showingChunkY);
        }

    }

}
