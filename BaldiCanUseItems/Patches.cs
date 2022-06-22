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
    [HarmonyPatch(typeof(GottaSweep))]
    [HarmonyPatch("OnTriggerEnter")]
    class SweepOnTriggerEnterPatch
    {
        static bool Prefix(ref Collider other)
        {
            if (other.tag == "NPC" && other.isTrigger)
            {
                if (other.GetComponent<Baldi>())
                {
                    return !NewBaldAI.Instance.HasBootsOn;
                }
            }
            return true;
        }
    }
}
