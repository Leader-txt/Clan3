using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using Clans3.Extensions;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace Clans3
{
	// Token: 0x02000003 RID: 3
	[ApiVersion(2, 1)]
	public class Clans3 : TerrariaPlugin
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000002 RID: 2 RVA: 0x000020AF File Offset: 0x000002AF
		public static string Tag
		{
			get
			{
				return TShock.Utils.ColorTag("Clans:", Color.Teal);
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000003 RID: 3 RVA: 0x000020C5 File Offset: 0x000002C5
		public override string Name
		{
			get
			{
				return "Clans3";
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000004 RID: 4 RVA: 0x000020CC File Offset: 0x000002CC
		public override string Author
		{
			get
			{
				return "Zaicon制作,nnt汉化,Leader升级";
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000005 RID: 5 RVA: 0x000020D3 File Offset: 0x000002D3
		public override string Description
		{
			get
			{
				return "Clan Plugin for TShock";
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000006 RID: 6 RVA: 0x000020DA File Offset: 0x000002DA
		public override Version Version
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Version;
			}
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000020EB File Offset: 0x000002EB
		public Clans3(Main game) : base(game)
		{
			base.Order = 1;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x000020FC File Offset: 0x000002FC
		public override void Initialize()
		{
			Clans3.clans = new List<Clan>();
			ServerApi.Hooks.GameInitialize.Register(this, new HookHandler<EventArgs>(this.OnInitialize));
			ServerApi.Hooks.ServerChat.Register(this, new HookHandler<ServerChatEventArgs>(this.onChat));
			ServerApi.Hooks.NetGreetPlayer.Register(this, new HookHandler<GreetPlayerEventArgs>(this.onGreet));
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002168 File Offset: 0x00000368
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, new HookHandler<EventArgs>(this.OnInitialize));
				ServerApi.Hooks.ServerChat.Deregister(this, new HookHandler<ServerChatEventArgs>(this.onChat));
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, new HookHandler<GreetPlayerEventArgs>(this.onGreet));
			}
			base.Dispose(disposing);
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000021D8 File Offset: 0x000003D8
		private void OnInitialize(EventArgs args)
		{
			DB.DBConnect();
			DB.loadClans();
			Clans3.invitebc = new Timer(300000.0)
			{
				AutoReset = true,
				Enabled = true
			};
			Clans3.invitebc.Elapsed += this.onUpdate;
			Clans3.ignores = new Dictionary<int, List<int>>();
			Commands.ChatCommands.Add(new Command("clans.use", new CommandDelegate(this.ClansMain), new string[]
			{
				"clan"
			}));
			Commands.ChatCommands.Add(new Command("clans.use", new CommandDelegate(this.CChat), new string[]
			{
				"c"
			}));
			Commands.ChatCommands.Add(new Command("clans.reload", new CommandDelegate(this.CReload), new string[]
			{
				"clanreload"
			}));
			Commands.ChatCommands.Add(new Command("clans.mod", new CommandDelegate(this.ClansStaff), new string[]
			{
				"clanstaff",
				"cs"
			}));
			Commands.ChatCommands.Add(new Command("ignore.use", new CommandDelegate(this.Ignore), new string[]
			{
				"ignore"
			}));
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002320 File Offset: 0x00000520
		private void onUpdate(object sender, ElapsedEventArgs e)
		{
			foreach (Clan clan in Clans3.clans)
			{
				if (clan.invited.Count > 0)
				{
					foreach (int num in clan.invited)
					{
						string name = TShock.UserAccounts.GetUserAccountByID(num).Name;
						List<TSPlayer> list = TSPlayer.FindByNameOrID(name);
						if (list.Count > 0)
						{
							foreach (TSPlayer tsplayer in list)
							{
								if (tsplayer.Account.ID == num)
								{
									tsplayer.SendInfoMessage("你收到了来自 " + clan.name + " 公会的邀请! 输入 '/clan accept' 来加入这个公会或输入 '/clan deny' 来拒绝这个邀请.");
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000244C File Offset: 0x0000064C
		private void onGreet(GreetPlayerEventArgs args)
		{
			TSPlayer tsplayer = TShock.Players[args.Who];
			if (tsplayer != null && tsplayer.Active && tsplayer.IsLoggedIn)
			{
				int num = this.findClan(tsplayer.Account.ID);
				if (num != -1)
				{
					tsplayer.SetData<string>("clan", Clans3.clans[num].prefix);
				}
			}
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000024AC File Offset: 0x000006AC
		private void onChat(ServerChatEventArgs args)
		{
            try
			{
				TSPlayer tsplayer = TShock.Players[args.Who];
				if (tsplayer == null || !tsplayer.Active || args.Handled || args.Text.StartsWith(TShock.Config.Settings.CommandSpecifier) || args.Text.StartsWith(TShock.Config.Settings.CommandSilentSpecifier) || tsplayer.mute)
				{
					return;
				}
				string text;
				if (!tsplayer.IsLoggedIn)
				{
					text = tsplayer.Group.Prefix;
				}
				else
				{
					int index = this.findClan(tsplayer.Account.ID);
					text = ((Clans3.clans[index].prefix == "") ? tsplayer.Group.Prefix : ("(" + Clans3.clans[index].prefix + ") " + tsplayer.Group.Prefix));
				}
				TSPlayer[] players = TShock.Players;
				foreach (TSPlayer tsplayer2 in players)
				{
					if (tsplayer2 != null && (!tsplayer2.IsLoggedIn || !tsplayer.IsLoggedIn || !Clans3.ignores.ContainsKey(tsplayer2.Account.ID) || !Clans3.ignores[tsplayer2.Account.ID].Contains(tsplayer.Account.ID)) && (tsplayer.IsLoggedIn || !tsplayer2.IsLoggedIn || !Clans3.ignores.ContainsKey(tsplayer2.Account.ID) || !Clans3.ignores[tsplayer2.Account.ID].Contains(-1)) && (!tsplayer2.IsLoggedIn || !Clans3.ignores.ContainsKey(tsplayer2.Account.ID) || !Clans3.ignores[tsplayer2.Account.ID].Contains(-2) || tsplayer.HasPermission("ignore.immune")))
					{
						tsplayer2.SendMessage(string.Format(TShock.Config.Settings.ChatFormat, new object[]
						{
						tsplayer.Group.Name,
						text,
						tsplayer.Name,
						tsplayer.Group.Suffix,
						args.Text
						}), new Color((int)tsplayer.Group.R, (int)tsplayer.Group.G, (int)tsplayer.Group.B));
					}
				}
				TSPlayer.Server.SendMessage(string.Format(TShock.Config.Settings.ChatFormat, new object[]
				{
				tsplayer.Group.Name,
				text,
				tsplayer.Name,
				tsplayer.Group.Suffix,
				args.Text
				}), new Color((int)tsplayer.Group.R, (int)tsplayer.Group.G, (int)tsplayer.Group.B));
				args.Handled = true;
			}
            catch { }
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000279C File Offset: 0x0000099C
		private void ClansMain(CommandArgs args)
		{
			int num = this.findClan(args.Player.Account.ID);
			if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() == "help")
			{
				List<string> list = new List<string>();
				if (num != -1 && Clans3.clans[num].owner == args.Player.Account.ID)
				{
					list.Add("prefix <聊天前缀> - 添加或更改你的公会聊天前缀.");
					list.Add("promote <用户名> - 提升一个公会成员为公会管理.");
					list.Add("demote <用户名> - 将一个公会管理变成公会成员.");
					list.Add("private - 切换你公会的私密状态;其他成员无法主动加入公会.");
				}
				if (num != -1 && (Clans3.clans[num].admins.Contains(args.Player.Account.ID) || Clans3.clans[num].owner == args.Player.Account.ID))
				{
					list.Add("kick <用户名> - 驱逐一个公会成员.");
					list.Add("ban <用户名> - 封禁一个公会成员让他再也无法加入你的公会.");
					list.Add("unban <用户名> - 解封一个公会成员的封禁状态.");
				}
				if (num != -1 && (Clans3.clans[num].members.Contains(args.Player.Account.ID) || Clans3.clans[num].admins.Contains(args.Player.Account.ID) || Clans3.clans[num].owner == args.Player.Account.ID))
				{
					if (!Clans3.clans[num].cprivate || !Clans3.clans[num].members.Contains(args.Player.Account.ID))
					{
						list.Add("invite <用户名> - 邀请一个用户加入你的公会.");
					}
					list.Add("members - 列出公会所有成员.");
					list.Add("leave - 离开你现在的公会.");
				}
				if (num == -1)
				{
					list.Add("create <公会名> - 创造一个新公会.");
					list.Add("join <公会名> - 加入一个公会.");
				}
				list.Add("list - 列出所有公会.");
				list.Add("check <用户名> - 查看用户现在在哪个公会中.");
				int num2;
				if (PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out num2))
				{
					PaginationTools.SendPage(args.Player, num2, list, new PaginationTools.Settings
					{
						HeaderFormat = "Clan公会插件可用命令 ({0}/{1}):",
						FooterFormat = StringExt.SFormat("输入 {0}clan help {{0}} 查看更多.", new object[]
						{
							args.Silent ? TShock.Config.Settings.CommandSilentSpecifier : TShock.Config.Settings.CommandSpecifier
						})
					});
					return;
				}
			}
			else if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "leave")
			{
				if (num == -1)
				{
					args.Player.SendErrorMessage("你不在工会里!");
					return;
				}
				if (Clans3.clans[num].owner != args.Player.Account.ID)
				{
					args.Player.RemoveData("clan");
					if (Clans3.clans[num].admins.Contains(args.Player.Account.ID))
					{
						Clans3.clans[num].admins.Remove(args.Player.Account.ID);
						DB.changeMembers(Clans3.clans[num].owner, Clans3.clans[num]);
						args.Player.SendSuccessMessage("你离开了你的公会.");
					}
					else
					{
						Clans3.clans[num].members.Remove(args.Player.Account.ID);
						DB.changeMembers(Clans3.clans[num].owner, Clans3.clans[num]);
						args.Player.SendSuccessMessage("你离开了你的公会.");
					}
					TShock.Log.Info(args.Player.Account.Name + " 离开了 " + Clans3.clans[num].name + " 公会.");
					return;
				}
				args.Player.RemoveData("clan");
				if (Clans3.clans[num].admins.Count > 0)
				{
					Clans3.clans[num].owner = Clans3.clans[num].admins[0];
					Clans3.clans[num].admins.RemoveAt(0);
					DB.changeOwner(args.Player.Account.ID, Clans3.clans[num]);
					string name = TShock.UserAccounts.GetUserAccountByID(Clans3.clans[num].owner).Name;
					args.Player.SendSuccessMessage("你离开了你的公会! 现在公会的所有权已转交给 " + name);
					List<TSPlayer> list2 = TSPlayer.FindByNameOrID(name);
					if (list2.Count == 1 && list2[0].Account.ID == Clans3.clans[num].owner)
					{
						list2[0].SendInfoMessage("你现在是 " + Clans3.clans[num].name + " 公会的管理员!");
					}
					TShock.Log.Info(args.Player.Account.Name + " 退出了 " + Clans3.clans[num].name + " 公会.");
					TShock.Log.Info(name + " 现在是 " + Clans3.clans[num].name + " 公会的管理员.");
					return;
				}
				if (Clans3.clans[num].members.Count > 0)
				{
					Clans3.clans[num].owner = Clans3.clans[num].members[0];
					Clans3.clans[num].members.RemoveAt(0);
					DB.changeOwner(args.Player.Account.ID, Clans3.clans[num]);
					string name2 = TShock.UserAccounts.GetUserAccountByID(Clans3.clans[num].owner).Name;
					args.Player.SendSuccessMessage("你离开了你的公会! 现在公会的所有权已转交给 " + name2);
					List<TSPlayer> list3 = TSPlayer.FindByNameOrID(name2);
					if (list3.Count == 1 && list3[0].Account.ID == Clans3.clans[num].owner)
					{
						list3[0].SendInfoMessage("你现在是 " + Clans3.clans[num].name + " 公会的管理员!");
					}
					TShock.Log.Info(args.Player.Account.Name + " 退出了 " + Clans3.clans[num].name + " 公会.");
					TShock.Log.Info(name2 + " 现在是 " + Clans3.clans[num].name + " 公会的管理员.");
					return;
				}
				DB.removeClan(Clans3.clans[num].owner);
				TShock.Log.Info(args.Player.Account.Name + " 退出了 " + Clans3.clans[num].name + " 公会.");
				TShock.Log.Info(Clans3.clans[num].name + " 已解散.");
				Clans3.clans.RemoveAt(num);
				args.Player.SendSuccessMessage("你离开了你的公会! 由于公会没有其他成员,公会已解散.");
				return;
			}
			else if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() == "list")
			{
				int num3;
				if (PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out num3))
				{
					Dictionary<string, int> dictionary = new Dictionary<string, int>();
					foreach (Clan clan in Clans3.clans)
					{
						int value = 1 + clan.admins.Count + clan.members.Count;
						if (!clan.banned.Contains(args.Player.Account.ID) && !clan.cprivate)
						{
							dictionary.Add(clan.name, value);
						}
					}
					IEnumerable<string> source = dictionary.OrderByDescending(delegate(KeyValuePair<string, int> entry)
					{
						KeyValuePair<string, int> keyValuePair = entry;
						return keyValuePair.Value;
					}).Select(delegate(KeyValuePair<string, int> entry)
					{
						KeyValuePair<string, int> keyValuePair = entry;
						return keyValuePair.Key;
					});
					PaginationTools.SendPage(args.Player, num3, source.ToList<string>(), new PaginationTools.Settings
					{
						HeaderFormat = "公会列表 ({0}/{1}):",
						FooterFormat = StringExt.SFormat("输入 {0}clan list {{0}} 查看更多.", new object[]
						{
							args.Silent ? TShock.Config.Settings.CommandSilentSpecifier : TShock.Config.Settings.CommandSpecifier
						})
					});
					return;
				}
			}
			else if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() == "members")
			{
				if (num == -1)
				{
					args.Player.SendErrorMessage("你不在公会里!");
					return;
				}
				int num4;
				if (PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out num4))
				{
					List<string> list4 = new List<string>();
					list4.Add(TShock.UserAccounts.GetUserAccountByID(Clans3.clans[num].owner).Name + " (创建者)");
					foreach (int num5 in Clans3.clans[num].admins)
					{
						list4.Add(TShock.UserAccounts.GetUserAccountByID(num5).Name + " (管理员)");
					}
					foreach (int num6 in Clans3.clans[num].members)
					{
						list4.Add(TShock.UserAccounts.GetUserAccountByID(num6).Name);
					}
					foreach (int num7 in Clans3.clans[num].invited)
					{
						list4.Add(TShock.UserAccounts.GetUserAccountByID(num7).Name + " (已邀请)");
					}
					PaginationTools.SendPage(args.Player, num4, list4, new PaginationTools.Settings
					{
						HeaderFormat = Clans3.clans[num].name + " 公会成员 ({0}/{1}):",
						FooterFormat = StringExt.SFormat("输入 {0}clan members {{0}} 查看更多成员.", new object[]
						{
							args.Silent ? TShock.Config.Settings.CommandSilentSpecifier : TShock.Config.Settings.CommandSpecifier
						})
					});
					return;
				}
			}
			else if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "private")
			{
				if (num == -1)
				{
					args.Player.SendErrorMessage("你不在公会里!");
					return;
				}
				if (Clans3.clans[num].owner != args.Player.Account.ID)
				{
					args.Player.SendErrorMessage("只有公会的所有者才能设置公会私密状态!");
					return;
				}
				Clans3.clans[num].cprivate = !Clans3.clans[num].cprivate;
				DB.changePrivate(Clans3.clans[num].owner, Clans3.clans[num].cprivate);
				TShock.Log.Info(string.Concat(new string[]
				{
					args.Player.Account.Name,
					" 将 ",
					Clans3.clans[num].name,
					" 公会的私密状态变为 ",
					Clans3.clans[num].cprivate ? "私密." : "公开."
				}));
				args.Player.SendSuccessMessage("成功将公会状态设置为 " + (Clans3.clans[num].cprivate ? "私密." : "公开."));
				return;
			}
			else if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "accept")
			{
				if (num != -1)
				{
					args.Player.SendErrorMessage("你已加入公会!");
					return;
				}
				int invite = this.getInvite(args.Player.Account.ID);
				if (invite == -1)
				{
					args.Player.SendErrorMessage("你没有收到公会邀请!");
					return;
				}
				Clans3.clans[invite].invited.Remove(args.Player.Account.ID);
				Clans3.clans[invite].members.Add(args.Player.Account.ID);
				TSPlayer[] players = TShock.Players;
				foreach (TSPlayer tsplayer in players)
				{
					if (tsplayer != null && tsplayer.Active && tsplayer.IsLoggedIn && this.findClan(tsplayer.Account.ID) == invite)
					{
						tsplayer.SendInfoMessage(args.Player.Name + " 加入了 " + Clans3.clans[invite].name + " 公会!大家欢迎!");
					}
				}
				TShock.Log.Info(args.Player.Account.Name + " 接受了 " + Clans3.clans[invite].name + " 公会的邀请.");
				DB.changeMembers(Clans3.clans[invite].owner, Clans3.clans[invite]);
				args.Player.SetData<string>("clan", Clans3.clans[invite].prefix);
				return;
			}
			else if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "deny")
			{
				if (num != -1)
				{
					args.Player.SendErrorMessage("你已加入公会!");
					return;
				}
				int invite2 = this.getInvite(args.Player.Account.ID);
				if (invite2 == -1)
				{
					args.Player.SendErrorMessage("你没有收到公会邀请!");
					return;
				}
				Clans3.clans[invite2].invited.Remove(args.Player.Account.ID);
				args.Player.SendSuccessMessage("拒绝了公会邀请.");
				return;
			}
			else if (args.Parameters.Count > 1)
			{
				string a = args.Parameters[0].ToLower();
				List<string> parameters = args.Parameters;
				parameters.RemoveAt(0);
				string text = string.Join(" ", parameters);
				if (a == "create")
				{
					if (num != -1)
					{
						args.Player.SendErrorMessage("你现在不能建造公会!");
						return;
					}
					List<int> list5 = this.findClanByName(text);
					if (list5.Count > 0)
					{
						foreach (int index in list5)
						{
							if (Clans3.clans[index].name == text)
							{
								args.Player.SendErrorMessage("该公会名已被使用!");
								return;
							}
						}
					}
					if (text.Contains("[c/") || text.Contains("[i"))
					{
						args.Player.SendErrorMessage("你不能使用物品和颜色标签!");
						return;
					}
					Clans3.clans.Add(new Clan(text, args.Player.Account.ID));
					DB.newClan(text, args.Player.Account.ID);
					args.Player.SendSuccessMessage("你创建了 " + text + " 公会! 输入 /clan prefix <前缀> 来设置聊天前缀.");
					TShock.Log.Info(args.Player.Account.Name + " 创建了 " + text + " 公会.");
					return;
				}
				else if (a == "check")
				{
					List<TSPlayer> list6 = TSPlayer.FindByNameOrID(text);
					if (list6.Count == 1)
					{
						TSPlayer tsplayer2 = list6[0];
						int index2 = this.findClan(tsplayer2.Account.ID);
						if (this.findClan(tsplayer2.Account.ID) != -1)
						{
							args.Player.SendInfoMessage(tsplayer2.Name + " 现在在 " + Clans3.clans[index2].name + " 公会!");
							return;
						}
						args.Player.SendInfoMessage(tsplayer2.Name + " 没有加入公会!");
						return;
					}
					else
					{
						if (list6.Count > 1)
						{
							args.Player.SendMultipleMatchError(from p in list6
							select p.Name);
							return;
						}
						UserAccount userAccountByName = TShock.UserAccounts.GetUserAccountByName(text);
						if (!(userAccountByName != null))
						{
							args.Player.SendErrorMessage("没有找到该玩家: " + userAccountByName.Name);
							return;
						}
						int num8 = this.findClan(userAccountByName.ID);
						if (num8 == -1)
						{
							args.Player.SendInfoMessage(userAccountByName.Name + " 没有加入公会!");
							return;
						}
						args.Player.SendInfoMessage(userAccountByName.Name + " 现在在 " + Clans3.clans[num8].name + " 公会!");
						return;
					}
				}
				else if (a == "prefix")
				{
					if (num == -1)
					{
						args.Player.SendErrorMessage("你不在公会里!");
						return;
					}
					if (Clans3.clans[num].owner != args.Player.Account.ID)
					{
						args.Player.SendErrorMessage("只有公会创造者才可以改变前缀.");
						return;
					}
					if (text.ToLower().Contains("[c") || text.ToLower().Contains("[i") || text.ToLower().Contains("[g"))
					{
						args.Player.SendErrorMessage("你不能使用物品和颜色标签!");
						return;
					}
					if (text.Length > 20)
					{
						args.Player.SendErrorMessage("前缀太长!");
						return;
					}
					Clans3.clans[num].prefix = text;
					DB.clanPrefix(args.Player.Account.ID, text);
					args.Player.SendSuccessMessage("成功将公会前缀变更为 \"" + text + "\"!");
					TShock.Log.Info(string.Concat(new string[]
					{
						args.Player.Account.Name,
						" 将 ",
						Clans3.clans[num].name,
						" 公会前缀改为 \"",
						text,
						"\"."
					}));
					TSPlayer[] players2 = TShock.Players;
					foreach (TSPlayer tsplayer3 in players2)
					{
						if (tsplayer3 != null && tsplayer3.IsLoggedIn && this.findClan(tsplayer3.Account.ID) == num)
						{
							tsplayer3.SetData<string>("clan", Clans3.clans[num].prefix);
						}
					}
					return;
				}
				else if (a == "invite")
				{
					if (num == -1)
					{
						args.Player.SendErrorMessage("你不在公会里!");
						return;
					}
					if (Clans3.clans[num].cprivate && Clans3.clans[num].members.Contains(args.Player.Account.ID))
					{
						args.Player.SendErrorMessage("只有公会的管理员才能邀请玩家.");
						return;
					}
					UserAccount userAccountByName2 = TShock.UserAccounts.GetUserAccountByName(text);
					if (userAccountByName2 == null)
					{
						args.Player.SendErrorMessage("没有找到该用户 " + text + ".");
						return;
					}
					int num9 = this.findClan(userAccountByName2.ID);
					int invite3 = this.getInvite(userAccountByName2.ID);
					if (num9 == num)
					{
						args.Player.SendErrorMessage("该用户已在你的公会中!");
						return;
					}
					if (num9 != -1)
					{
						args.Player.SendErrorMessage("该用户已在公会!");
						return;
					}
					if (Clans3.clans[num].banned.Contains(userAccountByName2.ID))
					{
						args.Player.SendErrorMessage("该用户已被封禁,无法加入.");
						return;
					}
					if (invite3 != -1 && invite3 != num)
					{
						args.Player.SendErrorMessage("该用户还有未处理的公会邀请.");
						return;
					}
					if (invite3 != -1 && invite3 == num)
					{
						args.Player.SendErrorMessage("该用户已收到你的公会邀请!");
						return;
					}
					if (!TShock.Groups.GetGroupByName(userAccountByName2.Group).HasPermission("clans.use"))
					{
						args.Player.SendErrorMessage("该用户没有接受邀请的命令权限!");
						return;
					}
					Clans3.clans[num].invited.Add(userAccountByName2.ID);
					string name3 = TShock.UserAccounts.GetUserAccountByID(userAccountByName2.ID).Name;
					List<TSPlayer> list7 = TSPlayer.FindByNameOrID(name3);
					if (list7.Count > 0)
					{
						foreach (TSPlayer tsplayer4 in list7)
						{
							UserAccount account = tsplayer4.Account;
							int? num10 = (account != null) ? new int?(account.ID) : null;
							int id = userAccountByName2.ID;
							if (num10.GetValueOrDefault() == id & num10 != null)
							{
								tsplayer4.SendInfoMessage("你收到了来自 " + Clans3.clans[num].name + " 公会的邀请! 输入 '/clan accept' 来加入这个公会或输入 '/clan deny' 来拒绝这个邀请.");
							}
						}
					}
					args.Player.SendSuccessMessage(userAccountByName2.Name + " 接受了来自 " + Clans3.clans[num].name + " 公会的邀请!");
					return;
				}
				else if (a == "join")
				{
					if (num != -1)
					{
						args.Player.SendErrorMessage("你不能加入多个公会!");
						return;
					}
					List<int> list8 = this.findClanByName(text);
					if (list8.Count == 0)
					{
						args.Player.SendErrorMessage("没有找到该公会 " + text + ".");
						return;
					}
					if (list8.Count > 1)
					{
						List<string> list9 = new List<string>();
						foreach (int index3 in list8)
						{
							list9.Add(Clans3.clans[index3].name);
						}
						args.Player.SendErrorMessage(string.Format("多个公会匹配: {0}", string.Join(", ", list9)));
						return;
					}
					num = list8[0];
					if (Clans3.clans[num].banned.Contains(args.Player.Account.ID))
					{
						args.Player.SendErrorMessage("你被禁止加入该公会!");
						return;
					}
					if (Clans3.clans[num].cprivate && !Clans3.clans[num].invited.Contains(args.Player.Account.ID))
					{
						args.Player.SendErrorMessage("没有邀请,你不能加入该公会!");
						return;
					}
					if (Clans3.clans[num].invited.Contains(args.Player.Account.ID))
					{
						Clans3.clans[num].invited.Remove(args.Player.Account.ID);
					}
					Clans3.clans[num].members.Add(args.Player.Account.ID);
					TSPlayer[] players3 = TShock.Players;
					foreach (TSPlayer tsplayer5 in players3)
					{
						if (tsplayer5 != null && tsplayer5.Active && tsplayer5.IsLoggedIn && tsplayer5.Index != args.Player.Index)
						{
							int num11 = this.findClan(tsplayer5.Account.ID);
							if (num11 == num)
							{
								tsplayer5.SendInfoMessage(args.Player.Name + " 加入了你的公会!");
							}
						}
					}
					DB.changeMembers(Clans3.clans[num].owner, Clans3.clans[num]);
					TShock.Log.Info(args.Player.Account.Name + " 加入了 " + Clans3.clans[num].name + " 公会.");
					args.Player.SendSuccessMessage("你加入了 " + Clans3.clans[num].name + " 公会!");
					args.Player.SetData<string>("clan", Clans3.clans[num].prefix);
					return;
				}
				else if (a == "kick")
				{
					if (num == -1)
					{
						args.Player.SendErrorMessage("你不在公会里!");
						return;
					}
					if (!Clans3.clans[num].admins.Contains(args.Player.Account.ID) && Clans3.clans[num].owner != args.Player.Account.ID)
					{
						args.Player.SendErrorMessage("你不能把该用户驱逐出你的公会!");
						return;
					}
					List<TSPlayer> list10 = TSPlayer.FindByNameOrID(text);
					if (list10.Count == 0)
					{
						UserAccount userAccountByName3 = TShock.UserAccounts.GetUserAccountByName(text);
						if (!(userAccountByName3 != null))
						{
							args.Player.SendErrorMessage("没有找到该用户 " + text + ".");
							return;
						}
						int num12 = this.findClan(userAccountByName3.ID);
						if (num12 == -1 || num12 != num)
						{
							args.Player.SendErrorMessage(userAccountByName3.Name + " 不是你的公会成员!");
							return;
						}
						if (Clans3.clans[num].owner == userAccountByName3.ID)
						{
							args.Player.SendErrorMessage("你不能驱逐一个公会管理员!");
							return;
						}
						if (Clans3.clans[num].admins.Contains(userAccountByName3.ID))
						{
							args.Player.SendErrorMessage("你不能驱逐一个公会管理员!");
							return;
						}
						Clans3.clans[num].members.Remove(userAccountByName3.ID);
						DB.changeMembers(Clans3.clans[num].owner, Clans3.clans[num]);
						args.Player.SendSuccessMessage("你已驱逐 " + userAccountByName3.Name + " !");
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							userAccountByName3.Name,
							" 驱逐出 ",
							Clans3.clans[num].name,
							" 公会."
						}));
						return;
					}
					else
					{
						if (list10.Count > 1 && list10[0].Name != text)
						{
							args.Player.SendMultipleMatchError(from p in list10
							select p.Name);
							return;
						}
						TSPlayer tsplayer6 = list10[0];
						if (Clans3.clans[num].owner == tsplayer6.Account.ID)
						{
							args.Player.SendErrorMessage("你不能驱逐一个公会管理员!");
							return;
						}
						if (Clans3.clans[num].admins.Contains(tsplayer6.Account.ID))
						{
							args.Player.SendErrorMessage("你不能驱逐一个公会管理员!");
							return;
						}
						Clans3.clans[num].members.Remove(tsplayer6.Account.ID);
						DB.changeMembers(Clans3.clans[num].owner, Clans3.clans[num]);
						args.Player.SendSuccessMessage("你驱逐了 " + tsplayer6.Name + " !");
						tsplayer6.SendInfoMessage(string.Concat(new string[]
						{
							"你已被驱逐出 ",
							Clans3.clans[num].name,
							",处理人: ",
							args.Player.Name,
							"!"
						}));
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							tsplayer6.Name,
							" 驱逐出 ",
							Clans3.clans[num].name,
							" 公会."
						}));
						tsplayer6.RemoveData("clan");
						return;
					}
				}
				else if (a == "ban")
				{
					if (num == -1)
					{
						args.Player.SendErrorMessage("你不在工会里!");
						return;
					}
					if (!Clans3.clans[num].admins.Contains(args.Player.Account.ID) && Clans3.clans[num].owner != args.Player.Account.ID)
					{
						args.Player.SendErrorMessage("你不能把该用户封禁!");
						return;
					}
					List<TSPlayer> list11 = TSPlayer.FindByNameOrID(text);
					if (list11.Count == 0)
					{
						UserAccount userAccountByName4 = TShock.UserAccounts.GetUserAccountByName(text);
						if (!(userAccountByName4 != null))
						{
							args.Player.SendErrorMessage("没有找到该用户 " + text + ".");
							return;
						}
						int num13 = this.findClan(userAccountByName4.ID);
						if (num13 == -1 || num13 != num)
						{
							args.Player.SendErrorMessage(userAccountByName4.Name + " 不是你的公会成员!");
							return;
						}
						if (Clans3.clans[num].owner == userAccountByName4.ID)
						{
							args.Player.SendErrorMessage("你不能封禁一个公会管理员!");
							return;
						}
						if (Clans3.clans[num].admins.Contains(userAccountByName4.ID))
						{
							args.Player.SendErrorMessage("你不能封禁一个公会管理员!");
							return;
						}
						Clans3.clans[num].members.Remove(userAccountByName4.ID);
						Clans3.clans[num].banned.Add(userAccountByName4.ID);
						DB.changeMembers(Clans3.clans[num].owner, Clans3.clans[num]);
						DB.changeBanned(Clans3.clans[num].owner, Clans3.clans[num].banned);
						args.Player.SendSuccessMessage("你封禁了 " + userAccountByName4.Name + " !");
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							userAccountByName4.Name,
							" 添加到 ",
							Clans3.clans[num].name,
							" 公会封禁列表."
						}));
						return;
					}
					else
					{
						if (list11.Count > 1 && list11[0].Name != text)
						{
							args.Player.SendMultipleMatchError(from p in list11
							select p.Name);
							return;
						}
						TSPlayer tsplayer7 = list11[0];
						if (Clans3.clans[num].owner == tsplayer7.Account.ID)
						{
							args.Player.SendErrorMessage("你不能封禁一个公会管理员!");
							return;
						}
						if (Clans3.clans[num].admins.Contains(tsplayer7.Account.ID))
						{
							args.Player.SendErrorMessage("你不能封禁一个公会管理员!");
							return;
						}
						Clans3.clans[num].members.Remove(tsplayer7.Account.ID);
						Clans3.clans[num].banned.Add(tsplayer7.Account.ID);
						DB.changeMembers(Clans3.clans[num].owner, Clans3.clans[num]);
						DB.changeBanned(Clans3.clans[num].owner, Clans3.clans[num].banned);
						args.Player.SendSuccessMessage("你封禁了 " + tsplayer7.Name + "!");
						tsplayer7.SendInfoMessage(string.Concat(new string[]
						{
							"你被 ",
							Clans3.clans[num].name,
							" 公会封禁,处理人: ",
							args.Player.Name,
							"!"
						}));
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							tsplayer7.Name,
							" 添加到 ",
							Clans3.clans[num].name,
							" 公会封禁列表."
						}));
						tsplayer7.RemoveData("clan");
						return;
					}
				}
				else if (a == "unban")
				{
					if (num == -1)
					{
						args.Player.SendErrorMessage("你不在工会里!");
						return;
					}
					if (Clans3.clans[num].owner != args.Player.Account.ID && !Clans3.clans[num].admins.Contains(args.Player.Account.ID))
					{
						args.Player.SendErrorMessage("你不能把该用户解封!");
						return;
					}
					List<TSPlayer> list12 = TSPlayer.FindByNameOrID(text);
					if (list12.Count == 0)
					{
						UserAccount userAccountByName5 = TShock.UserAccounts.GetUserAccountByName(text);
						if (!(userAccountByName5 != null))
						{
							args.Player.SendErrorMessage("没有找到该用户 " + text + ".");
							return;
						}
						if (!Clans3.clans[num].banned.Contains(userAccountByName5.ID))
						{
							args.Player.SendErrorMessage(userAccountByName5.Name + " 没有被封禁!");
							return;
						}
						Clans3.clans[num].banned.Remove(userAccountByName5.ID);
						DB.changeBanned(Clans3.clans[num].owner, Clans3.clans[num].banned);
						args.Player.SendSuccessMessage("你解封了 " + userAccountByName5.Name + " !");
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							userAccountByName5.Name,
							" 移除出 ",
							Clans3.clans[num].name,
							" 公会封禁列表."
						}));
						return;
					}
					else
					{
						if (list12.Count > 1 && list12[0].Name != text)
						{
							args.Player.SendMultipleMatchError(from p in list12
							select p.Name);
							return;
						}
						TSPlayer tsplayer8 = list12[0];
						if (!Clans3.clans[num].banned.Contains(tsplayer8.Account.ID))
						{
							args.Player.SendErrorMessage(tsplayer8.Name + " 没有被封禁!");
							return;
						}
						Clans3.clans[num].banned.Remove(tsplayer8.Account.ID);
						DB.changeBanned(Clans3.clans[num].owner, Clans3.clans[num].banned);
						args.Player.SendSuccessMessage("你解封了 " + tsplayer8.Name + " !");
						tsplayer8.SendInfoMessage(string.Concat(new string[]
						{
							"你已解封 ",
							Clans3.clans[num].name,
							" 处理人: ",
							args.Player.Name,
							"!"
						}));
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							tsplayer8.Name,
							" 移除出 ",
							Clans3.clans[num].name,
							" 公会封禁列表."
						}));
						return;
					}
				}
				else if (a == "promote")
				{
					if (num == -1)
					{
						args.Player.SendErrorMessage("你不在工会里!");
						return;
					}
					if (Clans3.clans[num].owner != args.Player.Account.ID)
					{
						args.Player.SendErrorMessage("你不能提升一个公会管理员!");
						return;
					}
					List<TSPlayer> list13 = TSPlayer.FindByNameOrID(text);
					if (list13.Count == 0)
					{
						UserAccount userAccountByName6 = TShock.UserAccounts.GetUserAccountByName(text);
						if (userAccountByName6 == null)
						{
							args.Player.SendErrorMessage("没有找到该用户 " + text);
							return;
						}
						if (Clans3.clans[num].admins.Contains(userAccountByName6.ID))
						{
							args.Player.SendErrorMessage(userAccountByName6.Name + " 已是公会管理员!");
							return;
						}
						if (!Clans3.clans[num].members.Contains(userAccountByName6.ID))
						{
							args.Player.SendErrorMessage(userAccountByName6.Name + " 不是你的公会成员!");
							return;
						}
						Clans3.clans[num].admins.Add(userAccountByName6.ID);
						Clans3.clans[num].members.Remove(userAccountByName6.ID);
						DB.changeMembers(args.Player.Account.ID, Clans3.clans[num]);
						args.Player.SendSuccessMessage(userAccountByName6.Name + " 现在是公会管理员!");
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							userAccountByName6.Name,
							" 提升为 ",
							Clans3.clans[num].name,
							" 的公会管理员."
						}));
						return;
					}
					else
					{
						if (list13.Count > 1 && list13[0].Name != text)
						{
							args.Player.SendMultipleMatchError(from p in list13
							select p.Name);
							return;
						}
						if (Clans3.clans[num].admins.Contains(list13[0].Account.ID))
						{
							args.Player.SendErrorMessage(list13[0].Name + " 已是公会管理员!");
							return;
						}
						if (!Clans3.clans[num].members.Contains(list13[0].Account.ID))
						{
							args.Player.SendErrorMessage(list13[0].Name + " 不是你的公会成员!");
							return;
						}
						Clans3.clans[num].admins.Add(list13[0].Account.ID);
						Clans3.clans[num].members.Remove(list13[0].Account.ID);
						DB.changeMembers(args.Player.Account.ID, Clans3.clans[num]);
						args.Player.SendSuccessMessage(list13[0].Name + " 现在是公会管理员!");
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							list13[0].Account.Name,
							" 提升为 ",
							Clans3.clans[num].name,
							" 的公会管理员."
						}));
						list13[0].SendInfoMessage(string.Concat(new string[]
						{
							"你被提升为 ",
							Clans3.clans[num].name,
							" 的公会管理员,处理人: ",
							args.Player.Name,
							"."
						}));
						return;
					}
				}
				else
				{
					if (!(a == "demote"))
					{
						args.Player.PluginErrorMessage("错误的指令. 输入 /clan help 查看帮助.");
						return;
					}
					if (num == -1)
					{
						args.Player.SendErrorMessage("你不在工会里!");
						return;
					}
					if (Clans3.clans[num].owner != args.Player.Account.ID)
					{
						args.Player.SendErrorMessage("你不能降级公会成员!");
						return;
					}
					List<TSPlayer> list14 = TSPlayer.FindByNameOrID(text);
					if (list14.Count == 0)
					{
						UserAccount userAccountByName7 = TShock.UserAccounts.GetUserAccountByName(text);
						if (userAccountByName7 == null)
						{
							args.Player.SendErrorMessage("没有找到该用户 " + text);
							return;
						}
						if (!Clans3.clans[num].admins.Contains(userAccountByName7.ID))
						{
							args.Player.SendErrorMessage(userAccountByName7.Name + " 不是公会管理员!");
							return;
						}
						Clans3.clans[num].admins.Remove(userAccountByName7.ID);
						Clans3.clans[num].members.Add(userAccountByName7.ID);
						DB.changeMembers(args.Player.Account.ID, Clans3.clans[num]);
						args.Player.SendSuccessMessage(userAccountByName7.Name + " 不是公会管理员!");
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 移除了 ",
							userAccountByName7.Name,
							" 的 ",
							Clans3.clans[num].name,
							" 管理员权限."
						}));
						return;
					}
					else
					{
						if (list14.Count > 1 && list14[0].Name != text)
						{
							args.Player.SendMultipleMatchError(from p in list14
							select p.Name);
							return;
						}
						if (!Clans3.clans[num].admins.Contains(list14[0].Account.ID))
						{
							args.Player.SendErrorMessage(list14[0].Name + " 不是公会管理员!");
							return;
						}
						Clans3.clans[num].admins.Remove(list14[0].Account.ID);
						Clans3.clans[num].members.Add(list14[0].Account.ID);
						DB.changeMembers(args.Player.Account.ID, Clans3.clans[num]);
						args.Player.SendSuccessMessage(list14[0].Name + " 已不再是公会管理员!");
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 移除了 ",
							list14[0].Account.Name,
							" 的 ",
							Clans3.clans[num].name,
							" 管理员权限."
						}));
						list14[0].SendInfoMessage(string.Format("你的 {0} 公会管理员被 {1} 移除.", Clans3.clans[num], args.Player.Name));
						return;
					}
				}
			}
			else
			{
				args.Player.PluginErrorMessage("错误的指令. 输入 /clan help 查看帮助.");
			}
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000054CC File Offset: 0x000036CC
		private void ClansStaff(CommandArgs args)
		{
			if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() == "help")
			{
				int num;
				if (PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out num))
				{
					List<string> list = new List<string>();
					list.Add("members <公会名> [页数 #] - 列出公会所有成员.");
					list.Add("prefix <公会名> <前缀> - 更改你的公会聊天前缀.");
					list.Add("kick <公会名> <用户名> - 驱逐一个公会成员.");
					list.Add("ban <公会名> <用户名> - 封禁一个公会成员让他再也无法加入你的公会.");
					list.Add("unban <公会名> <用户名> - 解封一个公会成员的封禁状态.");
					if (args.Player.Group.HasPermission("clans.admin"))
					{
						list.Add("delete <公会名> - 解散一个公会.");
					}
					PaginationTools.SendPage(args.Player, num, list, new PaginationTools.Settings
					{
						HeaderFormat = "Clan公会命令 ({0}/{1}):",
						FooterFormat = StringExt.SFormat("输入 {0}cs help {{0}} 查看更多.", new object[]
						{
							args.Silent ? TShock.Config.Settings.CommandSilentSpecifier : TShock.Config.Settings.CommandSpecifier
						})
					});
					return;
				}
			}
			else if (args.Parameters.Count > 1)
			{
				string a = args.Parameters[0].ToLower();
				string text = args.Parameters[1];
				if (a == "members")
				{
					List<int> list2 = this.findClanByName(text);
					if (list2.Count == 0)
					{
						args.Player.SendErrorMessage("没有找到该公会 \"" + text + "\".");
						return;
					}
					if (list2.Count > 1)
					{
						List<string> list3 = new List<string>();
						foreach (int index in list2)
						{
							list3.Add(Clans3.clans[index].name);
						}
						args.Player.SendMultipleMatchError(list3);
						return;
					}
					int index2 = list2[0];
					int num2;
					if (PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out num2))
					{
						List<string> list4 = new List<string>();
						list4.Add(TShock.UserAccounts.GetUserAccountByID(Clans3.clans[index2].owner).Name + " (创建者)");
						foreach (int num3 in Clans3.clans[index2].admins)
						{
							list4.Add(TShock.UserAccounts.GetUserAccountByID(num3).Name + " (管理员)");
						}
						foreach (int num4 in Clans3.clans[index2].members)
						{
							list4.Add(TShock.UserAccounts.GetUserAccountByID(num4).Name);
						}
						foreach (int num5 in Clans3.clans[index2].invited)
						{
							list4.Add(TShock.UserAccounts.GetUserAccountByID(num5).Name + " (已邀请)");
						}
						PaginationTools.SendPage(args.Player, num2, list4, new PaginationTools.Settings
						{
							HeaderFormat = Clans3.clans[index2].name + " 公会成员 ({0}/{1}):",
							FooterFormat = StringExt.SFormat("输入 {0}cs members {1} {{0}} 查看更多.", new object[]
							{
								args.Silent ? TShock.Config.Settings.CommandSilentSpecifier : TShock.Config.Settings.CommandSpecifier,
								Clans3.clans[index2].name
							})
						});
						return;
					}
				}
				else if (a == "kick" && args.Parameters.Count == 3)
				{
					List<int> list5 = this.findClanByName(text);
					if (list5.Count == 0)
					{
						args.Player.SendErrorMessage("没有找到该公会 \"" + text + "\".");
						return;
					}
					if (list5.Count > 1)
					{
						List<string> list6 = new List<string>();
						foreach (int index3 in list5)
						{
							list6.Add(Clans3.clans[index3].name);
						}
						args.Player.SendMultipleMatchError(list6);
						return;
					}
					string text2 = args.Parameters[2];
					UserAccount userAccountByName = TShock.UserAccounts.GetUserAccountByName(text2);
					if (userAccountByName == null)
					{
						args.Player.SendErrorMessage("没有找到该用户 \"{name}\"");
						return;
					}
					int index4 = list5[0];
					if (userAccountByName.ID == Clans3.clans[index4].owner)
					{
						args.Player.SendErrorMessage("你不能驱逐一个公会管理员!");
						return;
					}
					if (Clans3.clans[index4].admins.Contains(userAccountByName.ID))
					{
						Clans3.clans[index4].admins.Remove(userAccountByName.ID);
						DB.changeMembers(Clans3.clans[index4].owner, Clans3.clans[index4]);
						args.Player.SendSuccessMessage(string.Concat(new string[]
						{
							"成功将 ",
							userAccountByName.Name,
							" 从 ",
							Clans3.clans[index4].name,
							" 公会里驱逐."
						}));
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							userAccountByName.Name,
							" 从 ",
							Clans3.clans[index4].name,
							" 公会里驱逐."
						}));
						return;
					}
					if (Clans3.clans[index4].members.Contains(userAccountByName.ID))
					{
						Clans3.clans[index4].members.Remove(userAccountByName.ID);
						DB.changeMembers(Clans3.clans[index4].owner, Clans3.clans[index4]);
						args.Player.SendSuccessMessage(string.Concat(new string[]
						{
							"成功将 ",
							userAccountByName.Name,
							" 从 ",
							Clans3.clans[index4].name,
							" 公会里驱逐."
						}));
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							userAccountByName.Name,
							" 从 ",
							Clans3.clans[index4].name,
							" 公会里驱逐."
						}));
						return;
					}
					args.Player.SendErrorMessage(userAccountByName.Name + " 没有在 " + Clans3.clans[index4].name + " 公会里!");
					return;
				}
				else if (a == "ban" && args.Parameters.Count == 3)
				{
					List<int> list7 = this.findClanByName(text);
					if (list7.Count == 0)
					{
						args.Player.SendErrorMessage("没有找到该公会 \"" + text + "\".");
						return;
					}
					if (list7.Count > 1)
					{
						List<string> list8 = new List<string>();
						foreach (int index5 in list7)
						{
							list8.Add(Clans3.clans[index5].name);
						}
						args.Player.SendMultipleMatchError(list8);
						return;
					}
					string text3 = args.Parameters[2];
					UserAccount userAccountByName2 = TShock.UserAccounts.GetUserAccountByName(text3);
					if (userAccountByName2 == null)
					{
						args.Player.SendErrorMessage("没有找到该用户 \"{name}\"");
						return;
					}
					int index6 = list7[0];
					if (userAccountByName2.ID == Clans3.clans[index6].owner)
					{
						args.Player.SendErrorMessage("你不能封禁管理员!");
						return;
					}
					if (Clans3.clans[index6].admins.Contains(userAccountByName2.ID))
					{
						Clans3.clans[index6].admins.Remove(userAccountByName2.ID);
						Clans3.clans[index6].banned.Add(userAccountByName2.ID);
						DB.changeMembers(Clans3.clans[index6].owner, Clans3.clans[index6]);
						DB.changeBanned(Clans3.clans[index6].owner, Clans3.clans[index6].banned);
						args.Player.SendSuccessMessage(string.Concat(new string[]
						{
							"成功将 ",
							userAccountByName2.Name,
							" 添加到 ",
							Clans3.clans[index6].name,
							" 公会封禁列表."
						}));
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							userAccountByName2.Name,
							" 添加到 ",
							Clans3.clans[index6].name,
							" 公会封禁列表."
						}));
						return;
					}
					if (Clans3.clans[index6].members.Contains(userAccountByName2.ID))
					{
						Clans3.clans[index6].members.Remove(userAccountByName2.ID);
						Clans3.clans[index6].banned.Add(userAccountByName2.ID);
						DB.changeMembers(Clans3.clans[index6].owner, Clans3.clans[index6]);
						DB.changeBanned(Clans3.clans[index6].owner, Clans3.clans[index6].banned);
						args.Player.SendSuccessMessage(string.Concat(new string[]
						{
							"成功将 ",
							userAccountByName2.Name,
							" 添加到 ",
							Clans3.clans[index6].name,
							" 公会封禁列表."
						}));
						TShock.Log.Info(string.Concat(new string[]
						{
							args.Player.Account.Name,
							" 将 ",
							userAccountByName2.Name,
							" 添加到 ",
							Clans3.clans[index6].name,
							" 公会封禁列表."
						}));
						return;
					}
					args.Player.SendErrorMessage(userAccountByName2.Name + " 没有在 " + Clans3.clans[index6].name + " 公会里!");
					return;
				}
				else if (a == "unban" && args.Parameters.Count == 3)
				{
					List<int> list9 = this.findClanByName(text);
					if (list9.Count == 0)
					{
						args.Player.SendErrorMessage("没有找到该公会 \"" + text + "\".");
						return;
					}
					if (list9.Count > 1)
					{
						List<string> list10 = new List<string>();
						foreach (int index7 in list9)
						{
							list10.Add(Clans3.clans[index7].name);
						}
						args.Player.SendMultipleMatchError(list10);
						return;
					}
					string text4 = args.Parameters[2];
					UserAccount userAccountByName3 = TShock.UserAccounts.GetUserAccountByName(text4);
					if (userAccountByName3 == null)
					{
						args.Player.SendErrorMessage("没有找到该用户 \"{name}\"");
						return;
					}
					int index8 = list9[0];
					if (!Clans3.clans[index8].banned.Contains(userAccountByName3.ID))
					{
						args.Player.SendErrorMessage(text4 + " 没有被添加到 " + Clans3.clans[index8].name + " 公会封禁列表!");
						return;
					}
					Clans3.clans[index8].banned.Remove(userAccountByName3.ID);
					Clans3.clans[index8].members.Remove(userAccountByName3.ID);
					DB.changeMembers(Clans3.clans[index8].owner, Clans3.clans[index8]);
					DB.changeBanned(Clans3.clans[index8].owner, Clans3.clans[index8].banned);
					args.Player.SendSuccessMessage(string.Concat(new string[]
					{
						"成功将 ",
						userAccountByName3.Name,
						" 移出 ",
						Clans3.clans[index8].name,
						" 公会封禁列表."
					}));
					TShock.Log.Info(string.Concat(new string[]
					{
						args.Player.Account.Name,
						" 将 ",
						userAccountByName3.Name,
						" 移出 ",
						Clans3.clans[index8].name,
						" 公会封禁列表."
					}));
					return;
				}
				else if (a == "prefix" && args.Parameters.Count == 3)
				{
					List<int> list11 = this.findClanByName(text);
					if (list11.Count == 0)
					{
						args.Player.SendErrorMessage("没有找到该公会 \"" + text + "\".");
						return;
					}
					if (list11.Count > 1)
					{
						List<string> list12 = new List<string>();
						foreach (int index9 in list11)
						{
							list12.Add(Clans3.clans[index9].name);
						}
						args.Player.SendMultipleMatchError(list12);
						return;
					}
					string text5 = args.Parameters[2];
					if (text5.ToLower().Contains("[i") || text5.ToLower().Contains("[c") || text5.ToLower().Contains("[g"))
					{
						args.Player.SendErrorMessage("你不能使用物品和颜色标签!");
						return;
					}
					if (text5.Length > 20)
					{
						args.Player.SendErrorMessage("前缀太长!");
						return;
					}
					Clans3.clans[list11[0]].prefix = text5;
					DB.clanPrefix(Clans3.clans[list11[0]].owner, text5);
					args.Player.SendSuccessMessage(string.Concat(new string[]
					{
						"成功将公会前缀从 ",
						Clans3.clans[list11[0]].name,
						" 变为 \"",
						text5,
						"\"."
					}));
					TShock.Log.Info(string.Concat(new string[]
					{
						args.Player.Account.Name,
						" 将公会前缀 ",
						Clans3.clans[list11[0]].name,
						" 变更为 \"",
						text5,
						"\"."
					}));
					TSPlayer[] players = TShock.Players;
					foreach (TSPlayer tsplayer in players)
					{
						if (tsplayer != null && tsplayer.IsLoggedIn && this.findClan(tsplayer.Account.ID) == list11[0])
						{
							tsplayer.SetData<string>("clan", Clans3.clans[list11[0]].prefix);
						}
					}
					return;
				}
				else
				{
					if (!(a == "delete") || !args.Player.Group.HasPermission("clans.admin"))
					{
						args.Player.PluginErrorMessage("错误的命令. 输入 /cs help 查看更多.");
						return;
					}
					List<int> list13 = this.findClanByName(text);
					if (list13.Count == 0)
					{
						args.Player.SendErrorMessage("没有找到该公会 \"" + text + "\".");
						return;
					}
					if (list13.Count > 1)
					{
						List<string> list14 = new List<string>();
						foreach (int index10 in list13)
						{
							list14.Add(Clans3.clans[index10].name);
						}
						args.Player.SendMultipleMatchError(list14);
						return;
					}
					args.Player.SendSuccessMessage("成功解散 " + Clans3.clans[list13[0]].name + " 公会.");
					TShock.Log.Info(args.Player.Account.Name + " 解散了 " + Clans3.clans[list13[0]].name + " 公会.");
					TSPlayer[] players2 = TShock.Players;
					foreach (TSPlayer tsplayer2 in players2)
					{
						if (tsplayer2 != null && tsplayer2.IsLoggedIn && this.findClan(tsplayer2.Account.ID) == list13[0])
						{
							tsplayer2.RemoveData("clan");
						}
					}
					DB.removeClan(Clans3.clans[list13[0]].owner);
					Clans3.clans.Remove(Clans3.clans[list13[0]]);
					return;
				}
			}
			else
			{
				args.Player.PluginErrorMessage("错误的命令. 输入 /cs help 查看更多.");
			}
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00006748 File Offset: 0x00004948
		private void CChat(CommandArgs args)
		{
			int num = this.findClan(args.Player.Account.ID);
			if (num == -1)
			{
				args.Player.SendErrorMessage("你不在公会里!");
				return;
			}
			if (args.Player.mute)
			{
				args.Player.SendErrorMessage("你被禁言.");
				return;
			}
			TSPlayer[] players = TShock.Players;
			foreach (TSPlayer tsplayer in players)
			{
				if (tsplayer != null && tsplayer.Active && tsplayer.IsLoggedIn && this.findClan(tsplayer.Account.ID) == num)
				{
					tsplayer.SendMessage(string.Format("(公会消息) [{0}]: {1}", args.Player.Name, string.Join(" ", args.Parameters)), Color.ForestGreen);
				}
			}
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00006818 File Offset: 0x00004A18
		private void Ignore(CommandArgs args)
		{
			int id = args.Player.Account.ID;
			if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "-a")
			{
				if (!Clans3.ignores.ContainsKey(id))
				{
					Clans3.ignores.Add(args.Player.Account.ID, new List<int>
					{
						-2
					});
					args.Player.SendSuccessMessage("你正忽略所有用户.");
					return;
				}
				if (Clans3.ignores.ContainsKey(id) && !Clans3.ignores[id].Contains(-2))
				{
					Clans3.ignores[id].Add(-2);
					args.Player.SendSuccessMessage("你正忽略所有用户.");
					return;
				}
				Clans3.ignores[id].Remove(-2);
				args.Player.SendSuccessMessage("已切换到正常聊天状态.");
				return;
			}
			else if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "-r")
			{
				if (!Clans3.ignores.ContainsKey(id))
				{
					Clans3.ignores.Add(args.Player.Account.ID, new List<int>
					{
						-1
					});
					args.Player.SendSuccessMessage("你正忽略未注册用户.");
					return;
				}
				if (Clans3.ignores.ContainsKey(id) && !Clans3.ignores[id].Contains(-1))
				{
					Clans3.ignores[id].Add(-1);
					args.Player.SendSuccessMessage("你正忽略未注册用户.");
					return;
				}
				Clans3.ignores[id].Remove(-1);
				args.Player.SendSuccessMessage("已切换到正常聊天状态.");
				return;
			}
			else if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "-s")
			{
				if (args.Player.ContainsData("slackignore"))
				{
					args.Player.RemoveData("slackignore");
					args.Player.SendSuccessMessage("已切换到正常聊天状态.");
					return;
				}
				args.Player.SetData<bool>("slackignore", true);
				args.Player.SendSuccessMessage("你正忽略聊天.");
				return;
			}
			else
			{
				if (args.Parameters.Count != 1)
				{
					args.Player.PluginErrorMessage("错误的指令:");
					args.Player.SendErrorMessage("/ignore <-r/-s/-a/用户名>");
					args.Player.SendErrorMessage("'-r' 忽略未注册用户; '-s' 忽略用户的聊天; '-a' 忽略所有聊天");
					return;
				}
				string text = args.Parameters[0];
				List<TSPlayer> list = TSPlayer.FindByNameOrID(text);
				if (list.Count == 0)
				{
					args.Player.SendErrorMessage("没有找到该用户 " + text);
					return;
				}
				if (list.Count > 1)
				{
					args.Player.SendMultipleMatchError(from p in list
					select p.Name);
					return;
				}
				if (!list[0].IsLoggedIn)
				{
					args.Player.SendErrorMessage("你不能忽略单个未注册用户.");
					return;
				}
				int id2 = list[0].Account.ID;
				if (!Clans3.ignores.ContainsKey(id))
				{
					Clans3.ignores.Add(id, new List<int>
					{
						id2
					});
					args.Player.SendSuccessMessage("你正忽略 " + list[0].Name);
					return;
				}
				if (Clans3.ignores[id].Contains(id2))
				{
					Clans3.ignores[id].Remove(id2);
					args.Player.SendSuccessMessage("解除对 " + list[0].Name + "的忽略");
					return;
				}
				Clans3.ignores[id].Add(id2);
				args.Player.SendSuccessMessage("你正忽略 " + list[0].Name);
				return;
			}
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00006C13 File Offset: 0x00004E13
		private void CReload(CommandArgs args)
		{
			DB.loadClans();
			args.Player.SendSuccessMessage("公会数据已重载.");
			TShock.Log.Info(args.Player.Account.Name + " 已重载公会数据库.");
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00006C50 File Offset: 0x00004E50
		private int findClan(int userid)
		{
			if (userid == -1)
			{
				return -1;
			}
			for (int i = 0; i < Clans3.clans.Count; i++)
			{
				if (Clans3.clans[i].owner == userid || Clans3.clans[i].admins.Contains(userid) || Clans3.clans[i].members.Contains(userid))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00006CC0 File Offset: 0x00004EC0
		private List<int> findClanByName(string name)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < Clans3.clans.Count; i++)
			{
				if (Clans3.clans[i].name.Contains(name))
				{
					list.Add(i);
				}
				if (Clans3.clans[i].name == name)
				{
					return new List<int>
					{
						i
					};
				}
			}
			return list;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00006D30 File Offset: 0x00004F30
		private int getInvite(int userid)
		{
			for (int i = 0; i < Clans3.clans.Count; i++)
			{
				if (Clans3.clans[i].invited.Contains(userid))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x04000009 RID: 9
		public static List<Clan> clans;

		// Token: 0x0400000A RID: 10
		public static Dictionary<int, List<int>> ignores;

		// Token: 0x0400000B RID: 11
		public static Timer invitebc;
	}
}
