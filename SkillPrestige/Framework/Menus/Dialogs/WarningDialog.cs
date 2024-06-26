using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkillPrestige.Framework.InputHandling;
using SkillPrestige.Framework.Menus.Elements.Buttons;
using SkillPrestige.Logging;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace SkillPrestige.Framework.Menus.Dialogs
{
    internal class WarningDialog : IClickableMenu, IInputHandler
    {
        private OkayCallback OnOkay { get; }
        private CancelCallback OnCancel { get; }
        private bool ButtonsInstantiated;
        private int DebounceTimer = 10;
        private TextureButton OkayButton;
        private TextureButton CancelButton;
        private readonly string Message;

        public delegate void OkayCallback();
        public delegate void CancelCallback();

        public WarningDialog(Rectangle bounds, string message, OkayCallback okayCallback, CancelCallback cancelCallback)
            : base(bounds.X, bounds.Y, bounds.Width, bounds.Height, true)
        {
            Logger.LogVerbose("Warning dialog created");
            this.OnOkay = okayCallback;
            this.OnCancel = cancelCallback;
            this.exitFunction = this.Cancel;
            this.Message = message;
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            if (this.DebounceTimer > 0)
                this.DebounceTimer--;

            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);
            const int textPadding = 2 * Game1.pixelZoom;
            Game1.spriteBatch.DrawString(Game1.dialogueFont,
                this.Message.WrapText(Game1.dialogueFont, this.width - spaceToClearSideBorder * 2),
                new Vector2(this.xPositionOnScreen + spaceToClearSideBorder * 2 + textPadding,
                    this.yPositionOnScreen + spaceToClearTopBorder + textPadding), Game1.textColor);
            this.upperRightCloseButton?.draw(spriteBatch);
            if (!this.ButtonsInstantiated)
                this.InstantiateButtons();
            this.OkayButton.Draw(spriteBatch);
            this.CancelButton.Draw(spriteBatch);
            this.OkayButton.DrawHoverText(spriteBatch);
            this.CancelButton.DrawHoverText(spriteBatch);
            Mouse.DrawCursor(spriteBatch);
        }

        /// <summary>Raised after the player moves the in-game cursor.</summary>
        /// <param name="e">The event data.</param>
        public void OnCursorMoved(CursorMovedEventArgs e)
        {
            if (this.DebounceTimer > 0)
                return;

            this.OkayButton.OnCursorMoved(e);
            this.CancelButton.OnCursorMoved(e);
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="e">The event data.</param>
        /// <param name="isClick">Whether the button press is a click.</param>
        public void OnButtonPressed(ButtonPressedEventArgs e, bool isClick)
        {

            if (this.DebounceTimer > 0)
                return;

            this.OkayButton.OnButtonPressed(e, isClick);
            this.CancelButton.OnButtonPressed(e, isClick);
        }

        private void Cancel()
        {
            Logger.LogInformation("Warning Dialog - Cancel/Close called.");
            this.OnCancel.Invoke();
        }

        private void InstantiateButtons()
        {
            if (this.ButtonsInstantiated)
                return;
            this.ButtonsInstantiated = true;
            Logger.LogVerbose("Warning Dialog - Instantiating Okay/Cancel buttons...");
            const int buttonSize = Game1.tileSize;
            const int buttonPadding = Game1.tileSize * 4;
            var okayButtonBounds = new Rectangle(this.xPositionOnScreen + this.width - buttonSize - spaceToClearSideBorder * 3, this.yPositionOnScreen + this.height - (buttonSize * 1.5).Floor(), buttonSize, buttonSize);
            this.OkayButton = new TextureButton(okayButtonBounds, Game1.mouseCursors, new Rectangle(128, 256, 64, 64), this.Okay, "Prestige Skill")
                {
                    ClickableTextureComponent =
                    {
                        myID = 1
                    }
                };
            Logger.LogVerbose("Warning Dialog - Okay button instantiated.");
            var cancelButtonBounds = okayButtonBounds;
            cancelButtonBounds.X -= buttonSize + buttonPadding;
            this.CancelButton = new TextureButton(cancelButtonBounds, Game1.mouseCursors, new Rectangle(192, 256, 64, 64), () => this.exitThisMenu(false), "Cancel");
            Logger.LogVerbose("Warning Dialog - Cancel button instantiated.");
            this.CancelButton.ClickableTextureComponent.myID = 2;
            this.OkayButton.ClickableTextureComponent.upNeighborID = this.upperRightCloseButton.myID;
            this.CancelButton.ClickableTextureComponent.upNeighborID = this.upperRightCloseButton.myID;
            this.upperRightCloseButton.downNeighborID = this.OkayButton.ClickableTextureComponent.myID;
            this.upperRightCloseButton.leftNeighborID = this.CancelButton.ClickableTextureComponent.myID;
            this.OkayButton.ClickableTextureComponent.leftNeighborID = this.CancelButton.ClickableTextureComponent.myID;
            this.CancelButton.ClickableTextureComponent.rightNeighborID = this.OkayButton.ClickableTextureComponent.myID;
            this.allClickableComponents = new List<ClickableComponent>
            {
                this.upperRightCloseButton,
                this.OkayButton.ClickableTextureComponent,
                this.CancelButton.ClickableTextureComponent
            };
        }

        private void Okay()
        {
            Logger.LogVerbose("Warning Dialog - Okay button called.");
            this.OnOkay.Invoke();
            this.exitThisMenu(false);
        }
    }
}
