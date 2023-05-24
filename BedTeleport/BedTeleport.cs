using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UWE;
//using SMLHelper.V2.Handlers;
//using BedTeleport.Configuration;
using System;

namespace BedTeleport
{
	//[BepInDependency("SMLHelper", BepInDependency.DependencyFlags.HardDependency)]
	[BepInPlugin("com.blackFox.BedTeleport", "Bed Teleport", "0.1")]
	[BepInProcess("Subnautica.exe")]
	public class BedTeleport : BaseUnityPlugin
	{
		//internal static SMLConfig SMLConfig { get; private set; }

		internal static ManualLogSource logger { get; private set; }

		public static ConfigEntry<bool> modEnabled { get; private set; }
		public static ConfigEntry<bool> isDebug { get; private set; }
		public static ConfigEntry<bool> playSound { get; private set; }
		public static ConfigEntry<bool> immediateTeleport { get; private set; }
		public static ConfigEntry<KeyCode> modHotKey { get; private set; }
		public static ConfigEntry<string> handText { get; private set; }
		public static ConfigEntry<string> menuHeader { get; private set; }

		private void Awake()
		{
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");
            immediateTeleport = Config.Bind("General", "ImmediateTeleport", false, "Immediately teleport to location");
            playSound = Config.Bind("Options", "PlaySound", true, "Play teleport sound");

            modHotKey = Config.Bind("Options", "HotKeyMod", KeyCode.LeftShift, "Key to hold to allow teleportation.");
            handText = Config.Bind("Text", "HandText", "Teleport", "Hover message.");
			menuHeader = Config.Bind("Text", "MenuHeader", "Teleport Menu", "Menu header.");

			logger = Logger;

			//BedTeleport.SMLConfig = OptionsPanelHandler.RegisterModOptions<SMLConfig>();
			//IngameMenuHandler.RegisterOnSaveEvent(new Action(BedTeleport.SMLConfig.Save));

			Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);

            Dbgl("Plugin awake", LogLevel.Debug);
		}
		
		public static void Dbgl(string str = "", LogLevel logLevel = LogLevel.Message)
		{
			if (isDebug.Value)
			{
                logger.Log(logLevel, str);
			}
		}

		private static void ShowTeleportMenuV2(Bed source)
		{
			Dbgl($"ShowTeleportMenu({source.name})");

			IngameMenu inGameMenu = IngameMenu.main;
			IngameMenuTopLevel ingameMenuTopLevel = inGameMenu ? inGameMenu.GetComponentInChildren<IngameMenuTopLevel>() : null;

			if (!ingameMenuTopLevel)
			{
				ErrorMessage.AddWarning("Menu template not found!");
			}
			else
			{
				Dbgl("Searching for avilable beds...");
				//get beds in game
				List<Bed> bedsList = BedTeleportHelper.GetAllBeds();
				Dbgl(string.Format("Found {0} beds", bedsList.Count));

				if (bedsList.Count < 2)
				{
					ErrorMessage.AddWarning("No beds found!");
					return;
				}

				Dbgl("Creating menu...");

				GameObject menu = Instantiate(ingameMenuTopLevel.gameObject, uGUI.main.hud.transform);
				menu.name = menuHeader.Value;
				//menu.transform.GetComponentInChildren<IngameMenu>()?.Open();
				//Dbgl("IngameMenu.open()");
				//var IngameMenuTopLevel = menu.transform.GetComponentInChildren<IngameMenuTopLevel>();
				//if (IngameMenuTopLevel) {
				//	Dbgl("Destroyed IngameMenuTopLevel");
				//	DestroyImmediate(IngameMenuTopLevel);
				//}

				TextMeshProUGUI Header_Text = menu.transform.Find("Header").GetComponent<TextMeshProUGUI>();
				Header_Text.text = menuHeader.Value;

				BedTeleportMenu teleportMenu = menu.AddComponent<BedTeleportMenu>();
				teleportMenu.Select(false);

				Dbgl("Menu Created.");

				var menuGameObject = menu.gameObject;

				List<Button> activeMenuButtons = BedTeleportHelper.GetButtonsFromMenuGameObject(menuGameObject);
				if (activeMenuButtons.Count == 0)
				{
					ErrorMessage.AddWarning("No Buttons Found In Menu template!");
					return;
				}

				Button buttonPrefab = activeMenuButtons[0];
				buttonPrefab.gameObject.SetActive(true);

				var beacons = BedTeleportHelper.GetBeacons();

				foreach (Bed bed in bedsList)
				{
					if (source != bed)
					{
						var bedName = GetBedText(source.transform.position, bed.transform.position);
						foreach (var beacon in beacons)
						{
							var dist = Vector3.Distance(bed.gameObject.transform.position, beacon.gameObject.transform.position);
							if (dist < 50)
							{
								bedName = $"{beacon.GetLabel()} {bedName}";
								break;
							}
						}
						Button bedButton = Instantiate(buttonPrefab.gameObject, buttonPrefab.transform.parent).GetComponent<Button>();
						TextMeshProUGUI button_Text = bedButton.GetComponentInChildren<TextMeshProUGUI>();
						button_Text.text = bedName;

						bedButton.gameObject.name = bedName;

						bedButton.onClick = new Button.ButtonClickedEvent();
						bedButton.onClick.AddListener(() =>
						{
							Dbgl($"{bedName} Button: Clicked_Start");
							Destroy(menuGameObject);
							BedTeleportCoroutineHelper coroutineHelper = Instantiate(new GameObject(), null).AddComponent<BedTeleportCoroutineHelper>();
							coroutineHelper.GoToLocation(source, bed, immediateTeleport.Value);
							Dbgl($"{bedName} Button: Clicked_End");
						});

						Dbgl($"Setuped button({bedName})");
					}
				}

				//setup close button
				Button closeMenuButton = Instantiate(buttonPrefab.gameObject, buttonPrefab.transform.parent).GetComponent<Button>();
				var closeMenuButtonName = "Close";

				TextMeshProUGUI componentInChildren = closeMenuButton.gameObject.transform.GetComponentInChildren<TextMeshProUGUI>();
				componentInChildren.text = closeMenuButtonName;

				closeMenuButton.gameObject.name = closeMenuButtonName;
				closeMenuButton.onClick = new Button.ButtonClickedEvent();
				closeMenuButton.onClick.AddListener(() =>
				{
					Dbgl($"{closeMenuButtonName}: Clicked");
					Destroy(menuGameObject);
					Dbgl($"{closeMenuButtonName} Button: Clicked_End");
				});

				Dbgl($"Setuped button({closeMenuButtonName})");

				Destroy(buttonPrefab.gameObject);
			}
		}

