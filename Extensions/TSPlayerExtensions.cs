using System;
using Microsoft.Xna.Framework;
using TShockAPI;

namespace Clans3.Extensions
{
	// Token: 0x02000006 RID: 6
	public static class TSPlayerExtensions
	{
		// Token: 0x06000027 RID: 39 RVA: 0x000075AC File Offset: 0x000057AC
		public static PlayerInfo GetPlayerInfo(this TSPlayer player)
		{
			if (!player.ContainsData("Clans3_Data"))
			{
				player.SetData<PlayerInfo>("Clans3_Data", new PlayerInfo());
			}
			return player.GetData<PlayerInfo>("Clans3_Data");
		}

		// Token: 0x06000028 RID: 40 RVA: 0x000075D6 File Offset: 0x000057D6
		public static void PluginMessage(this TSPlayer player, string message, Color color)
		{
			player.SendMessage(Clans3.Tag + message, color);
		}

		// Token: 0x06000029 RID: 41 RVA: 0x000075EA File Offset: 0x000057EA
		public static void PluginErrorMessage(this TSPlayer player, string message)
		{
			player.PluginMessage(message, Color.Red);
		}

		// Token: 0x0600002A RID: 42 RVA: 0x000075F8 File Offset: 0x000057F8
		public static void PluginInfoMessage(this TSPlayer player, string message)
		{
			player.PluginMessage(message, Color.Yellow);
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00007606 File Offset: 0x00005806
		public static void PluginSuccessMessage(this TSPlayer player, string message)
		{
			player.PluginMessage(message, Color.Green);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00007614 File Offset: 0x00005814
		public static void PluginWarningMessage(this TSPlayer player, string message)
		{
			player.PluginMessage(message, Color.OrangeRed);
		}
	}
}
