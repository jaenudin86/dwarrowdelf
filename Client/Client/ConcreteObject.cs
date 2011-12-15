﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Dwarrowdelf.Client
{
	[SaveGameObjectByRef(ClientObject = true)]
	abstract class ConcreteObject : MovableObject, IConcreteObject
	{
		static ConcreteObject()
		{
			GameData.Data.SymbolDrawingCache.DrawingsChanged += OnSymbolDrawingCacheChanged;
		}

		static void OnSymbolDrawingCacheChanged()
		{
			foreach (var ob in GameData.Data.World.Objects.OfType<ConcreteObject>())
				ob.ReloadDrawing();
		}

		public ConcreteObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			this.SymbolID = Client.SymbolID.Unknown;
		}

		public override void Deserialize(BaseGameObjectData _data)
		{
			var data = (LocatableGameObjectData)_data;

			base.Deserialize(_data);

			ContainerObject env = null;
			if (data.Environment != ObjectID.NullObjectID)
				env = this.World.FindObject<ContainerObject>(data.Environment);

			MoveTo(env, data.Location);
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
				m_color = value;

				if (this.Environment != null)
					this.Environment.OnObjectVisualChanged(this);

				Notify("Color");
			}
		}

		SymbolID m_symbolID;
		public SymbolID SymbolID
		{
			get { return m_symbolID; }
			protected set
			{
				m_symbolID = value;

				if (this.Environment != null)
					this.Environment.OnObjectVisualChanged(this);

				Notify("SymbolID");
			}
		}

		void ReloadDrawing()
		{
			if (this.Environment != null)
				this.Environment.OnObjectVisualChanged(this);

			Notify("SymbolID");
		}

		MaterialID m_materialID;
		public MaterialID MaterialID
		{
			get { return m_materialID; }
			private set
			{
				m_materialID = value;
				m_materialInfo = Materials.GetMaterial(this.MaterialID);
				Notify("MaterialID");
				Notify("Material");
			}
		}

		MaterialInfo m_materialInfo;
		public MaterialInfo Material
		{
			get { return m_materialInfo; }
		}

		public MaterialCategory MaterialCategory { get { return m_materialInfo.Category; } } // XXX

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