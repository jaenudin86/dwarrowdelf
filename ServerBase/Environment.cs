﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame.Server
{
	public delegate void MapChanged(Environment map, IntPoint3D l, TileData tileData);

	public class Environment : ServerGameObject
	{
		public event MapChanged MapChanged;

		TileGrid m_tileGrid;
		// XXX this is quite good for add/remove child, but bad for gettings objects at certain location
		KeyedObjectCollection[] m_contentArray;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int Depth { get; private set; }

		public Environment(World world, int width, int height, int depth, VisibilityMode visibilityMode)
			: base(world)
		{
			this.Version = 1;
			base.Name = "unnamed map";
			this.VisibilityMode = visibilityMode;

			this.Width = width;
			this.Height = height;
			this.Depth = depth;

			m_tileGrid = new TileGrid(this.Width, this.Height, this.Depth);
			m_contentArray = new KeyedObjectCollection[this.Depth];
			for (int i = 0; i < depth; ++i)
				m_contentArray[i] = new KeyedObjectCollection();
		}

		public IntRect Bounds2D
		{
			get { return new IntRect(0, 0, this.Width, this.Height); }
		}

		public IntCuboid Bounds
		{
			get { return new IntCuboid(0, 0, 0, this.Width, this.Height, this.Depth); }
		}

		public delegate bool ActionHandlerDelegate(ServerGameObject ob, GameAction action);

		Dictionary<IntPoint3D, ActionHandlerDelegate> m_actionHandlers = new Dictionary<IntPoint3D, ActionHandlerDelegate>();
		public void SetActionHandler(IntPoint3D p, ActionHandlerDelegate handler)
		{
			m_actionHandlers[p] = handler;
		}

		public override bool HandleChildAction(ServerGameObject child, GameAction action)
		{
			ActionHandlerDelegate handler;
			if (m_actionHandlers.TryGetValue(child.Location, out handler) == false)
				return false;

			return handler(child, action);
		}

		public InteriorInfo GetInterior(IntPoint3D p)
		{
			return Interiors.GetInterior(GetInteriorID(p));
		}

		public InteriorID GetInteriorID(IntPoint3D l)
		{
			return m_tileGrid.GetInteriorID(l);
		}

		public MaterialID GetInteriorMaterialID(IntPoint3D l)
		{
			return m_tileGrid.GetInteriorMaterialID(l);
		}

		public FloorInfo GetFloor(IntPoint3D p)
		{
			return Floors.GetFloor(GetFloorID(p));
		}

		public FloorID GetFloorID(IntPoint3D l)
		{
			return m_tileGrid.GetFloorID(l);
		}

		public MaterialID GetFloorMaterialID(IntPoint3D l)
		{
			return m_tileGrid.GetFloorMaterialID(l);
		}

		public void SetInterior(IntPoint3D p, InteriorID interiorID, MaterialID materialID)
		{
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetInteriorID(p, interiorID);
			m_tileGrid.SetInteriorMaterialID(p, materialID);

			var d = m_tileGrid.GetTileData(p);

			if (MapChanged != null)
				MapChanged(this, p, d);
		}

		public void SetFloor(IntPoint3D p, FloorID floorID, MaterialID materialID)
		{
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetFloorID(p, floorID);
			m_tileGrid.SetFloorMaterialID(p, materialID);

			var d = m_tileGrid.GetTileData(p);

			if (MapChanged != null)
				MapChanged(this, p, d);
		}

		public void SetInteriorID(IntPoint3D l, InteriorID interiorID)
		{
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetInteriorID(l, interiorID);

			var d = m_tileGrid.GetTileData(l);

			if (MapChanged != null)
				MapChanged(this, l, d);
		}

		public void SetFloorID(IntPoint3D l, FloorID floorID)
		{
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetFloorID(l, floorID);

			var d = m_tileGrid.GetTileData(l);

			if (MapChanged != null)
				MapChanged(this, l, d);
		}

		public TileData GetTileData(IntPoint3D l)
		{
			return m_tileGrid.GetTileData(l);
		}

		public void SetTileData(IntPoint3D l, TileData data)
		{
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetTileData(l, data);

			var d = m_tileGrid.GetTileData(l);

			if (MapChanged != null)
				MapChanged(this, l, d);
		}

		public bool IsWalkable(IntPoint3D l)
		{
			return !Interiors.GetInterior(GetInteriorID(l)).Blocker;
		}

		// XXX not a good func. contents can be changed by the caller
		public IEnumerable<ServerGameObject> GetContents(IntPoint3D l)
		{
			var list = m_contentArray[l.Z];
			return list.Where(o => o.Location == l);
		}

		protected override void OnChildAdded(ServerGameObject child)
		{
			var list = m_contentArray[child.Z];
			Debug.Assert(!list.Contains(child));
			list.Add(child);
		}

		protected override void OnChildRemoved(ServerGameObject child)
		{
			var list = m_contentArray[child.Z];
			Debug.Assert(list.Contains(child));
			list.Remove(child);
		}

		protected override bool OkToAddChild(ServerGameObject ob, IntPoint3D p)
		{
			Debug.Assert(this.World.IsWritable);

			if (!this.Bounds.Contains(p))
				return false;

			if (!this.IsWalkable(p))
				return false;

			return true;
		}

		public bool CanEnter(IntPoint3D location)
		{
			var dstInter = GetInterior(location);
			var dstFloor = GetFloor(location);

			return !dstInter.Blocker && dstFloor.IsCarrying;
		}

		bool CanMoveTo(IntPoint3D srcLoc, IntPoint3D dstLoc)
		{
			IntVector3D v = dstLoc - srcLoc;

			if (!v.IsNormal)
				throw new Exception();

			var dstInter = GetInterior(dstLoc);
			var dstFloor = GetFloor(dstLoc);

			if (dstInter.Blocker || !dstFloor.IsCarrying)
				return false;

			if (v.Z == 0)
				return true;

			Direction dir = v.ToDirection();

			var srcInter = GetInterior(srcLoc);
			var srcFloor = GetFloor(srcLoc);

			if (dir == Direction.Up)
				return srcInter.ID == InteriorID.Stairs && dstFloor.ID == FloorID.Hole;

			if (dir == Direction.Down)
				return dstInter.ID == InteriorID.Stairs && srcFloor.ID == FloorID.Hole;

			var d2d = v.ToIntVector().ToDirection();

			if (dir.ContainsUp())
			{
				var tileAboveSlope = GetTileData(srcLoc + Direction.Up);
				return d2d.IsCardinal() && srcInter.ID.IsSlope() && srcInter.ID == Interiors.GetSlopeFromDir(d2d) && tileAboveSlope.IsEmpty;
			}

			if (dir.ContainsDown())
			{
				var tileAboveSlope = GetTileData(dstLoc + Direction.Up);
				return d2d.IsCardinal() && dstInter.ID.IsSlope() && dstInter.ID == Interiors.GetSlopeFromDir(d2d.Reverse()) && tileAboveSlope.IsEmpty;
			}

			return false;
		}

		protected override bool OkToMoveChild(ServerGameObject ob, Direction dir, IntPoint3D dstLoc)
		{
			if (!this.Bounds.Contains(dstLoc))
				return false;

			return CanMoveTo(ob.Location, dstLoc);
		}

		protected override void OnChildMoved(ServerGameObject child, IntPoint3D srcLoc, IntPoint3D dstLoc)
		{
			/* just for testing, check if grass is stomped down */
			var tileData = GetTileData(dstLoc);
			if (tileData.InteriorID == InteriorID.Grass)
			{
				if (tileData.InteriorData++ == 5)
				{
					tileData.InteriorID = InteriorID.Empty;
					tileData.InteriorMaterialID = MaterialID.Undefined;
					tileData.InteriorData = 0;
				}
				SetTileData(dstLoc, tileData);
			}

			if (srcLoc.Z == dstLoc.Z)
				return;

			var list = m_contentArray[srcLoc.Z];
			Debug.Assert(list.Contains(child));
			list.Remove(child);

			list = m_contentArray[dstLoc.Z];
			Debug.Assert(!list.Contains(child));
			list.Add(child);
		}


		HashSet<BuildingData> m_buildings = new HashSet<BuildingData>();

		public void AddBuilding(BuildingData building)
		{
			Debug.Assert(this.World.IsWritable);

			Debug.Assert(m_buildings.Any(b => b.Z == building.Z && b.Area.IntersectsWith(building.Area)) == false);
			Debug.Assert(building.Environment == null);
			building.Environment = this;
			m_buildings.Add(building);
		}

		public BuildingData GetBuildingAt(IntPoint3D p)
		{
			return m_buildings.SingleOrDefault(b => b.Contains(p));
		}

		public override void SerializeTo(Action<ClientMsgs.Message> writer)
		{
			writer(new ClientMsgs.MapData()
			{
				Environment = this.ObjectID,
				VisibilityMode = this.VisibilityMode,
				Bounds = this.Bounds,
			});

			List<ClientMsgs.Message> obList = new List<ClientMsgs.Message>();

			var bounds = this.Bounds;

			if (bounds.Volume < 2000)
			{
				// Send everything in one message
				var arr = new TileData[bounds.Volume];
				foreach (var p in this.Bounds.Range())
				{
					int idx = bounds.GetIndex(p);
					arr[idx] = m_tileGrid.GetTileData(p);

					var obs = GetContents(p);
					if (obs != null)
						obList.AddRange(obs.Select(o => o.Serialize()));
				}

				writer(new ClientMsgs.MapDataTerrains()
				{
					Environment = this.ObjectID,
					Bounds = bounds,
					TerrainIDs = arr,
				});
			}
			else if (bounds.Plane.Area < 2000)
			{
				var plane = bounds.Plane;
				// Send every 2D plane in one message
				for (int z = bounds.Z1; z < bounds.Z2; ++z)
				{
					var arr = new TileData[plane.Area];
					foreach (var p2d in plane.Range())
					{
						int idx = plane.GetIndex(p2d);
						var p = new IntPoint3D(p2d, z);
						arr[idx] = m_tileGrid.GetTileData(p);

						var obs = GetContents(p);
						if (obs != null)
							obList.AddRange(obs.Select(o => o.Serialize()));
					}

					writer(new ClientMsgs.MapDataTerrains()
					{
						Environment = this.ObjectID,
						Bounds = new IntCuboid(bounds.X1, bounds.Y1, z, bounds.Width, bounds.Height, 1),
						TerrainIDs = arr,
					});
				}
			}
			else
			{
				// Send every line in one message
				for (int z = bounds.Z1; z < bounds.Z2; ++z)
				{
					for (int y = bounds.Y1; y < bounds.Y2; ++y)
					{
						var arr = new TileData[this.Width];

						for (int x = bounds.X1; x < bounds.X2; ++x)
						{
							IntPoint3D p = new IntPoint3D(x, y, z);
							arr[x] = m_tileGrid.GetTileData(p);

							var obs = GetContents(p);
							if (obs != null)
								obList.AddRange(obs.Select(o => o.Serialize()));
						}

						writer(new ClientMsgs.MapDataTerrains()
						{
							Environment = this.ObjectID,
							Bounds = new IntCuboid(bounds.X1, y, z, bounds.Width, 1, 1),
							TerrainIDs = arr,
						});
					}
				}
			}

			// XXX this probably needs to be divided
			writer(new ClientMsgs.MapDataObjects()
			{
				Environment = this.ObjectID,
				ObjectData = obList,
			});

			// this may not need dividing, perhaps
			writer(new ClientMsgs.MapDataBuildings()
			{
				Environment = this.ObjectID,
				BuildingData = m_buildings.Select(b => (ClientMsgs.BuildingData)b.Serialize()).ToArray(),
			});
		}

		public override ClientMsgs.Message Serialize()
		{
			var msgs = new List<ClientMsgs.Message>();
			SerializeTo(m => msgs.Add(m));
			return new ClientMsgs.CompoundMessage() { Messages = msgs.ToArray() };
		}

		public override string ToString()
		{
			return String.Format("Environment({0})", this.ObjectID);
		}


		class TileGrid : Grid3DBase<TileData>
		{
			public TileGrid(int width, int height, int depth)
				: base(width, height, depth)
			{
			}

			public TileData GetTileData(IntPoint3D p)
			{
				return base.Grid[GetIndex(p)];
			}

			public void SetTileData(IntPoint3D p, TileData data)
			{
				base.Grid[GetIndex(p)] = data;
			}

			public void SetInteriorID(IntPoint3D p, InteriorID id)
			{
				base.Grid[GetIndex(p)].InteriorID = id;
			}

			public InteriorID GetInteriorID(IntPoint3D p)
			{
				return base.Grid[GetIndex(p)].InteriorID;
			}

			public void SetFloorID(IntPoint3D p, FloorID id)
			{
				base.Grid[GetIndex(p)].FloorID = id;
			}

			public FloorID GetFloorID(IntPoint3D p)
			{
				return base.Grid[GetIndex(p)].FloorID;
			}


			public void SetInteriorMaterialID(IntPoint3D p, MaterialID id)
			{
				base.Grid[GetIndex(p)].InteriorMaterialID = id;
			}

			public MaterialID GetInteriorMaterialID(IntPoint3D p)
			{
				return base.Grid[GetIndex(p)].InteriorMaterialID;
			}

			public void SetFloorMaterialID(IntPoint3D p, MaterialID id)
			{
				base.Grid[GetIndex(p)].FloorMaterialID = id;
			}

			public MaterialID GetFloorMaterialID(IntPoint3D p)
			{
				return base.Grid[GetIndex(p)].FloorMaterialID;
			}
		}
	}
}
