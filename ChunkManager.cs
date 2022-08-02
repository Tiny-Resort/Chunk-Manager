using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

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

        public static GameObject ChunkIndicator;

        public static Chunk currentChunk;
        public static ConfigEntry<KeyCode> ShowCurrentChunkHotkey;
        private static bool ShowCurrentChunk;

        public void Awake() {

            ShowCurrentChunkHotkey = Config.Bind("Keybinds", "ShowCurrentChunk", KeyCode.F3, "Pressing this key will show mark the current chunk as a square in the world.");
            
            Harmony harmony = new Harmony(pluginGuid);
            Tools.Initialize(harmony);
            //EditActiveChunks();

            MethodInfo OnTriggerEnter = AccessTools.Method(typeof(ChunkLoader), "OnTriggerEnter");
            MethodInfo OnTriggerEnterPrefix = AccessTools.Method(typeof(ChunkManager), "OnTriggerEnterPostfix");
            harmony.Patch(OnTriggerEnter, new HarmonyMethod(OnTriggerEnterPrefix));
            MethodInfo OnTriggerExit = AccessTools.Method(typeof(ChunkLoader), "OnTriggerExit");
            MethodInfo OnTriggerExitPrefix = AccessTools.Method(typeof(ChunkManager), "OnTriggerExitPrefix");
            harmony.Patch(OnTriggerExit, new HarmonyMethod(OnTriggerExitPrefix));

            MethodInfo updatePrefix = AccessTools.Method(typeof(ChunkManager), "updatePrefix");
            MethodInfo update = AccessTools.Method(typeof(CharInteract), "Update");
            harmony.Patch(update, new HarmonyMethod(updatePrefix));

            // Creates a chunk indicator
            ChunkIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ChunkIndicator.transform.localScale = new Vector3(20, 0.025f, 20);
            Destroy(ChunkIndicator.GetComponent<Collider>());
            var Rend = ChunkIndicator.GetComponent<MeshRenderer>();
            Rend.shadowCastingMode = ShadowCastingMode.Off;
            Rend.lightProbeUsage = LightProbeUsage.Off;
            Rend.material.color = new Color(1, 0, 0, 0.5f);
            ChunkIndicator.SetActive(ShowCurrentChunk);

        }

        // public void EditActiveChunks() {
        //     Config.Remove(new ConfigDefinition("SavedData", "ActiveChunkCount"));
        //     ActiveChunkCount = Config.Bind("SavedData", "ActiveChunkCount", ActiveChunks.Count, "IGNORE SETTING. DO NOT SET MANUALLY.");
        //     for (var i = 0; i < ActiveChunksConfig.Count; i++) { Config.Remove(new ConfigDefinition("SavedData", "ActiveChunk" + i)); }
        //     for (var i = 0; i < ActiveChunks.Count; i++) { ActiveChunksConfig.Add(Config.Bind("SavedData", "ActiveChunk" + i, ActiveChunks[i], "IGNORE SETTING. DO NOT SET MANUALLY.")); }
        //     Config.Save();
        // }

        [HarmonyPrefix]
        public static void updatePrefix(CharInteract __instance) {
            if (!__instance.isLocalPlayer) return;
            currentPosition = __instance.transform.position;
           // currentPosition = __instance.currentlyAttackingPos;
        }

        public void Update() {

            if (Input.GetKeyDown(ShowCurrentChunkHotkey.Value)) {
                ShowCurrentChunk = !ShowCurrentChunk;
                ChunkIndicator.SetActive(ShowCurrentChunk);    
            }
            
            if (ShowCurrentChunk) {
                FindCurrentChunk();
                ShowChunkInWorld();
            }

            // giveBackChunks
            // also load when loading into game
            // add selection of chunks to keep active
            // add visual element

        }

        // Finds which tile the player is currently in
        private void FindCurrentChunk() {
            for (var i = 0; i < WorldManager.manageWorld.chunksInUse.Count; i++) {
                var chunk = WorldManager.manageWorld.chunksInUse[i];
                if (currentPosition.x >= chunk.showingChunkX * 2 - 1 && currentPosition.x < chunk.showingChunkX * 2 + 19 &&
                    currentPosition.z >= chunk.showingChunkY * 2 - 1 && currentPosition.z < chunk.showingChunkY * 2 + 19) {
                    currentChunk = chunk;
                    break;
                }
            }
        }

        // Draws a "cube" indicating the exact area of the chunk the player is currently in
        private void ShowChunkInWorld() {
            if (currentChunk == null) return;
            ChunkIndicator.transform.position = new Vector3(
                currentChunk.showingChunkX * 2 + 9,
                currentPosition.y + 0.005f,
                currentChunk.showingChunkY * 2 + 9
            );
            
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