		private static string GetBedText(Vector3 source, Vector3 dest)
		{
			int dist_int = Mathf.RoundToInt(Vector3.Distance(dest, source));

			Vector3 vector = dest - source;
			float latitude = Mathf.Abs(vector.x);
			float longitude = Mathf.Abs(vector.z);
			string compassDir = "N";
			if (vector.x < 0f)
			{
				if (vector.z < 0f)
				{
					if (latitude > longitude * 4f)
					{
						compassDir = "W";
					}
					else
					{
						if (longitude > latitude * 4f)
						{
							compassDir = "S";
						}
						else
						{
							if (latitude > longitude * 2f)
							{
								compassDir = "WSW";
							}
							else
							{
								if (longitude > latitude * 2f)
								{
									compassDir = "SSW";
								}
								else
								{
									compassDir = "SW";
								}
							}
						}
					}
				}
				else
				{
					if (latitude > longitude * 4f)
					{
						compassDir = "W";
					}
					else
					{
						if (longitude > latitude * 4f)
						{
							compassDir = "N";
						}
						else
						{
							if (latitude > longitude * 2f)
							{
								compassDir = "WNW";
							}
							else
							{
								if (longitude > latitude * 2f)
								{
									compassDir = "NNW";
								}
								else
								{
									compassDir = "NW";
								}
							}
						}
					}
				}
			}
			else
			{
				if (vector.z < 0f)
				{
					if (latitude > longitude * 4f)
					{
						compassDir = "E";
					}
					else
					{
						if (longitude > latitude * 4f)
						{
							compassDir = "S";
						}
						else
						{
							if (latitude > longitude * 2f)
							{
								compassDir = "ESE";
							}
							else
							{
								if (longitude > latitude * 2f)
								{
									compassDir = "SSE";
								}
								else
								{
									compassDir = "SE";
								}
							}
						}
					}
				}
				else
				{
					if (latitude > longitude * 4f)
					{
						compassDir = "E";
					}
					else
					{
						if (longitude > latitude * 4f)
						{
							compassDir = "N";
						}
						else
						{
							if (latitude > longitude * 2f)
							{
								compassDir = "ENE";
							}
							else
							{
								if (longitude > latitude * 2f)
								{
									compassDir = "NNE";
								}
								else
								{
									compassDir = "NE";
								}
							}
						}
					}
				}
			}

			int height = Mathf.Abs(Mathf.RoundToInt(vector.y));
			string height_string = (height > 0) ? (string.Format(" ({0}m ", height) + ((vector.y > 0f) ? "up" : "down") + ")") : "";
			return string.Format("Bed {0}m {1}{2}", dist_int, compassDir, height_string);
		}


		[HarmonyPatch(typeof(Bed), "OnHandClick")]
		private static class Bed_OnHandClick_Patch
		{
			private static bool Prefix(Bed __instance, GUIHand hand)
			{
				bool flag = !modEnabled.Value || !Input.GetKey(modHotKey.Value);
				bool result;
				if (flag)
				{
					result = true;
				}
				else
				{
                    ShowTeleportMenuV2(__instance);
					result = false;
				}
				return result;
			}

		}

		[HarmonyPatch(typeof(Bed), "OnHandHover")]
		private static class Bed_OnHandHover_Patch
		{
			private static bool Prefix(Bed __instance, GUIHand hand)
			{
				bool flag = !modEnabled.Value || !Input.GetKey(modHotKey.Value);
				bool result;
				if (flag)
				{
					result = true;
				}
				else
				{
					if (hand.IsFreeToInteract())
					{
						HandReticle.main.SetText(0, handText.Value, true, GameInput.Button.LeftHand);
						HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, false, GameInput.Button.None);
						HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
					}
					result = false;
				}
				return result;
			}
		}
	}
}
