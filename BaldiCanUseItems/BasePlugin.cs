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
using FaDe; //haha funny mod menu dependency


namespace BaldiCanUseItems
{

	public enum BaldMode
	{
		RandomEveryDeath,
		Constant,
		Collect,
		RandomPerFloor,
		Always,
		Cycle
	}



	[BepInPlugin("mtm101.rulerp.bbplus.baldiusesitems", "Baldi Can Use Items", "0.0.0.0")]
    public class BaldiUsableItems : BaseUnityPlugin
    {
        public static readonly List<ItemObject> ItemStuffs = Resources.FindObjectsOfTypeAll<ItemObject>().ToList();

        public static Dictionary<string, SoundObject> soundobjs = new Dictionary<string, SoundObject>();


		public static BaldMode Mode = BaldMode.Always;

		public void SetAlways(Name_MenuObject yes)
		{
			Mode = BaldMode.Always;
			NameMenuManager.AllowContinue(true);
		}

		public void SetROD(Name_MenuObject yes)
		{
			Mode = BaldMode.RandomEveryDeath;
			NameMenuManager.AllowContinue(true);
		}

		public void SetRPF(Name_MenuObject yes)
		{
			Mode = BaldMode.RandomPerFloor;
			NameMenuManager.AllowContinue(true);
		}

		public void SetCycle(Name_MenuObject yes)
		{
			Mode = BaldMode.Cycle;
			NameMenuManager.AllowContinue(true);
		}


		void Awake()
		{
			Harmony harmony = new Harmony("mtm101.rulerp.bbplus.baldiusesitems");
			AudioClip clip = FaDe.Unity.ResourceManager.Get("bal_purchase1.ogg");
			AudioClip clip2 = FaDe.Unity.ResourceManager.Get("bal_purchase2.ogg");
			AudioClip clip3 = FaDe.Unity.ResourceManager.Get("bal_purchase3.ogg");
			soundobjs.Add("bal_purchase1", CreateSoundObject(clip, "BAL_CUST_PURCHASE1", SoundType.Voice, Color.green));
			soundobjs.Add("bal_purchase2", CreateSoundObject(clip2, "BAL_CUST_PURCHASE2", SoundType.Voice, Color.green));
			soundobjs.Add("bal_purchase3", CreateSoundObject(clip3, "BAL_CUST_PURCHASE3", SoundType.Voice, Color.green));


			NameMenuManager.AddPreStartPage("mandatoryitemconfig", true);
			List<Name_MenuObject> Objects = new List<Name_MenuObject>();
			Objects.Add(new Name_MenuGeneric("setalways", "Always", SetAlways));
			Objects.Add(new Name_MenuGeneric("setrod", "Randomize On Death", SetROD));
			Objects.Add(new Name_MenuGeneric("setrpf", "Randomize Per Seed", SetRPF));
			Objects.Add(new Name_MenuGeneric("setcycle", "Cycle", SetCycle));
			NameMenuManager.AddToPageBulk("mandatoryitemconfig", Objects);


			harmony.PatchAll();


		}


        public static SoundObject CreateSoundObject(AudioClip clip, string subtitle, SoundType type, Color color, float sublength = -1f)
        {
            SoundObject obj = ScriptableObject.CreateInstance<SoundObject>();
            obj.soundClip = clip;
            obj.subDuration = sublength == -1 ? clip.length + 1f : sublength;
            obj.soundType = type;
            obj.soundKey = subtitle;
            obj.color = color;
            return obj;

        }
    }
}
