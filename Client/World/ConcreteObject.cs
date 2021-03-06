﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Dwarrowdelf.Client
{
	[SaveGameObject(ClientObject = true)]
	public abstract class ConcreteObject : MovableObject, IConcreteObject
	{
		public ConcreteObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			this.SymbolID = Client.SymbolID.Unknown;
		}

		public override void SetProperty(PropertyID propertyID, object value)
		{
			switch (propertyID)
			{
				case PropertyID.Name:
					this.Name = (string)value;
					break;

				case PropertyID.Color:
					this.Color = (GameColor)value;
					break;

				case PropertyID.MaterialID:
					this.MaterialID = (MaterialID)value;
					break;

				default:
					throw new Exception(String.Format("Unknown property {0} in {1}", propertyID, this.GetType().FullName));
			}
		}

		// Contents of type IItemObject
		IEnumerable<IItemObject> IConcreteObject.Inventory { get { return this.Contents.OfType<IItemObject>(); } }
		// Contents of type ItemObject
		public IEnumerable<ItemObject> Inventory { get { return this.Contents.OfType<ItemObject>(); } }

		string m_name;
		public string Name
		{
			get { return m_name; }
			private set { m_name = value; Notify("Name"); }
		}

		GameColor m_color;
		public GameColor Color
		{
			get { return m_color; }
			private set
			{
				if (m_color == value)
					return;

				m_color = value;

				if (this.Environment != null)
					this.Environment.OnObjectVisualChanged(this);

				Notify("Color");
				Notify("EffectiveColor");
			}
		}

		public GameColor EffectiveColor
		{
			get { return m_color != GameColor.None ? m_color : this.Material.Color; }
		}

		SymbolID m_symbolID;
		public SymbolID SymbolID
		{
			get { return m_symbolID; }
			protected set
			{
				if (m_symbolID == value)
					return;

				m_symbolID = value;

				if (this.Environment != null)
					this.Environment.OnObjectVisualChanged(this);

				Notify("SymbolID");
			}
		}

		MaterialID m_materialID;
		public MaterialID MaterialID
		{
			get { return m_materialID; }
			private set
			{
				if (m_materialID == value)
					return;

				m_materialID = value;
				Notify("MaterialID");
				Notify("Material");
				Notify("MaterialCategory");

				// If no color set, the material gives the effective color
				if (m_color != GameColor.None)
					Notify("EffectiveColor");
			}
		}

		public MaterialInfo Material { get { return Materials.GetMaterial(this.MaterialID); } }
		public MaterialCategory MaterialCategory { get { return this.Material.Category; } }

		string m_desc;
		public string Description
		{
			get { return m_desc; }
			protected set
			{
				m_desc = value;
				Notify("Description");
			}
		}

		public override string ToString()
		{
			return String.Format("Object({0}/{1})", this.Name, this.ObjectID);
		}
	}
}
