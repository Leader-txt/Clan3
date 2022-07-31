using System;
using TShockAPI;

namespace Clans3
{
	// Token: 0x02000005 RID: 5
	public class PlayerInfo
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600001F RID: 31 RVA: 0x0000753F File Offset: 0x0000573F
		// (set) Token: 0x06000020 RID: 32 RVA: 0x00007547 File Offset: 0x00005747
		public PlayerData Backup { get; set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000021 RID: 33 RVA: 0x00007550 File Offset: 0x00005750
		// (set) Token: 0x06000022 RID: 34 RVA: 0x00007558 File Offset: 0x00005758
		public string CopyingUserName { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000023 RID: 35 RVA: 0x00007561 File Offset: 0x00005761
		// (set) Token: 0x06000024 RID: 36 RVA: 0x00007569 File Offset: 0x00005769
		public int UserID { get; set; }

		// Token: 0x06000025 RID: 37 RVA: 0x00007572 File Offset: 0x00005772
		public PlayerInfo()
		{
			this.Backup = null;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00007581 File Offset: 0x00005781
		public bool Restore(TSPlayer player)
		{
			if (this.Backup == null)
			{
				return false;
			}
			this.Backup.RestoreCharacter(player);
			this.Backup = null;
			this.CopyingUserName = "";
			return true;
		}

		// Token: 0x0400000D RID: 13
		public const string KEY = "Clans3_Data";
	}
}
