﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Diagnostics;

namespace Dwarrowdelf
{
	[Flags]
	public enum ItemFlags
	{
		None = 0,
		Installable = 1 << 0,
		Container = 1 << 1,
	}

	public enum ItemID
	{
		Undefined = 0,
		Log,
		Food,
		Drink,
		Rock,
		Ore,
		Gem,
		UncutGem,
		Block,
		Bar,
		Corpse,

		Chair,
		Table,
		Door,
		Bed,
		Barrel,
		Bin,

		CarpentersTools,
		MasonsTools,

		SmithsWorkbench,
		CarpentersWorkbench,
		MasonsWorkbench,
		SmelterWorkbench,
		GemcuttersWorkbench,

		Dagger,
		ShortSword,
		BattleAxe,
		Mace,

		ChainMail,
		PlateMail,

		Skullcap,
		Helmet,

		Gloves,
		Gauntlets,

		Boots,
		Sandals,

		Contraption,
	}

	[Serializable]
	public sealed class ItemIDMask : EnumBitMask<ItemID>
	{
		public ItemIDMask() : base() { }
		public ItemIDMask(ItemID itemID) : base(itemID) { }
		public ItemIDMask(IEnumerable<ItemID> itemIDs) : base(itemIDs) { }
	}

	public enum ItemCategory
	{
		Undefined = 0,

		Furniture,
		Food,
		Drink,
		Gem,
		RawMaterial,
		Corpse,
		Weapon,
		Armor,
		Workbench,
		Utility,
		Tools,
		Other,
	}

	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(ItemCategoryMaskConverter))]
	public sealed class ItemCategoryMask : EnumBitMask32<ItemCategory>
	{
		public ItemCategoryMask() : base() { }
		public ItemCategoryMask(IEnumerable<ItemCategory> categories) : base(categories) { }
	}

	public sealed class ItemInfo
	{
		public ItemID ID { get; internal set; }
		public string Name { get; internal set; }
		public ItemCategory Category { get; internal set; }
		public ItemFlags Flags { get; internal set; }
		public WeaponInfo WeaponInfo { get; internal set; }
		public ArmorInfo ArmorInfo { get; internal set; }
		public int Capacity { get; internal set; }

		public bool IsInstallable { get { return (this.Flags & ItemFlags.Installable) != 0; } }
		public bool IsContainer { get { return (this.Flags & ItemFlags.Container) != 0; } }
	}

	public enum WeaponType
	{
		Edged,
		Blunt,
	}

	public sealed class WeaponInfo
	{
		public int WC { get; internal set; }
		public bool IsTwoHanded { get; internal set; }
		public WeaponType WeaponType { get; internal set; }
	}

	public enum ArmorSlot
	{
		Undefined = 0,
		Head,
		Hands,
		Torso,
		Feet,
	}

	/* leather
	 * scale
	 * lamellar
	 * brigandine
	 * mail armor (chain mail)
	 * plate armor (plate mail)
	 */

	public sealed class ArmorInfo
	{
		public int AC { get; internal set; }
		public ArmorSlot Slot { get; internal set; }
	}

	public static class Items
	{
		static ItemInfo[] s_items;

		static Items()
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();

			ItemInfo[] items;

			using (var stream = asm.GetManifestResourceStream("Dwarrowdelf.Game.Items.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = asm,
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					items = (ItemInfo[])System.Xaml.XamlServices.Load(reader);
			}

			var max = items.Max(i => (int)i.ID);
			s_items = new ItemInfo[max + 1];

			foreach (var item in items)
			{
				if (s_items[(int)item.ID] != null)
					throw new Exception();

				if (item.Name == null)
					item.Name = item.ID.ToString().ToLowerInvariant();

				s_items[(int)item.ID] = item;
			}
		}

		public static ItemInfo GetItemInfo(ItemID id)
		{
			Debug.Assert(id != ItemID.Undefined);
			Debug.Assert(s_items[(int)id] != null);

			return s_items[(int)id];
		}

		public static IEnumerable<ItemInfo> GetItemInfos()
		{
			return s_items.Where(ii => ii != null);
		}

		public static IEnumerable<ItemInfo> GetItemInfos(ItemCategory category)
		{
			Debug.Assert(category != ItemCategory.Undefined);

			return s_items.Where(ii => ii != null && ii.Category == category);
		}

		public static IEnumerable<ItemID> GetItemIDs()
		{
			return EnumHelpers.GetEnumValues<ItemID>().Skip(1);
		}

		public static IEnumerable<ItemCategory> GetItemCategories()
		{
			return EnumHelpers.GetEnumValues<ItemCategory>().Skip(1);
		}
	}
}
