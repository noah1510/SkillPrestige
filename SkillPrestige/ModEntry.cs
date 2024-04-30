using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkillPrestige.Framework;
using SkillPrestige.Framework.Commands;
using SkillPrestige.Framework.InputHandling;
using SkillPrestige.Framework.Menus;
using SkillPrestige.Framework.Menus.Elements.Buttons;
using SkillPrestige.Logging;
using SkillPrestige.Mods;
using SkillPrestige.Professions;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace SkillPrestige
{
    /// <summary>The mod entry class.</summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    internal class ModEntry : Mod
    {
        private bool SaveIsLoaded { get; set; }

        public static string ModPath { get; private set; }

        public static ModConfig Config { get; private set; }

        public static IMonitor LogMonitor { get; private set; }

        public static string CurrentSaveOptionsPath => Path.Combine(ModPath, "psconfigs/", Constants.SaveFolderName!);

        public static string PerSaveOptionsDirectory { get; private set; }

        public static Texture2D PrestigeIconTexture { get; private set; }

        public static Texture2D CheckmarkTexture { get; private set; }

        public static IModRegistry ModRegistry { get; private set; }

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // init
            LogMonitor = this.Monitor;
            ModPath = helper.DirectoryPath;
            ModRegistry = helper.ModRegistry;
            PerSaveOptionsDirectory = Path.Combine(ModPath, "psconfigs/");
            Config = helper.ReadConfig<ModConfig>();

            // disable mod if All Professions is installed
            if (this.Helper.ModRegistry.IsLoaded("community.AllProfessions"))
            {
                Logger.LogCriticalWarning("Conflict Detected. This mod cannot work with AllProfessions. Skill Prestige disabled.");
                Logger.LogDisplay("Skill Prestige Mod: If you wish to use this mod in place of AllProfessions, remove the AllProfessions mod and run the player_resetallprofessions command.");
                return;
            }

            // hook events
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Input.CursorMoved += this.OnCursorMoved;
            helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            bool isClick = e.Button.IsOneOf(SButton.MouseLeft, SButton.ControllerA);

            if (Game1.activeClickableMenu is null
                && this.SaveIsLoaded
                && isClick)
            {
                var tileToCheck = e.Button switch
                {
                    SButton.MouseLeft => Game1.currentCursorTile,
                    SButton.ControllerA => Game1.player.GetGrabTile(),
                    _ => Vector2.Zero
                };
                if(Game1.currentLocation.objects.ContainsKey(tileToCheck)
                    && Game1.currentLocation.objects[tileToCheck].name == "Prestige Statue")
                    this.OpenSelectionMenu();
            }

            // ReSharper disable once InvertIf
            if (Game1.activeClickableMenu is not null && Game1.activeClickableMenu is IInputHandler handler)
                    handler.OnButtonPressed(e, isClick);
        }

        private void OpenSelectionMenu(bool playSound = true)
        {
            Logger.LogVerbose("Opening Selection Menu...");
            const int menuWidth = Game1.tileSize * 10;
            const int menuHeight = Game1.tileSize * 10;

            int menuXCenter = (menuWidth + IClickableMenu.borderWidth * 2) / 2;
            int menuYCenter = (menuHeight + IClickableMenu.borderWidth * 2) / 2;
            int screenXCenter = Game1.uiViewport.Width / 2;
            int screenYCenter = Game1.uiViewport.Height / 2;
            var bounds = new Rectangle(screenXCenter - menuXCenter, screenYCenter - menuYCenter, menuWidth + IClickableMenu.borderWidth * 2, menuHeight + IClickableMenu.borderWidth * 2);
            if(playSound) Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new SelectionMenu(bounds);
        }

        /// <summary>Raised after the player moves the in-game cursor.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnCursorMoved(object sender, CursorMovedEventArgs e)
        {
            if (Game1.activeClickableMenu is IInputHandler handler)
                handler.OnCursorMoved(e);
        }

        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            PrestigeSaveData.Instance.UpdateCurrentSaveFileInformation();
            PerSaveOptions.Instance.Check();
            Profession.AddMissingProfessions();
            this.SaveIsLoaded = true;
        }

        /// <summary>Raised after the game returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            PrestigeSaveData.Instance.Read();
            this.SaveIsLoaded = false;
            Logger.LogInformation("Return To Title.");
            PerSaveOptions.ClearLoadedPerSaveOptionsFile();
            ExperienceHandler.ResetExperience();
        }

        private bool SelectionMenuPendingReopen;
        private bool PrestigeMenuPendingReopen;
        /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (this.SelectionMenuPendingReopen && Game1.activeClickableMenu is SelectionMenu)
            {
                this.OpenSelectionMenu(false);
                this.SelectionMenuPendingReopen = false;
            }

            if (this.PrestigeMenuPendingReopen && Game1.activeClickableMenu is PrestigeMenu)
            {
                var currentPrestigeMenu = (PrestigeMenu)Game1.activeClickableMenu;
                var currentPrestigeMenuBounds = new Rectangle(currentPrestigeMenu.xPositionOnScreen, currentPrestigeMenu.yPositionOnScreen, currentPrestigeMenu.width, currentPrestigeMenu.height);
                var prestige = PrestigeSaveData.CurrentlyLoadedPrestigeSet.Prestiges.Single(x => x.SkillType == currentPrestigeMenu.Skill.Type);
                Game1.activeClickableMenu = new PrestigeMenu(currentPrestigeMenuBounds, currentPrestigeMenu.Skill, prestige);
                this.PrestigeMenuPendingReopen = false;
            }
            switch (Game1.nextClickableMenu.FirstOrDefault())
            {
                case SelectionMenu:
                    this.SelectionMenuPendingReopen = true;
                    break;
                case PrestigeMenu:
                    this.PrestigeMenuPendingReopen = true;
                    break;
            }
        }

        private void LoadSprites()
        {
            Logger.LogInformation("Loading sprites...");
            Button.DefaultButtonTexture = Game1.content.Load<Texture2D>(@"LooseSprites\DialogBoxGreen");
            MinimalistProfessionButton.ProfessionButtonTexture = Game1.content.Load<Texture2D>(@"LooseSprites\boardGameBorder");

            string prestigeIconFilePath = Path.Combine(ModPath, "assets", "prestige-icon.png");
            Logger.LogInformation($"Prestige Icon Path: {prestigeIconFilePath}");
            var prestigeIconFileStream = new FileStream(prestigeIconFilePath, FileMode.Open);
            PrestigeIconTexture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, prestigeIconFileStream);

            string checkmarkFilePath = Path.Combine(ModPath, "assets", "checkmark.png");
            Logger.LogInformation($"Checkmark Path: {checkmarkFilePath}");
            var checkmarkFileStream = new FileStream(checkmarkFilePath, FileMode.Open);
            CheckmarkTexture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, checkmarkFileStream);
            Logger.LogInformation("Sprites loaded.");
        }

        /// <summary>Raised after the game is launched, right before the first update tick. This happens once per game session (unrelated to loading saves). All mods are loaded and initialised at this point, so this is a good time to set up mod integrations.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // init mod
            this.LoadSprites();
            PrestigeSaveData.Instance.Read();
            ModHandler.Initialise();

            // register commands
            if (Config.TestingMode) this.RegisterTestingCommands();
            this.RegisterCommands();
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            this.CheckForGameSave();
            this.CheckForLevelUpMenu();
            if (e.IsOneSecond && this.SaveIsLoaded)
                ExperienceHandler.UpdateExperience(); //one second tick for this, as the detection of changed experience can happen as infrequently as possible. a 10 second tick would be well within tolerance.
        }

        private void CheckForGameSave()
        {
            if (!Game1.newDay || Game1.fadeToBlackAlpha <= 0.95f)
                return;
            Logger.LogInformation("New game day detected.");
            PrestigeSaveData.Instance.Save();
        }

        private void CheckForLevelUpMenu()
        {
            if (Game1.activeClickableMenu == null)
                return;

            foreach (var skill in Skill.AllSkills)
            {
                var levelUpManager = skill.LevelUpManager;
                if (!levelUpManager.IsMenu(Game1.activeClickableMenu))
                    continue;

                int currentLevel = levelUpManager.GetLevel.Invoke();
                if (currentLevel % 5 != 0)
                    return;

                Logger.LogInformation("Level up menu as profession chooser detected.");
                Game1.activeClickableMenu = levelUpManager.CreateNewLevelUpMenu.Invoke(skill, currentLevel);
                Logger.LogInformation("Replaced level up menu with custom menu.");
            }
        }

        private void RegisterTestingCommands()
        {
            Logger.LogInformation("Registering Testing commands...");
            SkillPrestigeCommand.RegisterCommands(this.Helper.ConsoleCommands, true);
            Logger.LogInformation("Testing commands registered.");
        }

        private void RegisterCommands()
        {
            Logger.LogInformation("Registering commands...");
            SkillPrestigeCommand.RegisterCommands(this.Helper.ConsoleCommands, false);
            Logger.LogInformation("Commands registered.");
        }
    }
}
