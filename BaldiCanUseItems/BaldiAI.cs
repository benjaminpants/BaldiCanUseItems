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
    public class NewBaldAI : MonoBehaviour
    {

        private static readonly FieldInfo _paused = AccessTools.Field(typeof(Baldi), "paused");

        private static readonly FieldInfo _animator = AccessTools.Field(typeof(Baldi), "animator");

        private static readonly FieldInfo _pauseTime = AccessTools.Field(typeof(Baldi), "pauseTime");

        private static readonly FieldInfo _am = AccessTools.Field(typeof(PlayerManager), "am");

        private static readonly FieldInfo _cameras = AccessTools.Field(typeof(CoreGameManager), "cameras");

        public static NewBaldAI Instance;

        public Baldi MyBaldi;

        public PlayerManager CurrentVisiblePlayer;

        public Items[] Inventory = new Items[5];
        public int[] Uses = new int[5];

        public GameObject ItemDisplayerObject;

        public SpriteRenderer ItemDisplayer;

        public float DisplayItemTimer;

        public float BootsTimer;

        public bool HasBootsOn = true;

        public PlayerManager FakePlayer;

        public void Awake()
        {
            MyBaldi = gameObject.GetComponent<Baldi>();
            if (MyBaldi == null)
            {
                throw new MissingComponentException("Baldi script not found!");
            }
            if (Instance != null)
            {
                GameObject.Destroy(Instance);
            }
            Instance = this;
            AddItem(Items.Quarter,5);
            AddItem(Items.ZestyBar,5);
            AddItem(Items.GrapplingHook);
            AddItem(Items.PrincipalWhistle,5);
            AddItem(Items.Scissors,2);
            ItemDisplayerObject = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<Pickup>()[0].itemSprite.gameObject);
            ItemDisplayerObject.transform.parent = MyBaldi.transform;
            ItemDisplayer = ItemDisplayerObject.GetComponent<SpriteRenderer>();
            ItemDisplayer.transform.localPosition = new Vector3(0.2f, 3, 0f);
            GameObject.Destroy(ItemDisplayer.GetComponent<PickupBob>());
            ItemDisplayer.enabled = false;



            FakePlayer = new GameObject().AddComponent<PlayerManager>();
            FakePlayer.playerNumber = 1;
            FakePlayer.ec = MyBaldi.ec;
            FakePlayer.transform.parent = MyBaldi.transform;
            FakePlayer.transform.localPosition = new Vector3(0f,0f,0f);
            FakePlayer.itm = new GameObject().AddComponent<ItemManager>();
            _am.SetValue(FakePlayer,MyBaldi.GetComponent<ActivityModifier>());

            GameCamera cam = new GameObject().AddComponent<GameCamera>();
            cam.transform.parent = MyBaldi.transform;
            (_cameras.GetValue(Singleton<CoreGameManager>.Instance) as GameCamera[])[1] = cam;

            Singleton<CoreGameManager>.Instance.disablePause = false;

            UnityEngine.Debug.Log("NewBaldi AI success!");
        }

        public IEnumerator DisplayTimer()
        {
            while (DisplayItemTimer > 0f)
            {
                DisplayItemTimer -= Time.deltaTime * MyBaldi.ec.NpcTimeScale;
                yield return null;
            }
            DisplayItemTimer = 0f;
            ItemDisplayer.enabled = false;
        }


        public IEnumerator BootsCountdown()
        {
            while (BootsTimer > 0f)
            {
                BootsTimer -= Time.deltaTime * MyBaldi.ec.NpcTimeScale;
                yield return null;
            }
            BootsTimer = 0f;
            HasBootsOn = false;
        }

        public void DisplayItem(Items type, float time)
        {
            ItemDisplayer.sprite = BaldiUsableItems.ItemStuffs.Find(x => x.itemType == type).itemSpriteLarge;
            DisplayItemTimer = time;
            if (!ItemDisplayer.enabled)
            {
                ItemDisplayer.enabled = true;
                StartCoroutine("DisplayTimer");
            }
        }


        public bool UseIfExists(Items type)
        {
            if (DisplayItemTimer != 0f) return false;
            for (int i = 0; i < Inventory.Length; i++)
            {
                if (Inventory[i] == type)
                {
                    DisplayItem(type,2f);
                    if (Uses[i] == 0)
                    {
                        Inventory[i] = Items.None;
                    }
                    else
                    {
                        Uses[i] = Uses[i] - 1;
                    }
                    return true;
                }
            }
            return false;
        }

        public void AddItem(Items type, int uses = 0)
        {
            for (int i = 0; i < Inventory.Length; i++)
            {
                if (Inventory[i] == Items.None)
                {
                    Inventory[i] = type;
                    Uses[i] = uses;
                    if (type == Items.GrapplingHook)
                    {
                        Uses[i] = 5;
                    }
                    return;
                }
            }
        }


        public void PutOnBootsNOW()
        {
            BootsTimer = 15f;
            if (!HasBootsOn)
            {
                HasBootsOn = true;
                StartCoroutine("BootsCountdown");
                return;
            }
        }


        public void PauseBaldiWithoutBloat(float time, bool playhappy)
        {
            if (!(bool)_paused.GetValue(MyBaldi))
            {
                _paused.SetValue(MyBaldi, true);
                _pauseTime.SetValue(MyBaldi, time);
                if (playhappy)
                {
                    ((Animator)_animator.GetValue(MyBaldi)).Play("BAL_Smile", -1, 0f);
                }
                MyBaldi.StartCoroutine("PauseTimer");
                return;
            }
            _pauseTime.SetValue(MyBaldi, (float)_pauseTime.GetValue(MyBaldi) + time);
        }

    }


    [HarmonyPatch(typeof(Baldi))]
    [HarmonyPatch("Start")]
    class CreateNewAI
    {
        static bool Prefix(Baldi __instance)
        {
            __instance.gameObject.AddComponent<NewBaldAI>();
            return true;
        }
    }

    [HarmonyPatch(typeof(Baldi))]
    [HarmonyPatch("PlayerInSight")]
    class PlayerInSightPatch
    {
        static void Prefix(ref PlayerManager player)
        {
            NewBaldAI.Instance.CurrentVisiblePlayer = player;
        }
    }

    [HarmonyPatch(typeof(Baldi))]
    [HarmonyPatch("PlayerLost")]
    class PlayerLostPatch
    {
        static void Prefix(ref PlayerManager player)
        {
            NewBaldAI.Instance.CurrentVisiblePlayer = null;
        }
    }

    [HarmonyPatch(typeof(Baldi))]
    [HarmonyPatch("Update")]
    class DisableDefaultBaldiAI
    {

        private static readonly MethodInfo _HasSoundLocation = AccessTools.Method(typeof(Baldi), "HasSoundLocation");

        private static readonly MethodInfo _UpdateSoundTarget = AccessTools.Method(typeof(Baldi), "UpdateSoundTarget");

        private static readonly FieldInfo _audWhistle = AccessTools.Field(typeof(ITM_PrincipalWhistle), "audWhistle");

        private static readonly FieldInfo _actMod = AccessTools.Field(typeof(Gum), "actMod");

        private static readonly FieldInfo _item = AccessTools.Field(typeof(SodaMachine), "item");

        private static readonly FieldInfo _usesLeft = AccessTools.Field(typeof(SodaMachine), "usesLeft");

        static bool Prefix(Baldi __instance, ref bool ___controlOverride, ref Navigator ___navigator, ref bool ___eatingApple, ref bool ___paused, ref float ___speed, ref float ___nextSlapDistance, ref float ___extraAnger, ref AudioManager ___audMan, ref WeightedSoundObject[] ___eatSounds)
        {
            if (!___controlOverride && !___navigator.HasDestination)
            {

                if ((bool)_HasSoundLocation.Invoke(__instance, new object[0]))
                {
                    _UpdateSoundTarget.Invoke(__instance, new object[0]);
                }
                else
                {
                    ___navigator.WanderRandom();
                }

            }


            //if the player is really far away, eat a zesty bar
            if (Vector3.Distance(__instance.gameObject.transform.position, ___navigator.CurrentDestination) > 80f)
            {
                if (!(___extraAnger >= 0f))
                {
                    if (NewBaldAI.Instance.UseIfExists(Items.ZestyBar))
                    {
                        ___audMan.PlaySingle(___eatSounds[0].selection);
                        __instance.GetExtraAnger(1f);
                    }
                }
            }

            if (NewBaldAI.Instance.CurrentVisiblePlayer != null)
            {
                if (NewBaldAI.Instance.CurrentVisiblePlayer.Disobeying && NewBaldAI.Instance.CurrentVisiblePlayer.ruleBreak != "Running")
                {
                    if (NewBaldAI.Instance.UseIfExists(Items.PrincipalWhistle))
                    {
                        foreach (NPC npc in __instance.ec.Npcs)
                        {
                            if (npc.Character == Character.Principal)
                            {
                                npc.GetComponent<Principal>().WhistleReact(__instance.transform.position);
                            }
                        }
                        Singleton<CoreGameManager>.Instance.audMan.PlaySingle((SoundObject)_audWhistle.GetValue(BaldiUsableItems.ItemStuffs.Find(x => x.itemType == Items.PrincipalWhistle).item));
                    }
                }
            }

            foreach (Gum gum in GameObject.FindObjectsOfType<Gum>())
            {
                if ((ActivityModifier)_actMod.GetValue(gum) == __instance.GetComponent<ActivityModifier>())
                {
                    if (NewBaldAI.Instance.UseIfExists(Items.Scissors))
                    {
                        gum.Cut();
                    }
                }
            }

            foreach (NPC npc in __instance.ec.Npcs)
            {
                if (npc.Character == Character.Sweep)
                {
                    __instance.looker.Raycast(npc.transform, __instance.ec.MaxRaycast, out bool sighted);
                    float distance = 0f;
                    if (NewBaldAI.Instance.CurrentVisiblePlayer != null)
                    {
                        distance = Vector3.Distance(NewBaldAI.Instance.CurrentVisiblePlayer.transform.position, npc.transform.position);
                    }
                    if (sighted && (Vector3.Distance(__instance.transform.position, npc.transform.position) < 100f) && !(distance < 40f) && !NewBaldAI.Instance.HasBootsOn)
                    {
                        if (NewBaldAI.Instance.UseIfExists(Items.Boots))
                        {
                            NewBaldAI.Instance.PutOnBootsNOW();
                        }
                    }
                }
            }

            foreach (SodaMachine machine in GameObject.FindObjectsOfType<SodaMachine>())
            {
                if (NewBaldAI.Instance.CurrentVisiblePlayer == null)
                {
                    // __instance.looker.Raycast(machine.gameObject.transform, __instance.ec.MaxRaycast, out bool sighted);
                    bool sighted = true;
                    if (sighted && (Vector3.Distance(__instance.transform.position, machine.gameObject.GetComponent<BoxCollider>().transform.position) < 15f) && ((int)_usesLeft.GetValue(machine) != 0))
                    {
                        if (NewBaldAI.Instance.UseIfExists(Items.Quarter))
                        {
                            machine.InsertItem(NewBaldAI.Instance.FakePlayer, NewBaldAI.Instance.MyBaldi.ec);
                            NewBaldAI.Instance.AddItem(((ItemObject)_item.GetValue(machine)).itemType);
                            NewBaldAI.Instance.PauseBaldiWithoutBloat(3f, true);
                            ___audMan.PlayRandomAudio(new SoundObject[3] { BaldiUsableItems.soundobjs["bal_purchase1"], BaldiUsableItems.soundobjs["bal_purchase2"], BaldiUsableItems.soundobjs["bal_purchase3"] });
                        }
                    }
                }
            }

            if (Vector3.Distance(__instance.gameObject.transform.position, ___navigator.CurrentDestination) > 120f && (NewBaldAI.Instance.CurrentVisiblePlayer != null))
            {
                if (NewBaldAI.Instance.UseIfExists(Items.GrapplingHook))
                {
                    Item grapple = GameObject.Instantiate<Item>(BaldiUsableItems.ItemStuffs.Find(x => x.itemType == Items.GrapplingHook).item);
                    grapple.Use(NewBaldAI.Instance.FakePlayer);
                }
            }

            if (!___eatingApple && !___paused)
            {
                ___nextSlapDistance += ___speed * Time.deltaTime * __instance.ec.NpcTimeScale;
            }


            if (___extraAnger > 0f)
            {
                __instance.GetAngry(0f);
                ___extraAnger -= Time.deltaTime * __instance.extraAngerDrain;
                if (___extraAnger < 0f)
                {
                    ___extraAnger = 0f;
                }
            }



            return false;
        }
    }

}
