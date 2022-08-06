using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace TinyResort {

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class ChunkManager : BaseUnityPlugin {

        public static TRPlugin Plugin;
        public const string pluginName = "ChunkManagement";
        public const string pluginGuid = "tinyresort.dinkum." + pluginName;
        public const string pluginVersion = "0.1.0";

        public static ConfigEntry<int> ActiveChunkCount;
        public static List<ConfigEntry<Vector2>> ActiveChunksConfig = new List<ConfigEntry<Vector2>>();

        public static List<Vector2> ActiveChunks = new List<Vector2>();
        public static Vector3 currentPosition;

        public static GameObject ChunkIndicator;

        public static Chunk currentChunk;
        public static ConfigEntry<KeyCode> ShowCurrentChunkHotkey;
        private static bool ShowCurrentChunk;

        public static ConfigEntry<KeyCode> ShowChunkGridHotkey;
        private static bool ShowChunkGrid;

        public GameObject MapGrid;
        private static bool TogglingChunk;

        public void Awake() {
            
            Plugin = TRTools.Initialize(this, Logger, 52, pluginGuid, pluginName, pluginVersion);
            Plugin.QuickPatch(typeof(ChunkLoader), "OnTriggerEnter", typeof(ChunkManager), "OnTriggerEnterPostfix");
            Plugin.QuickPatch(typeof(ChunkLoader), "OnTriggerExit", typeof(ChunkManager), "OnTriggerExitPrefix");
            Plugin.QuickPatch(typeof(CharInteract), "Update", typeof(ChunkManager), "updatePrefix");

            ShowCurrentChunkHotkey = Config.Bind("Keybinds", "ShowCurrentChunk", KeyCode.F3, "Pressing this key will show a red overlay on tiles within the player's current grid.");
            ShowChunkGridHotkey = Config.Bind("Keybinds", "ShowChunkGrid", KeyCode.F4, "Pressing this key will show a grid overlay on the map.");

            //EditActiveChunks();

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

            if (Input.GetKeyDown(ShowChunkGridHotkey.Value)) {
                ShowChunkGrid = !ShowChunkGrid;
                MapGrid.SetActive(ShowChunkGrid);
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

        // TODO: Make tiles on map clickable to activate chunks
        [HarmonyPrefix]
        public static bool MapUpdatePatch(RenderMap __instance) {

            if (!__instance.mapOpen || !ShowChunkGrid || __instance.selectTeleWindowOpen || __instance.iconSelectorOpen || !InputMaster.input.UISelect()) return true;
            TogglingChunk = true;
            //if ((!Inventory.inv.usingMouse || !InputMaster.input.Interact()) && (Inventory.inv.usingMouse || !InputMaster.input.Other())) { return true; }
            //RectTransformUtility.screen(__instance.mapImage.rectTransform, __instance.mapCursor.transform.position, null, out var _);

            return false;

        }

        // TODO: Make tiles on map clickable to activate chunks
        [HarmonyPostfix]
        public static void MapUpdatePostfix(RenderMap __instance) {

            if (!TogglingChunk) return;

            __instance.StopCoroutine("runIconSelector");
            __instance.iconSelectorOpen = false;
            __instance.iconSelectorWindow.SetActive(value: false);
            __instance.mapCursor.setPressing(isPressing: false);
            
            Debug.Log("Toggling Chunk");
            
            
            //if ((!Inventory.inv.usingMouse || !InputMaster.input.Interact()) && (Inventory.inv.usingMouse || !InputMaster.input.Other())) { return true; }
            //RectTransformUtility.screen(__instance.mapImage.rectTransform, __instance.mapCursor.transform.position, null, out var _);


        }

        // If showing the chunk grid, ignore interactions with icons in favor of toggling chunks
        [HarmonyPrefix]
        public static bool checkForIcon(RenderMap __instance, ref InvButton __result) {
            if (!ShowChunkGrid) { return true; }
            __result = null;
            return false;
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
