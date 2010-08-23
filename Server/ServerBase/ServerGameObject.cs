﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace MyGame.Server
{
	class KeyedObjectCollection : KeyedCollection<ObjectID, ServerGameObject>
	{
		public KeyedObjectCollection() : base(null, 10) { }

		protected override ObjectID GetKeyForItem(ServerGameObject item)
		{
			return item.ObjectID;
		}
	}

	/* Abstract game object, without inventory or conventional location. */
	abstract public class BaseGameObject : IIdentifiable
	{
		public ObjectID ObjectID { get; private set; }
		public World World { get; private set; }
		public bool Destructed { get; private set; }

		protected BaseGameObject(World world)
		{
			this.ObjectID = world.GetNewObjectID();
			this.World = world;
			this.World.AddGameObject(this);
			this.World.AddChange(new ObjectCreatedChange(this));
		}

		public virtual void Destruct()
		{
			// XXX or should we give an error?
			if (RealTickEvent != null)
				this.World.TickEvent -= Tick;

			this.Destructed = true;
			this.World.AddChange(new ObjectDestructedChange(this));
			this.World.RemoveGameObject(this);
		}

		event Action RealTickEvent;

		void Tick()
		{
			RealTickEvent();
		}

		public event Action TickEvent
		{
			add
			{
				if (RealTickEvent == null)
					this.World.TickEvent += Tick;

				RealTickEvent += value;
			}

			remove
			{
				RealTickEvent -= value;

				if (RealTickEvent == null)
					this.World.TickEvent -= Tick;
			}
		}

		public abstract BaseGameObjectData Serialize();
		public abstract void SerializeTo(Action<Messages.Message> writer);

		static Dictionary<Type, List<PropertyDefinition>> s_propertyDefinitionMap = new Dictionary<Type, List<PropertyDefinition>>();

		static protected PropertyDefinition RegisterProperty(Type ownerType, PropertyID propertyID, PropertyVisibility visibility, object defaultValue,
			PropertyChangedCallback propertyChangedCallback = null)
		{
			List<PropertyDefinition> propList;

			if (s_propertyDefinitionMap.TryGetValue(ownerType, out propList) == false)
				s_propertyDefinitionMap[ownerType] = new List<PropertyDefinition>();

			Debug.Assert(!s_propertyDefinitionMap[ownerType].Any(p => p.PropertyID == propertyID));

			var prop = new PropertyDefinition(propertyID, visibility, defaultValue, propertyChangedCallback);
			s_propertyDefinitionMap[ownerType].Add(prop);

			return prop;
		}

		Dictionary<PropertyDefinition, object> m_propertyMap = new Dictionary<PropertyDefinition, object>();

		protected void SetValue(PropertyDefinition property, object value)
		{
			object oldValue = null;

			if (property.PropertyChangedCallback != null)
				oldValue = GetValue(property);

			m_propertyMap[property] = value;
			this.World.AddChange(new PropertyChange(this, property, value));

			if (property.PropertyChangedCallback != null)
				property.PropertyChangedCallback(property, this, oldValue, value);
		}

		protected object GetValue(PropertyDefinition property)
		{
			object value;
			if (m_propertyMap.TryGetValue(property, out value))
				return value;
			else
				return property.DefaultValue;
		}

		protected Tuple<PropertyID, object>[] SerializeProperties()
		{
			var setProps = m_propertyMap.
				Select(kvp => new Tuple<PropertyID, object>(kvp.Key.PropertyID, kvp.Value));

			var props = setProps;

			var type = GetType();
			do
			{
				if (!s_propertyDefinitionMap.ContainsKey(type))
					continue;

				var defProps = s_propertyDefinitionMap[type].
					Where(pd => !setProps.Any(pp => pd.PropertyID == pp.Item1)).
					Select(pd => new Tuple<PropertyID, object>(pd.PropertyID, pd.DefaultValue));

				props = props.Concat(defProps);

			} while ((type = type.BaseType) != null);

			return props.ToArray();
		}
	}

	/* Game object that has inventory, location */
	abstract public class ServerGameObject : BaseGameObject
	{
		public ServerGameObject Parent { get; private set; }
		public Environment Environment { get { return this.Parent as Environment; } }
		KeyedObjectCollection m_children;
		public ReadOnlyCollection<ServerGameObject> Inventory { get; private set; }

		public IntPoint3D Location { get; private set; }
		public IntPoint Location2D { get { return new IntPoint(this.Location.X, this.Location.Y); } }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }
		public int Z { get { return this.Location.Z; } }

		internal ServerGameObject(World world)
			: base(world)
		{
			m_children = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyCollection<ServerGameObject>(m_children);
		}

		public override void Destruct()
		{
			this.MoveTo(null);
			base.Destruct();
		}

		static readonly PropertyDefinition NameProperty = RegisterProperty(typeof(ServerGameObject), PropertyID.Name, PropertyVisibility.Public, "");
		public string Name
		{
			get { return (string)GetValue(NameProperty); }
			set { SetValue(NameProperty, value); }
		}

		static readonly PropertyDefinition ColorProperty = RegisterProperty(typeof(ServerGameObject), PropertyID.Color, PropertyVisibility.Public, new GameColor());
		public GameColor Color
		{
			get { return (GameColor)GetValue(ColorProperty); }
			set { SetValue(ColorProperty, value); }
		}

		static readonly PropertyDefinition SymbolIDProperty = RegisterProperty(typeof(ServerGameObject), PropertyID.SymbolID, PropertyVisibility.Public, SymbolID.Undefined);
		public SymbolID SymbolID
		{
			get { return (SymbolID)GetValue(SymbolIDProperty); }
			set { SetValue(SymbolIDProperty, value); }
		}

		static readonly PropertyDefinition MaterialIDProperty = RegisterProperty(typeof(ServerGameObject), PropertyID.MaterialID, PropertyVisibility.Public, MaterialID.Undefined);
		public MaterialID MaterialID
		{
			get { return (MaterialID)GetValue(MaterialIDProperty); }
			set { SetValue(MaterialIDProperty, value); }
		}

		public virtual bool HandleChildAction(ServerGameObject child, GameAction action) { return false; }

		protected virtual bool OkToAddChild(ServerGameObject ob, IntPoint3D dstLoc) { return true; }
		protected virtual bool OkToMoveChild(ServerGameObject ob, Direction dir, IntPoint3D dstLoc) { return true; }

		protected virtual void OnChildAdded(ServerGameObject child) { }
		protected virtual void OnChildRemoved(ServerGameObject child) { }
		protected virtual void OnChildMoved(ServerGameObject child, IntPoint3D srcLoc, IntPoint3D dstLoc) { }

		protected virtual void OnEnvironmentChanged(ServerGameObject oldEnv, ServerGameObject newEnv) { }

		public bool MoveTo(ServerGameObject parent)
		{
			return MoveTo(parent, new IntPoint3D());
		}

		public bool MoveTo(ServerGameObject dst, IntPoint3D dstLoc)
		{
			Debug.Assert(this.World.IsWritable);

			if (dst != null && !dst.OkToAddChild(this, dstLoc))
				return false;

			MoveToLow(dst, dstLoc);

			return true;
		}

		public bool MoveTo(int x, int y, int z)
		{
			var p = new IntPoint3D(x, y, z);
			return MoveTo(this.Environment, p);
		}

		public bool MoveDir(Direction dir)
		{
			Debug.Assert(this.World.IsWritable);

			if (this.Environment == null)
				throw new Exception();

			var dst = this.Environment;
			var dstLoc = this.Location + dir;

			if (!dst.OkToMoveChild(this, dir, dstLoc))
				return false;

			MoveToLow(dst, dstLoc);

			return true;
		}

		void MoveToLow(ServerGameObject dst, IntPoint3D dstLoc)
		{
			var src = this.Parent;
			var srcLoc = this.Location;

			if (src != dst)
			{
				if (src != null)
				{
					src.OnChildRemoved(this);
					src.m_children.Remove(this);
				}

				this.Parent = dst;
			}

			if (this.Location != dstLoc)
			{
				this.Location = dstLoc;
				if (dst != null && src == dst)
					dst.OnChildMoved(this, srcLoc, dstLoc);
			}

			if (src != dst)
			{
				if (dst != null)
				{
					dst.m_children.Add(this);
					dst.OnChildAdded(this);
				}
			}

			if (src != dst)
				OnEnvironmentChanged(src, dst);

			this.World.AddChange(new ObjectMoveChange(this, src, srcLoc, dst, dstLoc));
		}

		public override string ToString()
		{
			return String.Format("ServerGameObject({0})", this.ObjectID);
		}
	}
}
