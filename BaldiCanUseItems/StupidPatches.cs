using System;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Net;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace BaldiCanUseItems
{
    [HarmonyPatch(typeof(GameCamera))]
    [HarmonyPatch("Awake")]
    class GameCamAwakePatch
    {
        static bool Prefix(GameCamera __instance)
        {
            if (__instance.camCom == null)
            {
                __instance.camNum = 99;
            }
            return __instance.camNum != 99;
        }
    }

    [HarmonyPatch(typeof(GameCamera))]
    [HarmonyPatch("Start")]
    class GameCamStartPatch
    {
        static bool Prefix(GameCamera __instance)
        {
            return __instance.camNum != 99;
        }
    }

	[HarmonyPatch(typeof(PlayerManager))]
	[HarmonyPatch("RuleBreak")]
	class PlayerManRuleBreakPatch
	{
		private static readonly MethodInfo _SetGuilt = AccessTools.Method(typeof(NPC), "SetGuilt");
		static bool Prefix(PlayerManager __instance, ref string rule, ref float linger)
		{
			if (__instance.tag == "Player") return true;
			_SetGuilt.Invoke(NewBaldAI.Instance.MyBaldi,new object[2] { linger, rule });

			return false;
		}
	}

	[HarmonyPatch(typeof(PlayerMovement))]
	[HarmonyPatch("Update")]
	class PlayerMoveUpdatePatch
	{
		static bool Prefix(PlayerMovement __instance)
		{
			return __instance.pm != null;
		}
	}

	[HarmonyPatch(typeof(PlayerClick))]
	[HarmonyPatch("Update")]
	class PlayerClickUpdatePatch
	{
		static bool Prefix(PlayerClick __instance)
		{
			return __instance.pm != null;
		}
	}

	[HarmonyPatch(typeof(PlayerManager))]
    [HarmonyPatch("Update")]
    class PlayerManUpdatePatch
    {
        static bool Prefix(PlayerManager __instance)
        {
            return __instance.tag == "Player";
		}
    }

    [HarmonyPatch(typeof(GameCamera))]
    [HarmonyPatch("LateUpdate")]
    class GameCamLateUpdatePatch
    {
        static bool Prefix(GameCamera __instance)
        {
            return __instance.camNum != 99;
        }
    }

    [HarmonyPatch(typeof(GameCamera))]
    [HarmonyPatch("StopRendering")]
    class GameCamStopRenderingPatch
    {
        static bool Prefix(GameCamera __instance)
        {
            return __instance.camNum != 99;
        }
    }



    [HarmonyPatch(typeof(ItemManager))]
    [HarmonyPatch("Awake")]
    class ItemManagerAwakePatch
    {
        static bool Prefix(ItemManager __instance)
        {
            if (__instance.pm == null)
            {
                __instance.maxItem = -1;
                __instance.pm = NewBaldAI.Instance.FakePlayer;
            }
            return __instance.maxItem != -1;
        }
    }

    [HarmonyPatch(typeof(ItemManager))]
    [HarmonyPatch("Update")]
    class ItemManagerUpdatePatch
    {
        static bool Prefix(ItemManager __instance)
        {
            return __instance.maxItem != -1;
        }
    }

    [HarmonyPatch(typeof(ItemManager))]
    [HarmonyPatch("SetItem")]
    class ItemManagerSetItemPatch
    {
        static bool Prefix(ItemManager __instance)
        {
            return __instance.maxItem != -1;
        }
    }

    [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.AddItem), typeof(ItemObject))]
    class AddItemPatch
    {
        static bool Prefix(ItemManager __instance, ref ItemObject item)
        {
            return __instance.maxItem != -1;
        }
    }

}
