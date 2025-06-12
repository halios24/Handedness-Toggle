using System;
using HarmonyLib;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace HandednessToggle;

public class Handedness
{
    // Set toggle keybind
    private static Key toggleHandednessKey = Plugin.modSettings.HandToggleKey;
    
    private static Player localPlayer;
    
    // Initialize togglestate handlers
    private static bool handednessToggle = false;
    private static bool keyWasPressed = false;
    
    // Set a unique prefix for your plugin
    private const string instancePrefix = "H_";
    // Generate the instance ID with a prefix
    private static string instanceId = GenerateInstanceId();
    private static string GenerateInstanceId()
    {
        var randomNumber = UnityEngine.Random.Range(10000, 99999);
        return instancePrefix + randomNumber.ToString();
    }
    
    /// <summary>
    /// Initialize and check player reference
    /// </summary>
    private static void EnsurePlayerReference()
    {
        if (Plugin.playerManager == null)
        {
            Plugin.playerManager = NetworkBehaviourSingleton<PlayerManager>.Instance;
        }
        if (localPlayer == null && Plugin.playerManager != null)
        {
            localPlayer = Plugin.playerManager.GetLocalPlayer();
            Plugin.Log(
                $"[Instance {instanceId}] Local player reference established: {(localPlayer != null ? "success" : "failed")}");
            handednessToggle = false;
        }
    }

    //method for taking input, toggling handedness variable
    private static void HandleHandednessToggleInput()
    {   
        bool isKeyPressed = Keyboard.current[toggleHandednessKey].isPressed; // Get key state
        
        if (isKeyPressed && !keyWasPressed) // Only occurs on first instance of key being pressed (rising edge)
        {
            // prevent triggering keybind while chat is open
            UIChat chat = NetworkBehaviourSingleton<UIChat>.Instance;
            if (chat.IsFocused) return;

            handednessToggle = !handednessToggle; // Invert toggle state

            string handednessState = handednessToggle ? "Left" : "Right"; // Assign string for printing to Log
            Plugin.Log($"[Instance {instanceId}] Handedness toggled: {handednessState}");
            
            // Determine the handedness based on the toggle
            PlayerHandedness handedness = handednessToggle ? PlayerHandedness.Left : PlayerHandedness.Right;

            // Call the method on the local player instance
            if (localPlayer != null)
            {
                localPlayer.Client_SetPlayerHandednessRpc(handedness); // change handedness
            }
            else
            {
                Plugin.LogError("Local player instance is null. Cannot set handedness.");
            }
        }
        keyWasPressed = isKeyPressed; // Set key state to previous state
    }

    
    //patch onto method PlayerCamera.OnTick
    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.OnTick))]
    private class PatchPlayerHandednessOnTick
    {
        private static void Postfix(Handedness __instance)
        {
            try
            {
                EnsurePlayerReference();
                HandleHandednessToggleInput();
                //run declared method
            }
            catch (Exception e)
            {
                Plugin.Log($" Error in camera tick: " + e.Message);
                throw;
            }

        }
    }
    
    // Event_OnPlayerHandednessChanged normally calls the ResetInputs method which moves the stick to the initial position
    [HarmonyPatch(typeof(PlayerInputController), "Event_OnPlayerHandednessChanged")]
    private class PatchEvent_OnPlayerHandednessChanged
    {
        // The transpiler runs when Patches are applied (on mod enable)
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Plugin.Log("transpiler activated");
            // Returns an empty list of instructions, which will remove the ResetInputs method call
            return new List<CodeInstruction>();
        }
    }
}

