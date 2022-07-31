using System;
using System.Collections.Generic;

namespace Clans3
{
	// Token: 0x02000002 RID: 2
	public class Clan
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public Clan(string _name, int _owner)
		{
			this.name = _name;
			this.owner = _owner;
			this.admins = new List<int>();
			this.members = new List<int>();
			this.prefix = "";
			this.banned = new List<int>();
			this.cprivate = false;
			this.invited = new List<int>();
		}

		// Token: 0x04000001 RID: 1
		public string name;

		// Token: 0x04000002 RID: 2
		public int owner;

		// Token: 0x04000003 RID: 3
		public List<int> admins;

		// Token: 0x04000004 RID: 4
		public List<int> members;

		// Token: 0x04000005 RID: 5
		public string prefix;

		// Token: 0x04000006 RID: 6
		public List<int> banned;

		// Token: 0x04000007 RID: 7
		public bool cprivate;

		// Token: 0x04000008 RID: 8
		public List<int> invited;
	}
}
