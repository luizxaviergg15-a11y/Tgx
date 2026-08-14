using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.UIDefs;
using Modding;
using Satchel;
using SFCore;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LostArtifacts.Artifacts;

namespace LostArtifacts
{
	public class LostArtifacts : Mod, ILocalSettings<Settings>
	{
		public static LostArtifacts Instance;
		public static Dictionary<string, Dictionary<string, GameObject>> Preloads;

		private GameObject prefabUI;
		public string iconPath;

		public GameObject artifactsGO;
		public Artifact[] artifacts;
		public HashSet<string> artifactNames;
		public GameObject artifactsUI;
		public PlayMakerFSM pageFSM;

		public static Settings Settings { get; set; } = new Settings();

		public void OnLoadLocal(Settings s)
		{
			LogDebug("Loading Lost Artifacts settings");
			Settings = s;
		}
		public Settings OnSaveLocal()
		{
			LogDebug("Saving Lost Artifacts settings");
			return Settings;
		}


		public override string GetVersion() => AssemblyUtils.GetAssemblyVersionHash();

		public LostArtifacts() : base("LostArtifacts")
		{
			Instance = this;

			//Load UI prefab
			AssetBundle assetBundle = AssetBundle.LoadFromStream(
				typeof(LostArtifacts).Assembly.GetManifestResourceStream("LostArtifacts.Resources.artifactsui"));
			if(assetBundle == null)
			{
				Log("Failed to load AssetBundle!");
				return;
			}
			prefabUI = assetBundle.LoadAsset<GameObject>("Artifacts");
			if(prefabUI == null)
			{
				Log("Mobile port: Artifacts UI prefab could not be loaded from AssetBundle.");
				return;
			}

			//Create UI
			artifactsUI = UnityEngine.Object.Instantiate(prefabUI);
			GameObject.DontDestroyOnLoad(artifactsUI);
			artifactsUI.SetActive(false);

			//Add inventory page with SFCore
			InventoryHelper.AddInventoryPage(
				InventoryPageType.Empty,
				"LostArtifacts.Artifacts",
				"LostArtifacts.PageConvKey",
				"Artifacts",
				"artifactsUnlocked",
				EditInventory);

			//Add artifacts
			artifactsGO = new GameObject("LostArtifactsGO");
			artifacts = new Artifact[21];
			artifactNames = new HashSet<string>();
			UnityEngine.Object.DontDestroyOnLoad(artifactsGO);

			//Create icons directory if it doesn't exist
			iconPath = Path.Combine(AssemblyUtils.getCurrentDirectory(), "Icons");
			IoUtils.EnsureDirectory(iconPath);

			AddArtifact<TravelersGarment>();
			AddArtifact<PavingStone>();
			AddArtifact<LushMoss>();
			AddArtifact<NoxiousShroom>();
			AddArtifact<CryingStatue>();
			AddArtifact<TotemShard>();
			AddArtifact<DungBall>();
			AddArtifact<Tumbleweed>();
			AddArtifact<ChargedCrystal>();
			AddArtifact<Dreamwood>();
			AddArtifact<LumaflyEssence>();
			AddArtifact<ThornedLeaf>();
			AddArtifact<WeaverSilk>();
			AddArtifact<WyrmAsh>();
			AddArtifact<BeastShell>();
			AddArtifact<Honeydrop>();
			AddArtifact<InfectedRock>();
			AddArtifact<Buzzsaw>();
			AddArtifact<VoidEmblem>();
			AddArtifact<AttunedJewel>();
			AddArtifact<HiddenMemento>();
		}

		public override List<ValueTuple<string, string>> GetPreloadNames()
		{
			return new List<ValueTuple<string, string>>
			{
				new ValueTuple<string, string>("Mines_03", "Crystal Crawler"),
				new ValueTuple<string, string>("RestingGrounds_08", "Ghost revek"),
				new ValueTuple<string, string>("GG_Uumuu", "Mega Jellyfish GG"),
				new ValueTuple<string, string>("Hive_01", "Hive Bench")
			};
		}

		public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
		{
			Log("Initializing");

			Preloads = preloadedObjects;

			foreach(Artifact artifact in artifacts)
			{
				//Define items
				ItemChanger.Items.BoolItem item = new ItemChanger.Items.BoolItem()
				{
					name = artifact.InternalName(),
					fieldName = "unlockedArtifact_" + artifact.ID(),
					UIDef = new MsgUIDef()
					{
						name = new LanguageString("UI", "LostArtifacts." + artifact.InternalName()),
						shopDesc = new LanguageString("UI", "LostArtifacts." + artifact.InternalName() + "Desc"),
						sprite = artifact.sprite
					}
				};

				Finder.DefineCustomItem(item);
				Finder.DefineCustomLocation(artifact.Location());

				artifactNames.Add(artifact.InternalName());
			}

			On.HeroController.Start += HeroControllerStart;
			On.HeroController.OnDisable += HeroControllerOnDisable;
			On.UIManager.StartNewGame += UIManagerStartNewGame;
			ModHooks.LanguageGetHook += LanguageGetHook;
			ModHooks.GetPlayerBoolHook += GetPlayerBoolHook;
			ModHooks.SetPlayerBoolHook += SetPlayerBoolHook;

			Log("Initialized");
		}

