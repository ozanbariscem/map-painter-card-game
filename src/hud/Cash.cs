using Godot;
using System;

namespace HUD
{
    public class Cash : Control
    {
        private Label label;

        public override void _Ready()
        {
            label = GetNode("Label") as Label;
            Player.OnGoldChanged += HandlePlayerGoldChanged;
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            Player.OnGoldChanged -= HandlePlayerGoldChanged;
        }

        private void HandlePlayerGoldChanged(Player player)
        {
            // ignore if this is a bot, might wanna change in future because spectating is imppossible with this structure
            if (player.IsBot) return;
            label.Text = $"Gold: {player.Gold}";
        }
    }
}
