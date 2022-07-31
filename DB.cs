using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace Clans3
{
	// Token: 0x02000004 RID: 4
	public static class DB
	{
		// Token: 0x06000016 RID: 22 RVA: 0x00006D70 File Offset: 0x00004F70
		public static void DBConnect()
		{
			string a = TShock.Config.Settings.StorageType.ToLower();
			if (!(a == "mysql"))
			{
				if (a == "sqlite")
				{
					string str = Path.Combine(TShock.SavePath, "Clans.sqlite");
					DB.db = new SqliteConnection("uri=file://" + str + ",Version=3");
				}
			}
			else
			{
				string[] array = TShock.Config.Settings.MySqlHost.Split(new char[]
				{
					':'
				});
				DB.db = new MySqlConnection
				{
					ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};", new object[]
					{
						array[0],
						(array.Length == 1) ? "3306" : array[1],
						TShock.Config.Settings.MySqlDbName,
						TShock.Config.Settings.MySqlUsername,
						TShock.Config.Settings.MySqlPassword
					})
				};
			}
			IDbConnection dbConnection = DB.db;
			IQueryBuilder queryBuilder2;
			if (DbExt.GetSqlType(DB.db) != (SqlType)1)
			{
				IQueryBuilder queryBuilder = new MysqlQueryCreator();
				queryBuilder2 = queryBuilder;
			}
			else
			{
				IQueryBuilder queryBuilder3 = new SqliteQueryCreator();
				queryBuilder2 = queryBuilder3;
			}
			SqlTableCreator sqlTableCreator = new SqlTableCreator(dbConnection, queryBuilder2);
			sqlTableCreator.EnsureTableStructure(new SqlTable("Clans", new SqlColumn[]
			{
				new SqlColumn("owner", (MySqlDbType)3)
				{
					Primary = true,
					Unique = true,
					Length = new int?(7)
				},
				new SqlColumn("name", (MySqlDbType)752)
				{
					Length = new int?(30)
				},
				new SqlColumn("admins", (MySqlDbType)752)
				{
					Length = new int?(100)
				},
				new SqlColumn("members", (MySqlDbType)752)
				{
					Length = new int?(100)
				},
				new SqlColumn("prefix", (MySqlDbType)752)
				{
					Length = new int?(30)
				},
				new SqlColumn("banned", (MySqlDbType)752)
				{
					Length = new int?(100)
				},
				new SqlColumn("priv", (MySqlDbType)3)
				{
					Length = new int?(1)
				}
			}));
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00006F84 File Offset: 0x00005184
		public static void loadClans()
		{
			Clans3.clans.Clear();
			using (QueryResult queryResult = DbExt.QueryReader(DB.db, "SELECT * FROM Clans", new object[0]))
			{
				while (queryResult.Read())
				{
					string text = queryResult.Get<string>("admins");
					List<int> list = new List<int>();
					if (text != "")
					{
						text = text.Trim(new char[]
						{
							','
						});
						string[] array = text.Split(new char[]
						{
							','
						});
						string[] array2 = array;
						foreach (string s in array2)
						{
							list.Add(int.Parse(s));
						}
					}
					string text2 = queryResult.Get<string>("members");
					List<int> list2 = new List<int>();
					if (text2 != "")
					{
						text2 = text2.Trim(new char[]
						{
							','
						});
						string[] array4 = text2.Split(new char[]
						{
							','
						});
						string[] array5 = array4;
						foreach (string s2 in array5)
						{
							list2.Add(int.Parse(s2));
						}
					}
					string text3 = queryResult.Get<string>("banned");
					List<int> list3 = new List<int>();
					if (text3 != "")
					{
						text3 = text3.Trim(new char[]
						{
							','
						});
						string[] array7 = text3.Split(new char[]
						{
							','
						});
						string[] array8 = array7;
						foreach (string s3 in array8)
						{
							list3.Add(int.Parse(s3));
						}
					}
					bool cprivate = queryResult.Get<int>("priv") == 1;
					Clans3.clans.Add(new Clan(queryResult.Get<string>("name"), queryResult.Get<int>("owner"))
					{
						admins = list,
						banned = list3,
						members = list2,
						prefix = queryResult.Get<string>("prefix"),
						cprivate = cprivate,
						invited = new List<int>()
					});
				}
			}
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000071C0 File Offset: 0x000053C0
		public static void removeClan(int owner)
		{
			int num = DbExt.Query(DB.db, "DELETE FROM Clans WHERE owner=@0", new object[]
			{
				owner
			});
			if (num != 1)
			{
				TShock.Log.Error(string.Format("Database error: Failed to delete from Clans where owner = {0}.", owner));
			}
		}

		// Token: 0x06000019 RID: 25 RVA: 0x0000720C File Offset: 0x0000540C
		public static void changeOwner(int oldowner, Clan newclan)
		{
			string text = ",";
			string text2 = ",";
			text += string.Join<int>(",", newclan.admins);
			text += ",";
			text2 += string.Join<int>(",", newclan.members);
			text2 += ",";
			if (newclan.admins.Count == 0)
			{
				text = "";
			}
			if (newclan.members.Count == 0)
			{
				text2 = "";
			}
			int num = DbExt.Query(DB.db, "UPDATE Clans SET owner=@0,admins=@1,members=@2 WHERE owner=@3;", new object[]
			{
				newclan.owner,
				text,
				text2,
				oldowner
			});
			if (num != 1)
			{
				TShock.Log.Error(string.Format("Database error: Failed to change owner where oldowner = {0} and newowner = {1}.", oldowner, newclan.owner));
			}
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000072F0 File Offset: 0x000054F0
		public static void changeMembers(int owner, Clan newclan)
		{
			string text = ",";
			string text2 = ",";
			text += string.Join<int>(",", newclan.admins);
			text += ",";
			text2 += string.Join<int>(",", newclan.members);
			text2 += ",";
			if (newclan.admins.Count == 0)
			{
				text = "";
			}
			if (newclan.members.Count == 0)
			{
				text2 = "";
			}
			int num = DbExt.Query(DB.db, "UPDATE Clans SET admins=@0,members=@1 WHERE owner=@2;", new object[]
			{
				text,
				text2,
				owner
			});
			if (num != 1)
			{
				TShock.Log.Error(string.Format("Database error: Failed to update players where owner = {0}.", owner));
			}
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000073BC File Offset: 0x000055BC
		public static void newClan(string name, int owner)
		{
			int num = DbExt.Query(DB.db, "INSERT INTO Clans (owner, name, admins, members, prefix, banned, priv) VALUES (@0, @1, '', '', '', '', @2);", new object[]
			{
				owner,
				name,
				0
			});
			if (num != 1)
			{
				TShock.Log.Error(string.Format("Database error: Failed to create a new clan with owner = {0}.", owner));
			}
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00007414 File Offset: 0x00005614
		public static void clanPrefix(int owner, string newprefix)
		{
			int num = DbExt.Query(DB.db, "UPDATE Clans SET prefix=@0 WHERE owner=@1;", new object[]
			{
				newprefix,
				owner
			});
			if (num != 1)
			{
				TShock.Log.Error(string.Format("Database error: Failed to set new prefix where owner = {0}.", owner));
			}
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00007464 File Offset: 0x00005664
		public static void changeBanned(int owner, List<int> bannedlist)
		{
			string text = ",";
			text += string.Join<int>(",", bannedlist);
			text += ",";
			if (bannedlist.Count == 0)
			{
				text = "";
			}
			int num = DbExt.Query(DB.db, "UPDATE Clans SET banned=@0 WHERE owner=@1;", new object[]
			{
				text,
				owner
			});
			if (num != 1)
			{
				TShock.Log.Error(string.Format("Database error: Failed to update banned list where owner = {0}.", owner));
			}
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000074E4 File Offset: 0x000056E4
		public static void changePrivate(int owner, bool isPrivate)
		{
			int num = isPrivate ? 1 : 0;
			int num2 = DbExt.Query(DB.db, "UPDATE Clans SET priv=@0 WHERE owner=@1;", new object[]
			{
				num,
				owner
			});
			if (num2 != 1)
			{
				TShock.Log.Error(string.Format("Database error: Failed to update private setting where owner = {0}.", owner));
			}
		}

		// Token: 0x0400000C RID: 12
		public static IDbConnection db;
	}
}
