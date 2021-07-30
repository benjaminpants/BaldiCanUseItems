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

		public float CooldownTimer;

		public float BootsTimer;

		public float UnknownLocalTimer;

		public bool HasBootsOn = true;

        public PlayerManager FakePlayer;

		public Item ItemToWaitFor;

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
            AddItem(Items.DoorLock);
            AddItem(Items.Bsoda);
            ItemDisplayerObject = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<Pickup>()[0].itemSprite.gameObject);
            ItemDisplayerObject.transform.parent = MyBaldi.transform;
            ItemDisplayer = ItemDisplayerObject.GetComponent<SpriteRenderer>();
            ItemDisplayer.transform.localPosition = new Vector3(0.2f, 3, 0f);
            GameObject.Destroy(ItemDisplayer.GetComponent<PickupBob>());
            ItemDisplayer.enabled = false;

			//this creates a fake "PlayerManager", which allows me to use already existing item code for Baldi, I disable all the actual functionality of these classes in StupidPatches.cs
            FakePlayer = MyBaldi.gameObject.AddComponent<PlayerManager>();
            FakePlayer.playerNumber = 1;
            FakePlayer.ec = MyBaldi.ec;
            FakePlayer.itm = new GameObject().AddComponent<ItemManager>();
			FakePlayer.pc = new GameObject().AddComponent<PlayerClick>();
            _am.SetValue(FakePlayer,MyBaldi.GetComponent<ActivityModifier>());

			//A fake Gamecamera with no cameras assigned.
            GameCamera cam = new GameObject().AddComponent<GameCamera>();
			cam.transform.parent = MyBaldi.transform;
			(_cameras.GetValue(Singleton<CoreGameManager>.Instance) as GameCamera[])[1] = cam;

			//fake playermovement with its only purpose is being to redirect items to use Baldi's movement modifier
			PlayerMovement pm = new GameObject().AddComponent<PlayerMovement>();
			pm.transform.parent = MyBaldi.transform;
			pm.am = MyBaldi.GetComponent<ActivityModifier>();
			FakePlayer.plm = pm;




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

		public int GetNavCount(IntVector2 pos)
		{
			List<TileController> til = new List<TileController>();
			MyBaldi.ec.GetNavNeighbors(MyBaldi.ec.ClosestTileFromPos((pos)), til, PathType.Nav);
			return til.Count;
		}

		void Update()
		{
			if (MyBaldi == null) return;
			CooldownTimer -= Time.deltaTime * MyBaldi.ec.NpcTimeScale;

		}



        public bool UseIfExists(Items type)
        {
			if (CooldownTimer >= 0f) return false;
			CooldownTimer = 0.5f;
			if (DisplayItemTimer != 0f) return false;
			if (ItemToWaitFor != null) return false;
			if (BaldiUsableItems.Mode == BaldMode.Always) return true;
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

		private static readonly FieldInfo _anger = AccessTools.Field(typeof(Baldi), "anger");

		private static readonly FieldInfo _usesLeft = AccessTools.Field(typeof(SodaMachine), "usesLeft");

        static bool Prefix(Baldi __instance, ref bool ___controlOverride, ref Navigator ___navigator, ref bool ___eatingApple, ref bool ___paused, ref float ___speed, ref float ___nextSlapDistance, ref float ___extraAnger, ref AudioManager ___audMan, ref WeightedSoundObject[] ___eatSounds)
        {
            if (!___controlOverride)
            {

                if ((bool)_HasSoundLocation.Invoke(__instance, new object[0]))
                {
					NewBaldAI.Instance.UnknownLocalTimer = 0f;
					if (!___navigator.HasDestination)
					{
						_UpdateSoundTarget.Invoke(__instance, new object[0]);
					}
                }
                else
                {
					if (NewBaldAI.Instance.CurrentVisiblePlayer == null)
					{
						NewBaldAI.Instance.UnknownLocalTimer += Time.deltaTime * __instance.ec.NpcTimeScale;
					}
					else
					{
						NewBaldAI.Instance.UnknownLocalTimer = 0f;
					}
					if (!___navigator.HasDestination)
					{
						___navigator.WanderRandom();
					}

                }

            }

			if (NewBaldAI.Instance.UnknownLocalTimer > 60f) //please help i have literally no idea where the player is yolo
			{
				NewBaldAI.Instance.UnknownLocalTimer = 0f;
				if (NewBaldAI.Instance.UseIfExists(Items.Teleporter))
				{
					Item tele = GameObject.Instantiate<Item>(BaldiUsableItems.ItemStuffs.Find(x => x.itemType == Items.Teleporter).item);
					NewBaldAI.Instance.ItemToWaitFor = tele;
					tele.Use(NewBaldAI.Instance.FakePlayer);
				}
			}



			if (NewBaldAI.Instance.CurrentVisiblePlayer != null)
			{
				int neighbors = NewBaldAI.Instance.GetNavCount(new IntVector2((int)(NewBaldAI.Instance.CurrentVisiblePlayer.transform.position.x / 10f), (int)(NewBaldAI.Instance.CurrentVisiblePlayer.transform.position.y / 10f)));
				if (neighbors > 2 && (((float)_anger.GetValue(__instance) >= 2f)))
				{
					if (NewBaldAI.Instance.UseIfExists(Items.Bsoda))
					{
						Item bsod = GameObject.Instantiate<Item>(BaldiUsableItems.ItemStuffs.Find(x => x.itemType == Items.Bsoda).item);
						bsod.transform.name = "BALDSODA";
						bsod.Use(NewBaldAI.Instance.FakePlayer);
					}
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
				if (npc.Character == Character.LookAt) //pretty much, if baldi sees test, and can see the player, assume that the player can also see the test, and thus use the chalk thingy to prevent the test from freezing baldi
				{
					__instance.looker.Raycast(npc.transform, __instance.ec.MaxRaycast, out bool sighted);
					if (NewBaldAI.Instance.CurrentVisiblePlayer != null && sighted)
					{
						if (NewBaldAI.Instance.UseIfExists(Items.ChalkEraser))
						{
							Item chalk = GameObject.Instantiate<Item>(BaldiUsableItems.ItemStuffs.Find(x => x.itemType == Items.ChalkEraser).item);
							chalk.Use(NewBaldAI.Instance.FakePlayer);
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

            foreach (SwingDoor door in GameObject.FindObjectsOfType<SwingDoor>())
            {
                if ((Vector3.Distance(__instance.transform.position, door.audMan.transform.position) < 25f))
                {
					if (door.locked)
					{
						if (NewBaldAI.Instance.UseIfExists(Items.DetentionKey))
						{
							door.Unlock();
							//NewBaldAI.Instance.PauseBaldiWithoutBloat(1f, false);
						}
					}
					else
					{
						if (NewBaldAI.Instance.CurrentVisiblePlayer != null)
						{
							int neighbors = NewBaldAI.Instance.GetNavCount(new IntVector2((int)(NewBaldAI.Instance.CurrentVisiblePlayer.transform.position.x / 10f), (int)(NewBaldAI.Instance.CurrentVisiblePlayer.transform.position.y / 10f)));
							int myneighbors = NewBaldAI.Instance.GetNavCount(new IntVector2((int)(__instance.transform.position.x / 10f), (int)(__instance.transform.position.y / 12f)));
							bool isinhall = __instance.ec.ClosestTileFromPos(new IntVector2((int)(__instance.transform.position.x / 10f), (int)(__instance.transform.position.y / 12f))).room.type == RoomType.Hall;
							if (neighbors < 3 && myneighbors > 2 && isinhall) //oddly specific, but pretty much, if the player can only go forward and backward, and there is a swinging door next to baldi and he has more directions available to him, lock the door.
							{
								if (NewBaldAI.Instance.UseIfExists(Items.DoorLock))
								{
									door.LockTimed(30f);
									//NewBaldAI.Instance.PauseBaldiWithoutBloat(1f, false);
								}
							}
						}
					}
                }
            }

            if (Vector3.Distance(__instance.gameObject.transform.position, ___navigator.CurrentDestination) > 120f && (NewBaldAI.Instance.CurrentVisiblePlayer != null))
            {
                if (NewBaldAI.Instance.UseIfExists(Items.GrapplingHook))
                {
					Item grapple = GameObject.Instantiate<Item>(BaldiUsableItems.ItemStuffs.Find(x => x.itemType == Items.GrapplingHook).item);
					NewBaldAI.Instance.ItemToWaitFor = grapple;
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
