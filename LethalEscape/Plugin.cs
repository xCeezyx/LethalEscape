using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace LethalEscape
{
	[BepInPlugin(modGUID, modName, modVersion)]
	public class Plugin : BaseUnityPlugin
	{
		public static GameObject ExtraValues;

		private const string modGUID = "xCeezy.LethalEscape";
		private const string modName = "Lethal Escape";
		private const string modVersion = "0.8.2";
		private void Awake()
		{
			CanThumperEscape = Config.Bind("Can Monster Escape", "ThumperEscape", true, "Whether or not the Thumper can escape");
			CanBrackenEscape = Config.Bind("Can Monster Escape", "BrackenEscape", true, "Whether or not the Bracken can escape");
			CanJesterEscape = Config.Bind("Can Monster Escape", "JesterEscape", true, "Whether or not the Jester can escape");
			CanHoardingBugEscape = Config.Bind("Can Monster Escape", "HoardingBugEscape", true, "Whether or not the Hoarding Bugs can escape");
			CanCoilHeadEscape = Config.Bind("Can Monster Escape", "CoilHeadEscape", true, "Whether or not Coil Head can escape");
			CanSpiderEscape = Config.Bind("Can Monster Escape", "SpiderEscape", true, "Whether or not the Spider can escape");
			CanNutCrackerEscape = Config.Bind("Can Monster Escape", "NutCrackerEscape", true, "Whether or not the Nut Cracker can escape");
			CanHygrodereEscape = Config.Bind("Can Monster Escape", "HygrodereEscape", false, "Whether or not the Hygrodere/Slime can escape");
			CanPufferEscape = Config.Bind("Can Monster Escape", "CanPufferEscape", true, "Whether or not the Puffer/Spore Lizard can escape");

			ThumperChanceToSpawnOutside = Config.Bind("Spawn Escape Settings", "ThumperSpawnOutsideChance", 5f, "The chance that the Thumper will spawn outside");
			BrackenChanceToSpawnOutside = Config.Bind("Spawn Escape Settings", "BrackenSpawnOutsideChance", 10f, "The chance that the Bracken will spawn outside");
			JesterChanceToSpawnOutside = Config.Bind("Spawn Escape Settings", "JesterSpawnOutsideChance", 0f, "The chance that the Jester will spawn outside");
			HoardingBugChanceToSpawnOutside = Config.Bind("Spawn Escape Settings", "HoardingBugSpawnOutsideChance", 30f, "The chance that the Hoarding Bug will spawn outside");
			SpiderChanceToSpawnOutside = Config.Bind("Spawn Escape Settings", "SpiderSpawnOutsideChance", 5f, "The chance that the Spider will spawn outside");
			NutCrackerChanceToSpawnOutside = Config.Bind("Spawn Escape Settings", "NutCrackerSpawnOutsideChance", 5f, "The chance that the NutCracker will spawn outside");
			HygrodereChanceToSpawnOutside = Config.Bind("Spawn Escape Settings", "HygrodereSpawnOutsideChance", 0f, "The chance that the Hygrodere will spawn outside");
			PufferChanceToSpawnOutside = Config.Bind("Spawn Escape Settings", "PufferSpawnOutsideChance", 5f, "The chance that the Puffer will spawn outside");


			PufferChanceToEscapeEveryMinute = Config.Bind("Escape Settings", "Puffer Escape Chance", 10f, "The chance that the Puffer/Spore Lizard will escape randomly every minute");
			BrackenChanceToEscapeEveryMinute = Config.Bind("Escape Settings", "Bracken Escape Chance", 10f, "The chance that the Bracken will escape randomly every minute");
			HoardingBugChanceToEscapeEveryMinute = Config.Bind("Escape Settings", "HoardingBug Escape Chance", 15f, "The chance that the Hoarding Bug will escape randomly every minute");

			HoardingBugChanceToNestNearShip = Config.Bind("Escape Settings", "HoardingBugShipNestChance", 50f, "The chance that the Hoarding Bug will make their nest at/near the ship");

			

			BrackenEscapeDelay = Config.Bind("Escape Settings", "Bracken Escape Delay", 5f, "Time it takes for the Bracken to follow a player outside");
			ThumperEscapeDelay = Config.Bind("Escape Settings", "Thumper Escape Delay", -5f, "Time it takes for the Thumper to follow a player outside which is based on how long the player was seen minus this value (Might break when under -15 and will force thumper outside when it sees someone when above 0)");

			SpiderMaxWebsOutside = Config.Bind("Escape Settings", "Max Spider Webs Outside", 28, "The maximum amount of spider webs the game will allow the spider to create webs outside (Vanilla game is 7 or 9 if update 47)");
			SpiderMinWebsOutside = Config.Bind("Escape Settings", "Min Spider Webs Outside", 20, "The minimum amount of spider webs the game will allow the spider to create webs outside (Vanilla game is 4 or 6 if update 47)");

			JesterSpeedIncreasePerSecond = Config.Bind("Escape Settings", "Jester Speed Increase", 1.35f, "How much speed the jester gets per second");
			MaxJesterOutsideSpeed = Config.Bind("Escape Settings", "Jester Outside Speed", 10f, "The max speed the Jester moves while outside (5 is the speed its at while in the box and 18 is jesters max speed inside the facility)");
			
			if (ExtraValues == null)
			{
				ExtraValues = new GameObject();
				DontDestroyOnLoad(ExtraValues);
				ExtraValues.hideFlags = HideFlags.HideAndDontSave;
				ExtraValues.AddComponent<LETimerFunctions>();
			}

			mls = BepInEx.Logging.Logger.CreateLogSource("GameMaster");
			// Plugin startup logic
			mls.LogInfo($"Loaded {modGUID}. Patching.");
			this._harmony.PatchAll(typeof(Plugin));



	}

		//------------ CRAWLER THUMPER AI----------------
		//------------ CRAWLER THUMPER AI----------------


		//--- CRAWLER THUMPER LEAVE CODE---
		[HarmonyPatch(typeof(CrawlerAI), "Update")]
		[HarmonyPrefix]
		static void CrawlerLEPrefixAI(CrawlerAI __instance)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			if (CanThumperEscape.Value == true && RoundManager.Instance.NetworkManager.IsHost)
			{
				//--- CHECK TO MAKE SURE WE ARE INSIDE BEFORE CALCULATING ESCAPE CODE---
				if (!__instance.isOutside)
				{
					//--- IS AI STILL ATTACKING---
					if (__instance.currentBehaviourStateIndex != 0)
					{

						//--- TIME SINCE TARGET LOST---
						if (__instance.noticePlayerTimer < ThumperEscapeDelay.Value)
						{

							if (__instance.targetPlayer != null && !__instance.targetPlayer.isInsideFactory)
							{
								SendEnemyOutside(__instance, true);
							}

						}

					}
				}
				else // If AI is outside then
				{ // Force set ownership to be host if its not set to host (thanks _nips)
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}
				}
			}
		}


		//Thumper outside host override attack patch
		[HarmonyPatch(typeof(CrawlerAI), "OnCollideWithPlayer")]
		[HarmonyPostfix]
		public static void ThumperAILEOutsideAttack(CrawlerAI __instance, ref Collider other)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			// Check if outside and player calling this is host
			if (__instance.isOutside && RoundManager.Instance.NetworkManager.IsHost)
			{
				PlayerControllerB target = other.gameObject.GetComponent<PlayerControllerB>();
				// Check if target is valid, not dead, is currently being controlled by a player, and the target is not the host (because then the override is not needed)
				if (target != null && !target.isPlayerDead && target.isPlayerControlled && target != GameNetworkManager.Instance.localPlayerController)
				{

					if (__instance.timeSinceHittingPlayer <= 0.65)
					{
						return;
					}
					if (__instance.stunNormalizedTimer > 0f)
					{
						return;
					}

					__instance.timeSinceHittingPlayer = 0f;
					__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					target.DamagePlayer(40, true, true, CauseOfDeath.Mauling, 0, false, default(Vector3));
					target.DamagePlayerFromOtherClientServerRpc(40, Vector3.zero, -1);

					
					__instance.agent.speed = 0f;
					__instance.HitPlayerServerRpc((int)target.playerClientId);
					target.JumpToFearLevel(1f, true);

				}
			}
		}


		//--- Chance to Spawn Outside---
		[HarmonyPatch(typeof(CrawlerAI), "Start")]
		[HarmonyPostfix]
		static void CrawlerAILEPostfixStart(CrawlerAI __instance)
		{
			if (RoundManager.Instance.NetworkManager.IsHost && ThumperChanceToSpawnOutside.Value != 0 && UnityEngine.Random.Range(1, 100) <= ThumperChanceToSpawnOutside.Value)
			{
				SendEnemyOutside(__instance, true);
			}
		}


		//------------ JESTER AI----------------
		//------------ JESTER AI----------------


		//--- Jester LEAVE CODE---
		[HarmonyPatch(typeof(JesterAI), "Update")]
		[HarmonyPrefix]
		static void JesterLEPrefixAI(JesterAI __instance)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			if (CanJesterEscape.Value == true && RoundManager.Instance.NetworkManager.IsHost)
			{ 
				if (!__instance.isOutside)
				{
					
					if (__instance.currentBehaviourStateIndex == 2 && __instance.targetPlayer != null && !__instance.targetPlayer.isInsideFactory )
					{
						JesterSpeedWindup = 0;
						SendEnemyOutside(__instance, false);
					}
				}
				else // If AI is outside then
				{ // Force set ownership to be host if its not set to host (thanks _nips)
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}
				}
			}
		}

		//--- Jester half fix code---
		[HarmonyPatch(typeof(JesterAI), "Update")]
		[HarmonyPostfix]
		static void JesterLEPostfixAI(JesterAI __instance)
		{
			if (CanJesterEscape.Value == true && RoundManager.Instance.NetworkManager.IsHost)
			{
				if (__instance.isOutside)
				{
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}

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
						__instance.agent.stoppingDistance = 0;
					}
					JesterSpeedWindup = Mathf.Clamp(JesterSpeedWindup + Time.deltaTime * JesterSpeedIncreasePerSecond.Value, 0f, MaxJesterOutsideSpeed.Value);
					__instance.agent.speed = JesterSpeedWindup;
				}

			}
		}


		//Jester outside host override attack patch
		[HarmonyPatch(typeof(JesterAI), "OnCollideWithPlayer")]
		[HarmonyPostfix]
		public static void JesterAILEOutsideAttack(JesterAI __instance, ref Collider other)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}
			if (RoundManager.Instance.NetworkManager.IsHost)
			{
				// Check if outside and player calling this is host
				if (__instance.isOutside)
				{
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}
					PlayerControllerB target = other.gameObject.GetComponent<PlayerControllerB>();
					// Check if target is valid, not dead, is currently being controlled by a player, and the target is not the host (because then the override is not needed)
					if (target != null && !target.isPlayerDead && target.isPlayerControlled && target != GameNetworkManager.Instance.localPlayerController)
					{
						if (!other.gameObject.GetComponent<PlayerControllerB>())
						{
							return;
						}


						if (__instance.currentBehaviourStateIndex != 2)
						{
							return;
						}

	
						__instance.KillPlayerServerRpc((int)target.playerClientId);

					}

				}
			}


		}


		//--- Chance to Spawn Outside---
		[HarmonyPatch(typeof(JesterAI), "Start")]
		[HarmonyPostfix]
		static void JesterAILEPostfixStart(JesterAI __instance)
		{
			if (RoundManager.Instance.NetworkManager.IsHost && JesterChanceToSpawnOutside.Value != 0 && UnityEngine.Random.Range(1, 100) <= JesterChanceToSpawnOutside.Value)
			{
				SendEnemyOutside(__instance, true);
			}
		}


		//------------ Bracken AI----------------
		//------------ Bracken AI----------------


		//--- Bracken LEAVE CODE---
		[HarmonyPatch(typeof(FlowermanAI), "Update")]
		[HarmonyPrefix]
		static void FlowermanAILEPrefixAI(FlowermanAI __instance)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			// Check if bracken escaping is enabled in config and if the player calling this code is the host
			if (CanBrackenEscape.Value == true && RoundManager.Instance.NetworkManager.IsHost )
			{
				// Increase 60 second timer to have chance to escape
				// Checking if the AI is not already outside
				if (!__instance.isOutside)
				{
					
					// Check if the target that the Bracken is chasing left the facility and is in stealth mode
					if (__instance.targetPlayer != null && __instance.targetPlayer.isInsideFactory == false && (__instance.currentBehaviourStateIndex == 1 || __instance.evadeStealthTimer > 0))
					{
						// Set delay for bracken to teleport
						if (Time.time - TimeStartTeleport >= BrackenEscapeDelay.Value + 5)
						{
							TimeStartTeleport = Time.time;
						}
					}
					// wait till delay is over to teleport
					if (Time.time - TimeStartTeleport > BrackenEscapeDelay.Value && Time.time - TimeStartTeleport < BrackenEscapeDelay.Value+5)
					{
						SendEnemyOutside(__instance, true);
					}
					else
					{
						// check if the random escape timer has hit 60 seconds
						if (LETimerFunctions.MinuteEscapeTimerBracken >= 60 && __instance.targetPlayer == null)
						{
							// If so then reset it and calculate the chance to see if it should escape
							LETimerFunctions.MinuteEscapeTimerBracken = 0;
							if (BrackenChanceToEscapeEveryMinute.Value  != 0 && UnityEngine.Random.Range(1, 100) <= BrackenChanceToEscapeEveryMinute.Value)
							{
								SendEnemyOutside(__instance, false);
							}
						}
					}
				}
				else // If AI is outside then
				{ // Force set ownership to be host if its not set to host (thanks _nips)
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}
				}
			}
		}

		//------Bracken Override ownership post fix
		//--- Bracken LEAVE CODE---
		[HarmonyPatch(typeof(FlowermanAI), "Update")]
		[HarmonyPostfix]
		static void FlowermanAILEPostfixAI(FlowermanAI __instance)
		{
			if (__instance.isEnemyDead)
			{
				return;
			}

			// Check if bracken escaping is enabled in config and if the player calling this code is the host
			if (CanBrackenEscape.Value == true && RoundManager.Instance.NetworkManager.IsHost)
			{
				if (__instance.isOutside == true)
				{
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}
				}
			}
		}



		//Bracken outside host override attack patch
		[HarmonyPatch(typeof(FlowermanAI), "OnCollideWithPlayer")]
		[HarmonyPostfix]
		public static void FlowerManAILEOutsideAttack(FlowermanAI __instance, ref Collider other, bool ___startingKillAnimationLocalClient)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			// Check if outside and player calling this is host
			if (__instance.isOutside && RoundManager.Instance.NetworkManager.IsHost)
			{
				PlayerControllerB target = other.gameObject.GetComponent<PlayerControllerB>();
				// Check if target is valid, not dead, is currently being controlled by a player, and the target is not the host (because then the override is not needed)
				if (target != null && !target.isPlayerDead && target.isPlayerControlled && target != GameNetworkManager.Instance.localPlayerController)
				{
					// Check if ai is not currently killing someone else
					if (!__instance.inKillAnimation && !___startingKillAnimationLocalClient)
					{
						__instance.KillPlayerAnimationServerRpc((int)target.playerClientId);
						___startingKillAnimationLocalClient = true;
					}


				}
			}
		}


		//--- Reset Bracken Escape Timer And chance to Spawn Outside---
		[HarmonyPatch(typeof(FlowermanAI), "Start")]
		[HarmonyPostfix]
		static void FlowermanAILEPostfixStart(FlowermanAI __instance)
		{
			TimeStartTeleport = 0f;
			if (RoundManager.Instance.NetworkManager.IsHost && BrackenChanceToSpawnOutside.Value != 0 && UnityEngine.Random.Range(1, 100) <= BrackenChanceToSpawnOutside.Value)
			{
				SendEnemyOutside(__instance, true);
			}
		}


		//------------ Hoarder Bug AI----------------
		//------------ Hoarder Bug AI----------------


		//--- HOARDING BUG LEAVE CODE---
		[HarmonyPatch(typeof(HoarderBugAI), "Update")]
		[HarmonyPrefix]
		static void HoardingBugAILEPrefixAI(HoarderBugAI __instance)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			if (CanHoardingBugEscape.Value == true && RoundManager.Instance.NetworkManager.IsHost)
			{
				if (!__instance.isOutside)
				{


					if (__instance.targetPlayer != null && __instance.targetPlayer.isInsideFactory == false && __instance.searchForPlayer.inProgress == true)
					{
						SendEnemyOutside(__instance, true);
					}
					else
					{
						
						if (LETimerFunctions.MinuteEscapeTimerHoardingBug >= 60 && __instance.targetPlayer == null)
						{
							LETimerFunctions.MinuteEscapeTimerHoardingBug = 0;
							if (UnityEngine.Random.Range(1, 100) <= HoardingBugChanceToEscapeEveryMinute.Value / FindObjectsOfType(typeof(HoarderBugAI)).Length)
							{
								SendEnemyOutside(__instance, false);
								if (HoardingBugChanceToNestNearShip.Value != 0 && UnityEngine.Random.Range(1, 100) <= HoardingBugChanceToNestNearShip.Value)
								{
									StartOfRound GetElevatorPosFromGameLogic = (StartOfRound)FindObjectOfType(typeof(StartOfRound));

									Transform NodeClosestToShip = __instance.ChooseClosestNodeToPosition(GetElevatorPosFromGameLogic.elevatorTransform.position, false, 0);
									__instance.nestPosition = NodeClosestToShip.position;
								}
							}
						}
					}
				}
				else
				{ // Force set ownership to be host (thanks _nips)
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}
				}
			}
		}


		// Hoarding bug outside host override attack patch
		[HarmonyPatch(typeof(HoarderBugAI), "OnCollideWithPlayer")]
		[HarmonyPostfix]
		public static void HoardingBugAILEOutsideAttack(HoarderBugAI __instance, ref Collider other)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			if (__instance.isOutside && RoundManager.Instance.NetworkManager.IsHost)
			{
				PlayerControllerB target = other.gameObject.GetComponent<PlayerControllerB>();
				
				if (target != null && !target.isPlayerDead && target.isPlayerControlled && target != GameNetworkManager.Instance.localPlayerController)
				{

					if (__instance.timeSinceHittingPlayer <= 0.5)
					{
						return;
					}
					if (__instance.stunNormalizedTimer > 0f)
					{
						return;
					}
					if (!__instance.inChase)
					{
						return;
					}
					__instance.timeSinceHittingPlayer = 0;

					target.DamagePlayer(30, true, true, CauseOfDeath.Mauling, 0, false, default(Vector3));
					target.DamagePlayerFromOtherClientServerRpc(30, Vector3.zero, -1);
					__instance.HitPlayerServerRpc();

				}
			}
		}


		//--- Chance to Spawn Outside---
		[HarmonyPatch(typeof(HoarderBugAI), "Start")]
		[HarmonyPostfix]
		static void HoardingBugAILEPostfixStart(HoarderBugAI __instance)
		{
			if (RoundManager.Instance.NetworkManager.IsHost && HoardingBugChanceToSpawnOutside.Value != 0 && UnityEngine.Random.Range(1, 100) <= HoardingBugChanceToSpawnOutside.Value)
			{
				SendEnemyOutside(__instance, true);
			}
		}


		//------------ Coil Head AI----------------
		//------------ Coil Head AI----------------


		//--- Coil Head LEAVE CODE---
		[HarmonyPatch(typeof(SpringManAI), "Update")]
		[HarmonyPrefix]
		static void CoilHeadAILEPrefixAI(SpringManAI __instance)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			if (CanCoilHeadEscape.Value == true && RoundManager.Instance.NetworkManager.IsHost)
			{
				if (!__instance.isOutside)
				{
					if (__instance.targetPlayer != null && !__instance.targetPlayer.isInsideFactory)
					{
						SendEnemyOutside(__instance, false);
					}
				}
				else
				{ // Force set ownership to be host (thanks _nips)
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}
				}
			}
		}
		// Coil Head outside damage code
		[HarmonyPatch(typeof(SpringManAI), "OnCollideWithPlayer")]
		[HarmonyPostfix]
		public static void CoilHeadAILEOutsideAttack(SpringManAI __instance, ref Collider other)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			if (__instance.isOutside && RoundManager.Instance.NetworkManager.IsHost)
			{
				PlayerControllerB target = other.gameObject.GetComponent<PlayerControllerB>();

				if (target != null && !target.isPlayerDead && target.isPlayerControlled && target != GameNetworkManager.Instance.localPlayerController)
				{

					if (__instance.stoppingMovement)
					{
						return;
					}
					if (__instance.currentBehaviourStateIndex != 1)
					{
						return;
					}
					if (__instance.timeSinceHittingPlayer >= 0f)
					{
						return;
					}
					if (__instance.stunNormalizedTimer > 0f)
					{
						return;
					}

					__instance.timeSinceHittingPlayer = .2f;

					target.DamagePlayer(90, true, true, CauseOfDeath.Mauling, 2, false, default(Vector3));
					target.DamagePlayerFromOtherClientServerRpc(90, Vector3.zero, -1);
					target.JumpToFearLevel(1f, true);

				}
			}
		}


		//------------ Spider AI----------------
		//------------ Spider AI----------------

		//--- Spider LEAVE CODE---
		[HarmonyPatch(typeof(SandSpiderAI), "Update")]
		[HarmonyPrefix]
		static void SpiderAILEPrefixAI(SandSpiderAI __instance)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			if (CanSpiderEscape.Value == true && RoundManager.Instance.NetworkManager.IsHost)
			{
				if (!__instance.isOutside)
				{
					if (__instance.targetPlayer != null && !__instance.targetPlayer.isInsideFactory)
					{
						SendEnemyOutside(__instance, false);
						__instance.meshContainerPosition = __instance.serverPosition;
						__instance.meshContainerTarget = __instance.serverPosition;
						__instance.maxWebTrapsToPlace += UnityEngine.Random.Range(SpiderMinWebsOutside.Value, SpiderMaxWebsOutside.Value);
					}
				}
				else
				{ // Force set ownership to be host (thanks _nips)
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}
				}
			}
		}
		// Spider outside damage code
		[HarmonyPatch(typeof(SandSpiderAI), "OnCollideWithPlayer")]
		[HarmonyPostfix]
		public static void SpiderAILEOutsideAttack(SandSpiderAI __instance, ref Collider other)
		{
			if (__instance.isEnemyDead)
			{
				return;
			}

			if (__instance.isOutside && RoundManager.Instance.NetworkManager.IsHost)
			{
				PlayerControllerB target = other.gameObject.GetComponent<PlayerControllerB>();

				if (target != null && !target.isPlayerDead && target.isPlayerControlled && target != GameNetworkManager.Instance.localPlayerController)
				{


					if (__instance.onWall)
					{
						return;
					}

					if (__instance.timeSinceHittingPlayer <= 1f)
					{
						return;
					}
					if (__instance.stunNormalizedTimer > 0f)
					{
						return;
					}
					__instance.timeSinceHittingPlayer = 0f;
					target.DamagePlayer(90, true, true, CauseOfDeath.Mauling, 0, false, default(Vector3));
					target.DamagePlayerFromOtherClientServerRpc(90, Vector3.zero, -1);
					__instance.HitPlayerServerRpc((int)target.playerClientId);

				}
			}
		}


		//--- Chance to Spawn Outside---
		[HarmonyPatch(typeof(SandSpiderAI), "Start")]
		[HarmonyPostfix]
		static void SpiderAILEPostfixStart(SandSpiderAI __instance)
		{
			if (RoundManager.Instance.NetworkManager.IsHost && SpiderChanceToSpawnOutside.Value != 0 && UnityEngine.Random.Range(1, 100) <= SpiderChanceToSpawnOutside.Value)
			{
				SendEnemyOutside(__instance, true);
				__instance.meshContainerPosition = __instance.serverPosition;
				__instance.meshContainerTarget = __instance.serverPosition;
				__instance.maxWebTrapsToPlace = UnityEngine.Random.Range(SpiderMinWebsOutside.Value, SpiderMaxWebsOutside.Value);
			}
		}


		//------------ NutCracker AI----------------
		//------------ NutCracker AI----------------

		//--- NutCracker LEAVE CODE---
		[HarmonyPatch(typeof(NutcrackerEnemyAI), "Update")]
		[HarmonyPrefix]
		static void NutCrackerAILEPrefixAI(NutcrackerEnemyAI __instance)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			if (CanNutCrackerEscape.Value == true && RoundManager.Instance.NetworkManager.IsHost)
			{
				if (!__instance.isOutside)
				{
					if (__instance.targetPlayer != null && !__instance.targetPlayer.isInsideFactory)
					{
						SendEnemyOutside(__instance, true);
					}
				}
				else
				{ // Force set ownership to be host (thanks _nips)
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}
				}
			}
		}
		// NutCracker outside damage code
		[HarmonyPatch(typeof(NutcrackerEnemyAI), "OnCollideWithPlayer")]
		[HarmonyPostfix]
		public static void NutCrackerAILEOutsideAttack(NutcrackerEnemyAI __instance, ref Collider other)
		{
			if (__instance.isEnemyDead)
			{
				return;
			}

			if (__instance.isOutside && RoundManager.Instance.NetworkManager.IsHost)
			{
				PlayerControllerB target = other.gameObject.GetComponent<PlayerControllerB>();

				if (target != null && !target.isPlayerDead && target.isPlayerControlled && target != GameNetworkManager.Instance.localPlayerController)
				{

					if (__instance.stunNormalizedTimer >= 0f)
					{
						return;
					}
					if (__instance.timeSinceHittingPlayer < 1f)
					{
						return;
					}
					__instance.timeSinceHittingPlayer = 0;
					__instance.LegKickPlayerServerRpc((int)target.playerClientId);

				}
			}
		}


		//--- Chance to Spawn Outside---
		[HarmonyPatch(typeof(NutcrackerEnemyAI), "Start")]
		[HarmonyPostfix]
		static void NutCrackerAILEPostfixStart(NutcrackerEnemyAI __instance)
		{
			if (RoundManager.Instance.NetworkManager.IsHost && NutCrackerChanceToSpawnOutside.Value != 0 && UnityEngine.Random.Range(1, 100) <= NutCrackerChanceToSpawnOutside.Value)
			{
				SendEnemyOutside(__instance, true);
			}
		}


		//------------ Slime/Hygrodere AI----------------
		//------------ Slime/Hygrodere AI----------------

		//--- Slime/Hygrodere LEAVE CODE---
		[HarmonyPatch(typeof(BlobAI), "Update")]
		[HarmonyPrefix]
		static void BlobAILEPrefixAI(BlobAI __instance)
		{

			if (__instance.isEnemyDead)
			{
				return;
			}

			if (CanHygrodereEscape.Value == true && RoundManager.Instance.NetworkManager.IsHost)
			{
				if (!__instance.isOutside)
				{
					if (__instance.targetPlayer != null && !__instance.targetPlayer.isInsideFactory)
					{
						SendEnemyOutside(__instance, false);
						__instance.centerPoint = __instance.transform;
					}
				}
				else
				{ // Force set ownership to be host (thanks _nips)
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}
				}
			}
		}
		// Slime/Hygrodere outside damage code
		[HarmonyPatch(typeof(BlobAI), "OnCollideWithPlayer")]
		[HarmonyPostfix]
		public static void BlobAILEOutsideAttack(BlobAI __instance, ref Collider other)
		{
			if (__instance.isEnemyDead)
			{
				return;
			}

			if (__instance.isOutside && RoundManager.Instance.NetworkManager.IsHost)
			{
				PlayerControllerB target = other.gameObject.GetComponent<PlayerControllerB>();

				if (target != null && !target.isPlayerDead && target.isPlayerControlled && target != GameNetworkManager.Instance.localPlayerController)
				{
					if (__instance.timeSinceHittingLocalPlayer < 0.25f)
					{
						return;
					}
					if (__instance.tamedTimer > 0f && __instance.angeredTimer < 0f)
					{
						return;
					}
					__instance.timeSinceHittingLocalPlayer = 0f;

					target.DamagePlayerFromOtherClientServerRpc(35, Vector3.zero, -1);
					target.DamagePlayer(35, true, true, CauseOfDeath.Unknown, 0, false, default(Vector3));
					if (target.isPlayerDead)
					{
						__instance.SlimeKillPlayerEffectServerRpc((int)target.playerClientId);
					}

				}
			}
		}


		//--- Chance to Spawn Outside---
		[HarmonyPatch(typeof(BlobAI), "Start")]
		[HarmonyPostfix]
		static void HygrodereAILEPostfixStart(BlobAI __instance)
		{
			if (RoundManager.Instance.NetworkManager.IsHost && HygrodereChanceToSpawnOutside.Value != 0 && UnityEngine.Random.Range(1, 100) <= HygrodereChanceToSpawnOutside.Value)
			{
				SendEnemyOutside(__instance, true);
				__instance.centerPoint = __instance.transform;
			}
		}


		//------------ Puffer/Lizard AI----------------
		//------------ Puffer/Lizard AI----------------

		//--- Puffer/Lizard LEAVE CODE---
		[HarmonyPatch(typeof(PufferAI), "Update")]
		[HarmonyPrefix]
		static void PufferAILEPrefixAI(PufferAI __instance)
		{

			if (__instance.isEnemyDead)
			{
				return;
			} 

			if (CanPufferEscape.Value == true && RoundManager.Instance.NetworkManager.IsHost)
			{
				if (!__instance.isOutside)
				{
					if (__instance.targetPlayer != null && !__instance.targetPlayer.isInsideFactory)
					{
						SendEnemyOutside(__instance, false);
					}
					else
					{
						
						if (LETimerFunctions.MinuteEscapeTimerPuffer >= 60 && __instance.targetPlayer == null)
						{
							LETimerFunctions.MinuteEscapeTimerPuffer = 0;
							if (PufferChanceToEscapeEveryMinute.Value != 0 && UnityEngine.Random.Range(1, 100) <= PufferChanceToEscapeEveryMinute.Value)
							{
								SendEnemyOutside(__instance, false);
							}
						}
					}
				}
				else
				{ // Force set ownership to be host (thanks _nips)
					if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
					{
						__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
					}
				}
			}
		}
		// Puffer/Lizard outside damage code
		[HarmonyPatch(typeof(PufferAI), "OnCollideWithPlayer")]
		[HarmonyPostfix]
		public static void PufferAILEOutsideAttack(PufferAI __instance, ref Collider other)
		{
			if (__instance.isEnemyDead)
			{
				return;
			}

			if (__instance.isOutside && RoundManager.Instance.NetworkManager.IsHost)
			{
				PlayerControllerB target = other.gameObject.GetComponent<PlayerControllerB>();

				if (target != null && !target.isPlayerDead && target.isPlayerControlled && target != GameNetworkManager.Instance.localPlayerController)
				{
					if (__instance.timeSinceHittingPlayer <= 1f)
					{
						return;
					}
					__instance.timeSinceHittingPlayer = 0;
					target.DamagePlayer(20, true, true, CauseOfDeath.Mauling, 0, false, default(Vector3));
					target.DamagePlayerFromOtherClientServerRpc(20, Vector3.zero, -1);
					__instance.BitePlayerServerRpc((int)target.playerClientId);

				}
			}
		}


		//--- Chance to Spawn Outside---
		[HarmonyPatch(typeof(PufferAI), "Start")]
		[HarmonyPostfix]
		static void PufferAILEPostfixStart(PufferAI __instance)
		{
			if (RoundManager.Instance.NetworkManager.IsHost && PufferChanceToSpawnOutside.Value != 0 && UnityEngine.Random.Range(1, 100) <= PufferChanceToSpawnOutside.Value)
			{
				SendEnemyOutside(__instance, true);
			}
		}


		// Send AI Outside/Inside Code
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

			if (__instance.OwnerClientId != GameNetworkManager.Instance.localPlayerController.actualClientId)
			{
				__instance.ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
			}

			Transform ClosestNodePos = __instance.ChooseClosestNodeToPosition(__instance.serverPosition, false, 0);

			if (Vector3.Magnitude(ClosestNodePos.position - __instance.serverPosition) > 10 || SpawnOnDoor == false)
			{
				__instance.serverPosition = ClosestNodePos.position;
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
		public static ConfigEntry<bool> CanSpiderEscape;
		public static ConfigEntry<bool> CanNutCrackerEscape;
		public static ConfigEntry<bool> CanHygrodereEscape;
		public static ConfigEntry<bool> CanPufferEscape;

		public static float JesterSpeedWindup;


		public static ConfigEntry<float> BrackenChanceToEscapeEveryMinute;
		public static ConfigEntry<float> PufferChanceToEscapeEveryMinute;
		public static ConfigEntry<float> HoardingBugChanceToEscapeEveryMinute;

		public static ConfigEntry<float> HoardingBugChanceToNestNearShip;

		public static ConfigEntry<float> HoardingBugChanceToSpawnOutside;
		public static ConfigEntry<float> BrackenChanceToSpawnOutside;
		public static ConfigEntry<float> PufferChanceToSpawnOutside;
		public static ConfigEntry<float> JesterChanceToSpawnOutside;
		public static ConfigEntry<float> HygrodereChanceToSpawnOutside;
		public static ConfigEntry<float> NutCrackerChanceToSpawnOutside;
		public static ConfigEntry<float> ThumperChanceToSpawnOutside;
		public static ConfigEntry<float> SpiderChanceToSpawnOutside;

		public static ConfigEntry<float> JesterSpeedIncreasePerSecond;
		public static ConfigEntry<float> MaxJesterOutsideSpeed;

		public static ConfigEntry<float> BrackenEscapeDelay;
		public static ConfigEntry<float> ThumperEscapeDelay;

		public static ConfigEntry<int> SpiderMinWebsOutside;
		public static ConfigEntry<int> SpiderMaxWebsOutside;

	}


	

}



