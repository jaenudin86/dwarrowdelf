﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	public enum ActionState
	{
		/// <summary>
		/// None
		/// </summary>
		None,

		/// <summary>
		/// Action progressing ok
		/// </summary>
		Ok,

		/// <summary>
		/// Action failed
		/// </summary>
		Fail,

		/// <summary>
		/// Action aborted by somebody
		/// </summary>
		Abort,

		/// <summary>
		/// Action done successfully
		/// </summary>
		Done,
	}

	public enum ActionPriority
	{
		Undefined = 0,
		Idle,
		User,
		High,
	}

	// XXX ActionGUID could be much shorter. byte is enough for playerID and actionID
	[Serializable]
	public struct ActionGUID : IEquatable<ActionGUID>
	{
		public int PlayerID;
		public int ActionID;

		public ActionGUID(int playerID, int actionID)
		{
			this.PlayerID = playerID;
			this.ActionID = actionID;
		}

		public bool IsNull { get { return this.PlayerID == 0 && this.ActionID == 0; } }

		public override bool Equals(object obj)
		{
			if (obj is ActionGUID)
				return this.Equals((ActionGUID)obj);
			return false;
		}

		public bool Equals(ActionGUID other)
		{
			return this.PlayerID == other.PlayerID && this.ActionID == other.ActionID;
		}

		public override int GetHashCode()
		{
			return this.PlayerID ^ this.ActionID;
		}

		public static bool operator ==(ActionGUID lhs, ActionGUID rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(ActionGUID lhs, ActionGUID rhs)
		{
			return !(lhs.Equals(rhs));
		}
	}

	[Serializable]
	[SaveGameObject]
	public abstract class GameAction
	{
		[SaveGameProperty]
		public ActionGUID GUID { get; set; }

		protected GameAction()
		{
		}

		protected GameAction(SaveGameContext ctx)
		{
		}

		public sealed override string ToString()
		{
			return String.Format("{0} ({1})", GetType().Name, this.GetParams());
		}

		protected abstract string GetParams();
	}

	[Serializable]
	[SaveGameObject]
	public sealed class MoveAction : GameAction
	{
		[SaveGameProperty]
		public Direction Direction { get; private set; }

		public MoveAction(Direction direction)
		{
			this.Direction = direction;
		}

		MoveAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class HaulAction : GameAction
	{
		[SaveGameProperty]
		public Direction Direction { get; private set; }

		[SaveGameProperty]
		public ObjectID ItemID { get; private set; }

		public HaulAction(Direction direction, IItemObject item)
		{
			this.Direction = direction;
			this.ItemID = item.ObjectID;
		}

		HaulAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Format("{0}, {1}", this.Direction.ToString(), this.ItemID.ToString());
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class WaitAction : GameAction
	{
		[SaveGameProperty]
		public int WaitTicks { get; private set; }

		public WaitAction(int ticks)
		{
			this.WaitTicks = ticks;
		}

		WaitAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.WaitTicks.ToString();
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class SleepAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID Bed { get; private set; }

		public SleepAction(IItemObject bed)
		{
			this.Bed = bed.ObjectID;
		}

		SleepAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Format("bed: {0}", this.Bed);
		}
	}

	public enum MineActionType
	{
		None,
		/// <summary>
		/// Mine can be done for planar directions, and the roof of the mined tile stays,
		/// or up, if the current tile has stairs
		/// </summary>
		Mine,
		/// <summary>
		/// Stairs can be created for planar directions or down. To create stairs up, we need stairs already in the current tile.
		/// </summary>
		Stairs,
	}

	[Serializable]
	[SaveGameObject]
	public sealed class MineAction : GameAction
	{
		[SaveGameProperty]
		public Direction Direction { get; private set; }
		[SaveGameProperty]
		public MineActionType MineActionType { get; private set; }

		public MineAction(Direction dir, MineActionType mineActionType)
		{
			this.Direction = dir;
			this.MineActionType = mineActionType;
		}

		MineAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class FellTreeAction : GameAction
	{
		[SaveGameProperty]
		public Direction Direction { get; private set; }

		public FellTreeAction(Direction dir)
		{
			this.Direction = dir;
		}

		FellTreeAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}


	public enum ConstructMode
	{
		None = 0,
		Wall,
		Floor,
		Pavement,
	}

	[Serializable]
	[SaveGameObject]
	public sealed class ConstructAction : GameAction
	{
		public ConstructMode Mode { get; private set; }
		public IntVector3 Location { get; private set; }
		public ObjectID[] ItemObjectIDs { get; private set; }

		public ConstructAction(ConstructMode mode, IntVector3 location, IEnumerable<IItemObject> items)
		{
			this.Mode = mode;
			this.Location = location;
			this.ItemObjectIDs = items.Select(i => i.ObjectID).ToArray();
		}

		ConstructAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Format("{0}", this.Mode);
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class BuildItemAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID WorkbenchID { get; private set; }
		[SaveGameProperty]
		public ObjectID[] SourceObjectIDs { get; private set; }
		[SaveGameProperty]
		public string BuildableItemKey { get; private set; }

		public BuildItemAction(IItemObject workbench, string buildableItemKey, IEnumerable<IMovableObject> sourceItems)
		{
			this.WorkbenchID = workbench.ObjectID;
			this.SourceObjectIDs = sourceItems.Select(i => i.ObjectID).ToArray();
			this.BuildableItemKey = buildableItemKey;
		}

		BuildItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.SourceObjectIDs.Select(i => i.ToString()).ToArray());
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class AttackAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID Target { get; private set; }

		public AttackAction(ILivingObject target)
		{
			this.Target = target.ObjectID;
		}

		AttackAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Target.ToString();
		}
	}

	[Serializable]
	[SaveGameObject]
	public abstract class ItemAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID ItemID { get; private set; }

		protected ItemAction(IItemObject item)
		{
			this.ItemID = item.ObjectID;
		}

		protected ItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.ItemID.ToString();
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class DropItemAction : ItemAction
	{
		public DropItemAction(IItemObject item)
			: base(item)
		{
		}

		DropItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class GetItemAction : ItemAction
	{
		public GetItemAction(IItemObject item)
			: base(item)
		{
		}

		GetItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class CarryItemAction : ItemAction
	{
		public CarryItemAction(IItemObject item)
			: base(item)
		{
		}

		CarryItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class ConsumeAction : ItemAction
	{
		public ConsumeAction(IItemObject consumable)
			: base(consumable)
		{
		}

		ConsumeAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	public enum InstallMode
	{
		None = 0,
		Install,
		Uninstall,
	}

	[Serializable]
	[SaveGameObject]
	public sealed class InstallItemAction : ItemAction
	{
		[SaveGameProperty]
		public InstallMode Mode { get; private set; }

		public InstallItemAction(IItemObject item, InstallMode mode)
			: base(item)
		{
			this.Mode = mode;
		}

		InstallItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class EquipItemAction : ItemAction
	{
		public EquipItemAction(IItemObject item)
			: base(item)
		{
		}

		EquipItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObject]
	public sealed class UnequipItemAction : ItemAction
	{
		public UnequipItemAction(IItemObject item)
			: base(item)
		{
		}

		UnequipItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}
}
