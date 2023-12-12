using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace LethalEscape
{
	[BepInPlugin(modGUID, modName, modVersion)]
	public class Plugin : BaseUnityPlugin
	{
		private const string modGUID = "xCeezy.LethalEscape";
		private const string modName = "Lethal Escape";
		private const string modVersion = "0.5";
		private void Awake()
		{
			CanThumperEscape = Config.Bind("Can Monster Escape", "ThumperEscape", true, "Whether or not the Thumper can escape");
			CanBrackenEscape = Config.Bind("Can Monster Escape", "BrackenEscape", true, "Whether or not the Bracken can escape");
			CanJesterEscape = Config.Bind("Can Monster Escape", "JesterEscape", true, "Whether or not the Jester can escape (Semi broken)");
			CanHoardingBugEscape = Config.Bind("Can Monster Escape", "HoardingBugEscape", true, "Whether or not the Hoarding Bugs can escape");
			CanCoilHeadEscape = Config.Bind("Can Monster Escape", "CoilHeadEscape", true, "Whether or not Coil Head can escape");

			mls = BepInEx.Logging.Logger.CreateLogSource("GameMaster");
			// Plugin startup logic
			mls.LogInfo($"Loaded {modGUID}. Patching.");
			this._harmony.PatchAll(typeof(Plugin));



	}

		//------------ CRAWLER THUMPER AI----------------


		//--- CRAWLER THUMPER LEAVE CODE---
		[HarmonyPatch(typeof(CrawlerAI), "Update")]
		[HarmonyPrefix]
		static void CrawlerLEPrefixAI(CrawlerAI __instance)
		{
			if (CanThumperEscape.Value == true && __instance.IsOwner)
			{
				//--- CHECK TO MAKE SURE WE ARE INSIDE BEFORE CALCULATING ESCAPE CODE---
				if (!__instance.isOutside)
				{
					// Fail safe code that runs every 5 seconds to make sure we are still inside the facility
					if ((Math.Round(Time.time / .01) * .01) / 5 == Math.Round((Math.Round(Time.time / .01) * .01) / 5))
					{
						Transform ClosestNodePos = __instance.ChooseClosestNodeToPosition(__instance.transform.position, false, 0);
						if (Vector3.Magnitude(ClosestNodePos.position - __instance.transform.position) > 50)
						{
							Debug.Log(":Lethal Escape: FAILSAFE ACTIVATED AI STATE IS INSIDE BUT THEY ARE OUTSIDE POTENTIALLY?!?!!!!!!!!!!!!!!!!!!!!!!///////////////////////////////////////////////////////////////");
							SendEnemyOutside(__instance, true);
						}
					}
					//--- IS AI STILL ATTACKING---
					if (__instance.currentBehaviourStateIndex != 0)
					{

						//--- TIME SINCE TARGET LOST---
						if (__instance.noticePlayerTimer < -5f)
						{

							if (__instance.targetPlayer != null && !__instance.targetPlayer.isInsideFactory)
							{
								SendEnemyOutside(__instance, true);
							}

						}

					}
				}
				else
				{
					if ((Math.Round(Time.time / .01) * .01) / 10 == Math.Round((Math.Round(Time.time / .01) * .01) / 10))
					{
						__instance.allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
						if (Vector3.Magnitude(__instance.ChooseClosestNodeToPosition(__instance.transform.position, false, 0).position - __instance.transform.position) > 50)
						{

							Debug.Log(":Lethal Escape: AI IS OUTSIDE TYPE BUT IS STILL STUCK IN FACILITY SETTING OUTSIDE!!!!!!!!!!!!!!!!!!!!!!///////////////////////////////////////////////////////////////");
							SendEnemyOutside(__instance, true);

						}
					}
				}
			}
		}



		//------------ JESTER AI----------------


		//--- Jester LEAVE CODE---
		[HarmonyPatch(typeof(JesterAI), "Update")]
		[HarmonyPrefix]
		static void JesterLEPrefixAI(JesterAI __instance)
		{
			if (CanJesterEscape.Value == true && __instance.IsOwner)
			{ 
				if (!__instance.isOutside)
				{
					if ((Math.Round(Time.time / .01) * .01) / 5 == Math.Round((Math.Round(Time.time / .01) * .01) / 5))
					{
						Transform ClosestNodePos = __instance.ChooseClosestNodeToPosition(__instance.transform.position, false, 0);
						if (Vector3.Magnitude(ClosestNodePos.position - __instance.transform.position) > 50)
						{
							Debug.Log(":Lethal Escape: FAILSAFE ACTIVATED AI STATE IS INSIDE BUT THEY ARE OUTSIDE POTENTIALLY?!?!!!!!!!!!!!!!!!!!!!!!!///////////////////////////////////////////////////////////////");
							SendEnemyOutside(__instance, true);
						}
					}
					if (__instance.currentBehaviourStateIndex == 2 && (!__instance.targetPlayer.isInsideFactory || __instance.targetPlayer == null) )
					{
						SendEnemyOutside(__instance, true);
					}
				}
				else
				{


					if ((Math.Round(Time.time / .01) * .01) / 10 == Math.Round((Math.Round(Time.time / .01) * .01) / 10))
					{

						if (Vector3.Magnitude(__instance.ChooseClosestNodeToPosition(__instance.transform.position, false, 0).position - __instance.transform.position) > 50)
						{

							Debug.Log(":Lethal Escape: AI IS OUTSIDE TYPE BUT IS STILL STUCK IN FACILITY SETTING OUTSIDE!!!!!!!!!!!!!!!!!!!!!!///////////////////////////////////////////////////////////////");
							SendEnemyOutside(__instance, true);

						}
					}
				}
			}
		}

		//--- Jester half fix code---
		[HarmonyPatch(typeof(JesterAI), "Update")]
		[HarmonyPostfix]
		static void JesterLEPostfixAI(JesterAI __instance)
		{
			if (CanJesterEscape.Value == true)
			{
				if (__instance.isOutside)
				{
					bool flag = false;
					for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
					{
						if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && !StartOfRound.Instance.allPlayerScripts[i].isInsideFactory)
						{
							flag = true;
						}
					}
					if (flag)
					{
						__instance.SwitchToBehaviourState(2);
					}
				}
			}
		}

		//
		//------------ Bracken AI----------------


		[HarmonyPatch(typeof(FlowermanAI), "Start")]
		[HarmonyPrefix]
		static void FlowermanAILEPrefixStart(FlowermanAI __instance)
		{
			TimeStartTeleport = 0f;
		}

		//--- Bracken LEAVE CODE---
		[HarmonyPatch(typeof(FlowermanAI), "Update")]
		[HarmonyPrefix]
		static void FlowermanAILEPrefixAI(FlowermanAI __instance)
		{
			if (CanBrackenEscape.Value == true && __instance.IsOwner)
			{
				if (!__instance.isOutside)
				{
					if ((Math.Round(Time.time / .01) * .01) / 5 == Math.Round((Math.Round(Time.time / .01) * .01) / 5))
					{
						Transform ClosestNodePos = __instance.ChooseClosestNodeToPosition(__instance.transform.position, false, 0);
						if (Vector3.Magnitude(ClosestNodePos.position - __instance.transform.position) > 50)
						{
							Debug.Log(":Lethal Escape: FAILSAFE ACTIVATED AI STATE IS INSIDE BUT THEY ARE OUTSIDE POTENTIALLY?!?!!!!!!!!!!!!!!!!!!!!!!///////////////////////////////////////////////////////////////");
							SendEnemyOutside(__instance, true);
						}
					}
					if (__instance.targetPlayer != null && __instance.targetPlayer.isInsideFactory == false && (__instance.currentBehaviourStateIndex == 1 || __instance.evadeStealthTimer > 0))
					{

						if (Time.time - TimeStartTeleport >= 10)
						{
							TimeStartTeleport = Time.time;
						}
					}

					if (Time.time - TimeStartTeleport > 5 && Time.time - TimeStartTeleport < 10)
					{
						SendEnemyOutside(__instance, true);
					}
				}
				else
				{
					if ((Math.Round(Time.time / .01) * .01) / 10 == Math.Round((Math.Round(Time.time / .01) * .01) / 10))
					{
						__instance.allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
						if (Vector3.Magnitude(__instance.ChooseClosestNodeToPosition(__instance.transform.position, false, 0).position - __instance.transform.position) > 50)
						{
							Debug.Log(":Lethal Escape: AI IS OUTSIDE TYPE BUT IS STILL STUCK IN FACILITY SETTING OUTSIDE!!!!!!!!!!!!!!!!!!!!!!///////////////////////////////////////////////////////////////");
							SendEnemyOutside(__instance, true);
						}
					}
				}
			}
		}


		//------------ Hoarder Bug AI----------------


		//--- HOARDING BUG LEAVE CODE---
		[HarmonyPatch(typeof(HoarderBugAI), "Update")]
		[HarmonyPrefix]
		static void HoardingBugAILEPrefixAI(HoarderBugAI __instance)
		{
			if (CanHoardingBugEscape.Value == true && __instance.IsOwner)
			{
				if (!__instance.isOutside)
				{
					if ((Math.Round(Time.time / .01) * .01) / 5 == Math.Round((Math.Round(Time.time / .01) * .01) / 5))
					{
						Transform ClosestNodePos = __instance.ChooseClosestNodeToPosition(__instance.transform.position, false, 0);
						if (Vector3.Magnitude(ClosestNodePos.position - __instance.transform.position) > 50)
						{
							Debug.Log(":Lethal Escape: FAILSAFE ACTIVATED AI STATE IS INSIDE BUT THEY ARE OUTSIDE POTENTIALLY?!?!!!!!!!!!!!!!!!!!!!!!!///////////////////////////////////////////////////////////////");
							SendEnemyOutside(__instance, true);
						}
					}

					if (__instance.targetPlayer != null &&__instance.targetPlayer.isInsideFactory == false && __instance.searchForPlayer.inProgress == true)
					{
						SendEnemyOutside(__instance, true);
					}

				}
				else
				{
					if ((Math.Round(Time.time / .01) * .01) / 10 == Math.Round((Math.Round(Time.time / .01) * .01) / 10))
					{
						__instance.allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
						if (Vector3.Magnitude(__instance.ChooseClosestNodeToPosition(__instance.transform.position, false, 0).position - __instance.transform.position) > 50)
						{

							Debug.Log(":Lethal Escape: AI IS OUTSIDE TYPE BUT IS STILL STUCK IN FACILITY SETTING OUTSIDE!!!!!!!!!!!!!!!!!!!!!!///////////////////////////////////////////////////////////////");
							SendEnemyOutside(__instance, true);

						}
					}
				}
			}
		}

		//------------ Coil Head AI----------------


		//--- Coil Head LEAVE CODE---
		[HarmonyPatch(typeof(SpringManAI), "Update")]
		[HarmonyPrefix]
		static void CoilHeadAILEPrefixAI(SpringManAI __instance)
		{
			if (CanCoilHeadEscape.Value == true && __instance.IsOwner)
			{
				if (!__instance.isOutside)
				{
					if ((Math.Round(Time.time / .01) * .01) / 5 == Math.Round((Math.Round(Time.time / .01) * .01) / 5))
					{
						Transform ClosestNodePos = __instance.ChooseClosestNodeToPosition(__instance.transform.position, false, 0);
						if (Vector3.Magnitude(ClosestNodePos.position - __instance.transform.position) > 50)
						{
							Debug.Log(":Lethal Escape: FAILSAFE ACTIVATED AI STATE IS INSIDE BUT THEY ARE OUTSIDE POTENTIALLY?!?!!!!!!!!!!!!!!!!!!!!!!///////////////////////////////////////////////////////////////");
							SendEnemyOutside(__instance, true);
						}
					}
					if (__instance.targetPlayer != null && !__instance.targetPlayer.isInsideFactory)
					{
						SendEnemyOutside(__instance, false);
					}
				}
				else
				{

					if ((Math.Round(Time.time / .01) * .01) / 10 == Math.Round((Math.Round(Time.time / .01) * .01) / 10))
					{
						__instance.allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
						if (Vector3.Magnitude(__instance.ChooseClosestNodeToPosition(__instance.transform.position, false, 0).position - __instance.transform.position) > 50)
						{
							SendEnemyOutside(__instance, true);
						}
					}
				}
			}
		}

		public static void SendEnemyInside(EnemyAI __instance)
		{

			__instance.isOutside = false;
			__instance.allAINodes = GameObject.FindGameObjectsWithTag("AINode");


			//--- FIND MAIN ENTERANCE ---
			EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(false);
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].entranceId == 0 && !array[j].isEntranceToBuilding)
				{
					__instance.serverPosition = array[j].entrancePoint.position;
					break;
				}
			}

			Transform ClosestNodePos = __instance.ChooseClosestNodeToPosition(__instance.serverPosition, false, 0);

			if (Vector3.Magnitude(ClosestNodePos.position - __instance.serverPosition) > 10)
			{
				__instance.serverPosition = ClosestNodePos.position;
				__instance.transform.position = __instance.serverPosition;
			}

			__instance.transform.position = __instance.serverPosition;

			__instance.agent.Warp(__instance.serverPosition);
			__instance.SyncPositionToClients();

		}
		public static void SendEnemyOutside(EnemyAI __instance, bool SpawnOnDoor = true)
		{
			__instance.isOutside = true;
			__instance.allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
			


			//--- FIND ENTERANCE DOOR CLOSEST TO PLAYERS
			EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(false);
			float ClosestexitDistance = 999;
			for (int j = 0; j < array.Length; j++)
			{

				if (array[j].isEntranceToBuilding)
				{
					for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
					{
						if (!StartOfRound.Instance.allPlayerScripts[i].isInsideFactory & Vector3.Magnitude(StartOfRound.Instance.allPlayerScripts[i].transform.position - array[j].entrancePoint.position) < ClosestexitDistance)
						{
							ClosestexitDistance = Vector3.Magnitude(StartOfRound.Instance.allPlayerScripts[i].transform.position - array[j].entrancePoint.position);
							__instance.serverPosition = array[j].entrancePoint.position;
						}
					}

				}
			}

			Transform ClosestNodePos = __instance.ChooseClosestNodeToPosition(__instance.serverPosition, false, 0);

			if (Vector3.Magnitude(ClosestNodePos.position - __instance.serverPosition) > 10 || SpawnOnDoor == false)
			{
				__instance.serverPosition = ClosestNodePos.position;
				__instance.transform.position = __instance.serverPosition;
			}
			__instance.transform.position = __instance.serverPosition;

			__instance.agent.Warp(__instance.serverPosition);
			__instance.SyncPositionToClients();

			if (GameNetworkManager.Instance.localPlayerController != null)
			{
				__instance.EnableEnemyMesh(!StartOfRound.Instance.hangarDoorsClosed || !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom, false);
			}
		}

		private Harmony _harmony = new Harmony("LethalEscape");
		public static ManualLogSource mls;
		public static float TimeStartTeleport;
		public static ConfigEntry<bool> CanThumperEscape;
		public static ConfigEntry<bool> CanBrackenEscape;
		public static ConfigEntry<bool> CanJesterEscape;
		public static ConfigEntry<bool> CanHoardingBugEscape;
		public static ConfigEntry<bool> CanCoilHeadEscape;
	}

	

}



