using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace EncyclopediaTool
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BasePlugin
    {
        public const string GUID = "com.encyclopedia_highlighter";
        public const string NAME = "Encyclopedia Highlighter";
        public const string VERSION = "1.0.0";
        internal static ManualLogSource LogSource;

        internal static HashSet<string> SlugsToHighlight = new HashSet<string>();

        public override void Load()
        {
            LogSource = Log;
            Harmony.CreateAndPatchAll(typeof(Patches));
            Log.LogInfo($"Encyclopedia Highlighter {VERSION} loaded.");
        }

        static class Patches
        {
            [HarmonyPatch(typeof(GameMgr), nameof(GameMgr.SetState))]
            [HarmonyPostfix]
            static void OnGameStateChanged(GameState st)
            {
                SlugsToHighlight.Clear();

                if ((st == GameState.kPickTreasure || st == GameState.kPaused) && BattleSaveData.I != null)
                {
                    if (BattleSaveData.I.Heroes != null)
                    {
                        foreach (var hero in BattleSaveData.I.Heroes)
                        {
                            if (hero != null && !hero.IsCombo() && hero.GetBaseInfo() != null)
                            {
                                SlugsToHighlight.Add(hero.GetBaseInfo().Slug);
                            }
                        }
                    }

                    if (BattleSaveData.I.Passives != null)
                    {
                        foreach (var passive in BattleSaveData.I.Passives)
                        {
                            if (passive != null)
                            {
                                var upg = passive.TryCast<UpgradeInst<PassiveInfo>>();
                                if (upg?.GetInfo() != null)
                                {
                                    SlugsToHighlight.Add(upg.GetInfo().Slug);
                                }
                            }
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(BaseMgr), nameof(BaseMgr.SetState))]
            [HarmonyPostfix]
            static void OnBaseStateChanged(BaseState st)
            {
                if (st == BaseState.kNormal)
                {
                    SlugsToHighlight.Clear();
                }
            }

            [HarmonyPatch(typeof(EncyclopediaItem), nameof(EncyclopediaItem.Init), new Type[] { typeof(UpgradeInfo) })]
            [HarmonyPostfix]
            static void OnEncycloItemInit(EncyclopediaItem __instance, UpgradeInfo inf)
            {
                if (SlugsToHighlight.Count > 0 && inf != null && SlugsToHighlight.Contains(inf.Slug))
                {
                    __instance.SetSelected(true);

                    if (GameMgr.I != null && (GameMgr.I.CurState == GameState.kPickTreasure || GameMgr.I.CurState == GameState.kPaused))
                    {
                        __instance.transform.SetAsFirstSibling();
                    }
                }
            }

            [HarmonyPatch(typeof(EncyclopediaItem), nameof(EncyclopediaItem.SetSelected))]
            [HarmonyPrefix]
            static bool PrefixSetSelected(EncyclopediaItem __instance, bool isSelected)
            {
                if (!isSelected && __instance.TgtInf != null && SlugsToHighlight.Contains(__instance.TgtInf.Slug))
                {
                    return false;
                }
                return true;
            }
        }
    }
}