		private void HeroControllerStart(On.HeroController.orig_Start orig, HeroController self)
		{
			orig(self);

			//Activate artifacts according to settings
			foreach(Artifact artifact in artifacts)
			{
				if(artifact == null || artifact.active) continue;

				//Activate artifact
				artifact.level = 0;
				if(Settings.slots[0] == artifact.ID()) artifact.level = Settings.overchargedSlot == 0 ? 2 : 1;
				if(Settings.slots[1] == artifact.ID()) artifact.level = Settings.overchargedSlot == 1 ? 3 : 2;
				if(Settings.slots[2] == artifact.ID()) artifact.level = Settings.overchargedSlot == 2 ? 3 : 2;
				if(Settings.slots[3] == artifact.ID()) artifact.level = Settings.overchargedSlot == 3 ? 4 : 3;

				if(artifact.level > 0)
				{
					artifact.Activate();
				}
			}
		}

		private void HeroControllerOnDisable(On.HeroController.orig_OnDisable orig, HeroController self)
		{
			//Deactivate artifacts
			foreach(Artifact artifact in artifacts)
			{
				if(artifact == null || !artifact.active) continue;

				artifact.Deactivate();
			}

			//Reset UI so it doesn't transfer across saves
			UnityEngine.Object.Destroy(artifactsUI);
			artifactsUI = UnityEngine.Object.Instantiate(prefabUI);
			GameObject.DontDestroyOnLoad(artifactsUI);
			artifactsUI.SetActive(false);

			orig(self);
		}

		private void EditInventory(GameObject page)
		{
			//Cache FSM
			pageFSM = page.LocateMyFSM("Empty UI");

			// Mobile ports can have slightly different inventory FSMs.
			// Only patch the inventory when the states used by Lost Artifacts exist.
			if(pageFSM == null || pageFSM.Fsm == null)
			{
				Log("Mobile port: inventory FSM not found; artifact UI disabled.");
				return;
			}

			pageFSM.AddState("Artifacts");

			try
			{
				pageFSM.ChangeTransition("Init Heart Piece", "FINISHED", "Artifacts");
				pageFSM.AddTransition("Artifacts", "LEFT", "Move Pane L");
				pageFSM.AddTransition("Artifacts", "RIGHT", "Move Pane R");
				pageFSM.AddCustomAction("Artifacts", () => artifactsUI.SetActive(true));
				pageFSM.InsertCustomAction("Move Pane L", () => artifactsUI.SetActive(false), 0);
				pageFSM.InsertCustomAction("Move Pane R", () => artifactsUI.SetActive(false), 0);
				On.HutongGames.PlayMaker.Actions.SendEventByName.OnEnter += CloseInventoryHook;
			}
			catch(Exception e)
			{
				Log("Mobile port: inventory FSM layout differs; artifact UI could not be attached: " + e.Message);
			}
		}

		private void CloseInventoryHook(On.HutongGames.PlayMaker.Actions.SendEventByName.orig_OnEnter orig, SendEventByName self)
		{
			orig(self);

			if(self.Fsm.Name != "Inventory Control") return;

			if(self.State.Name == "Loop Through" && self.sendEvent.Value == "INV PANEL CHANGE")
			{
				artifactsUI.SetActive(false);
			}
			if((self.State.Name == "Close" || self.State.Name == "Damage Close" || self.State.Name == "R Lock Close") &&
				self.sendEvent.Value == "MAP KEY DOWN")
			{
				artifactsUI.SetActive(false);
				pageFSM.SetState("Init");
			}
		}

		public void AddArtifact<T>() where T : Artifact
		{
			Artifact artifact = artifactsGO.AddComponent<T>();
			artifact.sprite = new ArtifactSprite(artifact.InternalName());
			artifacts[artifact.ID()] = artifact;
		}

		private void UIManagerStartNewGame(On.UIManager.orig_StartNewGame orig, UIManager self, bool permaDeath, bool bossRush)
		{
			orig(self, permaDeath, bossRush);

			if(bossRush)
			{
				for(int i = 0; i < Settings.unlocked.Length; i++)
				{
					Settings.unlocked[i] = true;
				}
				return;
			}
			PlaceArtifacts();
		}

		private void PlaceArtifacts()
		{
			ItemChangerMod.CreateSettingsProfile(false, false);

			List<AbstractPlacement> placements = new List<AbstractPlacement>();
			foreach(Artifact artifact in artifacts)
			{
				placements.Add(Finder.GetLocation(artifact.InternalName()).Wrap().Add(Finder.GetItem(artifact.InternalName())));
			}
			ItemChangerMod.AddPlacements(placements, PlacementConflictResolution.Ignore);
		}

		private string LanguageGetHook(string key, string sheetTitle, string orig)
		{
			if(key.StartsWith("LostArtifacts."))
			{
				string str = key.Split(new char[] { '.' })[1];
				if(str == "PageConvKey") return "Artifacts";
				foreach(Artifact artifact in artifacts)
				{
					if(str == artifact.InternalName())
					{
						return artifact.Name();
					}
					if(str == artifact.InternalName() + "Desc")
					{
						return artifact.LoreDescription();
					}
				}
			}
			return orig;
		}

		private bool GetPlayerBoolHook(string name, bool orig)
		{
			if(name == "artifactsUnlocked")
			{
				foreach(bool unlocked in Settings.unlocked)
				{
					if(unlocked) return true;
				}
				return false;
			}
			if(name.StartsWith("unlockedArtifact_"))
			{
				return Settings.unlocked[int.Parse(name.Split(new char[] { '_' })[1])];
			}
			return orig;
		}

		private bool SetPlayerBoolHook(string name, bool orig)
		{
			if(name.StartsWith("unlockedArtifact_"))
			{
				Settings.unlocked[int.Parse(name.Split(new char[] { '_' })[1])] = orig;
			}
			return orig;
		}

	}
}