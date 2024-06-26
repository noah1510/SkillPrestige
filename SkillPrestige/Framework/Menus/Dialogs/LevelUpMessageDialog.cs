using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

// ReSharper disable PossibleLossOfFraction

namespace SkillPrestige.Framework.Menus.Dialogs
{
    /// <summary>Represents a message dialog for skill level up messages.</summary>
    internal class LevelUpMessageDialog : MessageDialog
    {
        private readonly Skill Skill;
        // ReSharper disable once MemberCanBePrivate.Global
        protected readonly int YPositionOfHeaderPartition;

        public LevelUpMessageDialog(Rectangle bounds, string message, Skill skill)
            : base(bounds, message)
        {
            this.Skill = skill;
            this.YPositionOfHeaderPartition = this.yPositionOnScreen + (Game1.tileSize * 2.5).Floor();
            this.allClickableComponents = new List<ClickableComponent> { this.upperRightCloseButton };
        }

        protected override void DrawDecorations(SpriteBatch spriteBatch)
        {
            base.DrawDecorations(spriteBatch);
            this.DrawLevelUpHeader(spriteBatch);
        }

        private void DrawLevelUpHeader(SpriteBatch spriteBatch)
        {
            string title = $"{this.Skill.Type.Name} Level Up";
            this.DrawSkillIcon(spriteBatch, new Vector2(this.xPositionOnScreen + spaceToClearSideBorder + borderWidth, this.yPositionOnScreen + spaceToClearTopBorder + Game1.tileSize / 4));
            spriteBatch.DrawString(Game1.dialogueFont, title, new Vector2(this.xPositionOnScreen + this.width / 2 - Game1.dialogueFont.MeasureString(title).X / 2f, this.yPositionOnScreen + spaceToClearTopBorder + Game1.tileSize / 4), Game1.textColor);
            this.DrawSkillIcon(spriteBatch, new Vector2(this.xPositionOnScreen + this.width - spaceToClearSideBorder - borderWidth - Game1.tileSize, this.yPositionOnScreen + spaceToClearTopBorder + Game1.tileSize / 4));
            this.drawHorizontalPartition(spriteBatch, this.yPositionOnScreen + (Game1.tileSize * 2.5).Floor());
        }

        private void DrawSkillIcon(SpriteBatch spriteBatch, Vector2 location)
        {
            Utility.drawWithShadow(spriteBatch, this.Skill.SkillIconTexture, location, this.Skill.SourceRectangleForSkillIcon, Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom, false, 0.88f);
        }

        protected override void DrawMessage(SpriteBatch spriteBatch)
        {
            const int textPadding = 2 * Game1.pixelZoom;
            int xLocationOfMessage = this.xPositionOnScreen + borderWidth + textPadding;
            int yLocationOfMessage = this.YPositionOfHeaderPartition + spaceToClearTopBorder;
            this.DrawMessage(spriteBatch, Game1.dialogueFont, new Vector2(xLocationOfMessage, yLocationOfMessage), this.width - borderWidth * 2);
        }
    }
}
