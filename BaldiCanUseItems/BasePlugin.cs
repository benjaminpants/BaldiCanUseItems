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
    [BepInPlugin("mtm101.rulerp.bbplus.baldiusesitems", "Baldi Can Use Items", "0.0.0.0")]
    public class BaldiUsableItems : BaseUnityPlugin
    {
        public static readonly List<ItemObject> ItemStuffs = Resources.FindObjectsOfTypeAll<ItemObject>().ToList();

        public static Dictionary<string, SoundObject> soundobjs = new Dictionary<string, SoundObject>();

        void Awake()
        {
            Harmony harmony = new Harmony("mtm101.rulerp.bbplus.baldiusesitems");
            harmony.PatchAll();
            AudioClip clip = FaDe.Unity.ResourceManager.Get("bal_purchase1.ogg");
            AudioClip clip2 = FaDe.Unity.ResourceManager.Get("bal_purchase2.ogg");
            AudioClip clip3 = FaDe.Unity.ResourceManager.Get("bal_purchase3.ogg");
            soundobjs.Add("bal_purchase1", CreateSoundObject(clip, "BAL_CUST_PURCHASE1", SoundType.Voice, Color.green));
            soundobjs.Add("bal_purchase2", CreateSoundObject(clip2, "BAL_CUST_PURCHASE2", SoundType.Voice, Color.green));
            soundobjs.Add("bal_purchase3", CreateSoundObject(clip3, "BAL_CUST_PURCHASE3", SoundType.Voice, Color.green));
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
