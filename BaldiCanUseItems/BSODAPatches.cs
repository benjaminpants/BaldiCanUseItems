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
using BBPlusNameAPI;
using System.Collections.Generic;
using System.Linq;

namespace BaldiCanUseItems
{
	[HarmonyPatch(typeof(ITM_BSODA))]
	[HarmonyPatch("OnTriggerEnter")]
	class BSODATriggerEnter
	{
		private static readonly FieldInfo _moveMod = AccessTools.Field(typeof(ITM_BSODA), "moveMod");
		private static readonly FieldInfo _activityMods = AccessTools.Field(typeof(ITM_BSODA), "activityMods");
		static bool Prefix(ITM_BSODA __instance, ref Collider other)
		{
			if (__instance.transform.name != "BALDSODA") return true;
			if (other.tag == "Player")
			{
				ActivityModifier component = other.GetComponent<ActivityModifier>();
				component.moveMods.Add((MovementModifier)_moveMod.GetValue(__instance));
				((List<ActivityModifier>)_activityMods.GetValue(__instance)).Add(component);
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(ITM_BSODA))]
	[HarmonyPatch("Update")]
	class BSODAUpdate
	{
		private static readonly FieldInfo _speed = AccessTools.Field(typeof(ITM_BSODA), "speed");

		static void Prefix(ITM_BSODA __instance)
		{
			if (__instance.transform.name != "BALDSODA") return;
			//_speed.SetValue(__instance,((float)_speed.GetValue(__instance)) - ((5f * Time.deltaTime) * NewBaldAI.Instance.MyBaldi.ec.EnvironmentTimeScale));
		}
	}

		[HarmonyPatch(typeof(ITM_BSODA))]
	[HarmonyPatch("OnTriggerExit")]
	class BSODATriggerExit
	{
		private static readonly FieldInfo _moveMod = AccessTools.Field(typeof(ITM_BSODA), "moveMod");
		private static readonly FieldInfo _activityMods = AccessTools.Field(typeof(ITM_BSODA), "activityMods");
		static bool Prefix(ITM_BSODA __instance, ref Collider other)
		{
			if (__instance.transform.name != "BALDSODA") return true;
			if (other.tag == "Player")
			{
				ActivityModifier component = other.GetComponent<ActivityModifier>();
				component.moveMods.Remove((MovementModifier)_moveMod.GetValue(__instance));
				((List<ActivityModifier>)_activityMods.GetValue(__instance)).Remove(component);
			}
			return false;
		}
	}
}
