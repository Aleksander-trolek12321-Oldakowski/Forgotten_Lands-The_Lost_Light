using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Player;

namespace Inventory
{
public static class InputBlocker
{
        static int blockCount = 0;

        static FieldInfo cinemachineGetInputField = null;
        static object prevCinemachineDelegate = null;

        public static void Block(PlayerBase player)
        {
            try
            {
                if (player != null)
                {
                    try { player.SetControlsEnabled(false); }
                    catch (Exception) { Debug.LogWarning("InputBlocker: Player.SetControlsEnabled not found or threw."); }
                }

                if (blockCount == 0)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;

                    TryBlockCinemachine();
                }

                blockCount++;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("InputBlocker.Block exception: " + ex.Message);
            }
        }

        public static void Restore(PlayerBase player)
        {
            try
            {
                if (player != null)
                {
                    try { player.SetControlsEnabled(true); }
                    catch (Exception) { Debug.LogWarning("InputBlocker: Player.SetControlsEnabled not found or threw."); }
                }

                blockCount = Math.Max(0, blockCount - 1);

                if (blockCount == 0)
                {
                    TryRestoreCinemachine();

                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }

            }
            catch (Exception ex)
            {
                Debug.LogWarning("InputBlocker.Restore exception: " + ex.Message);
            }
        }

        static void TryBlockCinemachine()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies();
                var cmType = asm.SelectMany(a =>
                {
                    try { return a.GetTypes(); } catch { return new Type[0]; }
                }).FirstOrDefault(t => t.FullName == "Cinemachine.CinemachineCore");

                if (cmType == null) return;

                cinemachineGetInputField = cmType.GetField("GetInputAxis", BindingFlags.Public | BindingFlags.Static);
                if (cinemachineGetInputField == null) return;

                prevCinemachineDelegate = cinemachineGetInputField.GetValue(null);

                MethodInfo mi = typeof(InputBlocker).GetMethod(nameof(AlwaysZero), BindingFlags.NonPublic | BindingFlags.Static);
                if (mi == null) return;

                var del = Delegate.CreateDelegate(cinemachineGetInputField.FieldType, mi);
                cinemachineGetInputField.SetValue(null, del);

                Debug.Log("InputBlocker: Cinemachine input blocked.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("InputBlocker: TryBlockCinemachine failed: " + e.Message);
                cinemachineGetInputField = null;
                prevCinemachineDelegate = null;
            }
        }

        static void TryRestoreCinemachine()
        {
            try
            {
                if (cinemachineGetInputField != null)
                {
                    cinemachineGetInputField.SetValue(null, prevCinemachineDelegate);
                    prevCinemachineDelegate = null;
                    cinemachineGetInputField = null;
                    Debug.Log("InputBlocker: Cinemachine input restored.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("InputBlocker: TryRestoreCinemachine failed: " + e.Message);
            }
        }

        static float AlwaysZero(string axisName) => 0f;
    }
}
