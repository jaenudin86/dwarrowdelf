﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server
{
	[SaveGameObjectByRef]
	public sealed class EnvironmentObject : ContainerObject, IEnvironmentObject
	{
		internal static EnvironmentObject Create(World world, EnvironmentObjectBuilder builder)
		{
			var ob = new EnvironmentObject(builder);
			ob.Initialize(world);
			return ob;
		}

		[SaveGameProperty("Grid", ReaderWriter = typeof(TileGridReaderWriter))]
		TileGrid m_tileGrid;

		ArrayGrid2D<byte> m_depthMap;

		// XXX this is quite good for add/remove child, but bad for gettings objects at certain location
		KeyedObjectCollection[] m_contentArray;

		[SaveGameProperty]
		public uint Version { get; private set; }

		[SaveGameProperty]
		public VisibilityMode VisibilityMode { get; private set; }

		[SaveGameProperty]
		public int Width { get; private set; }
		[SaveGameProperty]
		public int Height { get; private set; }
		[SaveGameProperty]
		public int Depth { get; private set; }

		public IntSize3 Size { get { return new IntSize3(this.Width, this.Height, this.Depth); } }

		[SaveGameProperty("LargeObjects", Converter = typeof(LargeObjectSetConv))]
		HashSet<AreaObject> m_largeObjectSet;

		public event Action<AreaObject> LargeObjectAdded;
		public event Action<AreaObject> LargeObjectRemoved;

		public event Action<IntPoint3, TileData, TileData> TerrainOrInteriorChanged;

		EnvWaterHandler m_waterHandler;
		EnvTreeHandler m_treeHandler;

		EnvironmentObject(SaveGameContext ctx)
			: base(ctx, ObjectType.Environment)
		{
		}

		EnvironmentObject(EnvironmentObjectBuilder builder)
			: base(ObjectType.Environment)
		{
			this.Version = 1;
			this.VisibilityMode = builder.VisibilityMode;

			m_tileGrid = builder.Grid;
			m_depthMap = builder.DepthMap;

			var size = m_tileGrid.Size;
			this.Width = size.Width;
			this.Height = size.Height;
			this.Depth = size.Depth;

			SetSubterraneanFlags();

			m_contentArray = new KeyedObjectCollection[this.Depth];
			for (int i = 0; i < size.Depth; ++i)
				m_contentArray[i] = new KeyedObjectCollection();

			m_largeObjectSet = new HashSet<AreaObject>();
		}

		[OnSaveGamePostDeserialization]
		void OnDeserialized()
		{
			m_contentArray = new KeyedObjectCollection[this.Depth];
			for (int i = 0; i < this.Depth; ++i)
				m_contentArray[i] = new KeyedObjectCollection();

			foreach (var ob in this.Inventory)
				m_contentArray[ob.Z].Add(ob);

			this.World.TickStarting += Tick;

			m_waterHandler = new EnvWaterHandler(this);

			CreateDepthMap();
		}

		void CreateDepthMap()
		{
			m_depthMap = new ArrayGrid2D<byte>(this.Size.Plane);

			Parallel.ForEach(this.Size.Plane.Range(), p =>
			{
				for (int z = this.Size.Depth - 1; z >= 0; --z)
				{
					var p3 = new IntPoint3(p, z);

					if (m_tileGrid.GetTileData(p3).IsEmpty == false)
					{
						m_depthMap[p] = (byte)z;
						break;
					}
				}
			});
		}

		void SetSubterraneanFlags()
		{
			Parallel.ForEach(this.Size.Plane.Range(), p =>
			{
				int d = m_depthMap[p];

				for (int z = this.Size.Depth - 1; z >= 0; --z)
				{
					var p3 = new IntPoint3(p, z);

					if (z < d)
						m_tileGrid.SetFlags(p3, TileFlags.Subterranean);
					else
						m_tileGrid.ClearFlags(p3, TileFlags.Subterranean);
				}
			});
		}

		protected override void Initialize(World world)
		{
			base.Initialize(world);

			world.TickStarting += Tick;

			m_treeHandler = new EnvTreeHandler(this);

			m_waterHandler = new EnvWaterHandler(this);
		}

		public override void Destruct()
		{
			this.World.TickStarting -= Tick;

			base.Destruct();
		}

		void MapChanged(IntPoint3 l, TileData tileData)
		{
			this.World.AddChange(new MapChange(this, l, tileData));
		}

		public bool Contains(IntPoint3 p)
		{
			return p.X >= 0 && p.Y >= 0 && p.Z >= 0 && p.X < this.Width && p.Y < this.Height && p.Z < this.Depth;
		}

		void Tick()
		{
			m_waterHandler.HandleWater();
			m_treeHandler.Tick();
		}

		// XXX called by SetTerrain script
		public void ScanWaterTiles()
		{
			m_waterHandler.Rescan();
		}

		public int GetDepth(IntPoint2 p)
		{
			return m_depthMap[p];
		}

		public IntPoint3 GetSurface(int x, int y)
		{
			return new IntPoint3(x, y, m_depthMap[x, y]);
		}

		public IntPoint3 GetSurface(IntPoint2 p)
		{
			return new IntPoint3(p.X, p.Y, m_depthMap[p.X, p.Y]);
		}

		public TerrainID GetTerrainID(IntPoint3 l)
		{
			return m_tileGrid.GetTerrainID(l);
		}

		public MaterialID GetTerrainMaterialID(IntPoint3 l)
		{
			return m_tileGrid.GetTerrainMaterialID(l);
		}

		public InteriorID GetInteriorID(IntPoint3 l)
		{
			return m_tileGrid.GetInteriorID(l);
		}

		public MaterialID GetInteriorMaterialID(IntPoint3 l)
		{
			return m_tileGrid.GetInteriorMaterialID(l);
		}

		public TerrainInfo GetTerrain(IntPoint3 l)
		{
			return Terrains.GetTerrain(GetTerrainID(l));
		}

		public MaterialInfo GetTerrainMaterial(IntPoint3 l)
		{
			return Materials.GetMaterial(m_tileGrid.GetTerrainMaterialID(l));
		}

		public InteriorInfo GetInterior(IntPoint3 l)
		{
			return Interiors.GetInterior(GetInteriorID(l));
		}

		public MaterialInfo GetInteriorMaterial(IntPoint3 l)
		{
			return Materials.GetMaterial(m_tileGrid.GetInteriorMaterialID(l));
		}

		public TileData GetTileData(IntPoint3 l)
		{
			return m_tileGrid.GetTileData(l);
		}

		public byte GetWaterLevel(IntPoint3 l)
		{
			return m_tileGrid.GetWaterLevel(l);
		}

		public bool GetTileFlags(IntPoint3 l, TileFlags flags)
		{
			return (m_tileGrid.GetFlags(l) & flags) != 0;
		}

		public void SetTileData(IntPoint3 p, TileData data)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			var oldData = GetTileData(p);

			// retain the old flags
			data.Flags = oldData.Flags;

			m_tileGrid.SetTileData(p, data);

			if (oldData.HasTree != data.HasTree)
			{
				if (data.HasTree)
					m_treeHandler.AddTree();
				else
					m_treeHandler.RemoveTree();
			}

			var p2d = p.ToIntPoint();
			int oldSurfaceLevel = m_depthMap[p2d];
			int newSurfaceLevel = oldSurfaceLevel;

			if (data.IsEmpty == false && oldSurfaceLevel < p.Z)
			{
				// surface level has risen
				Debug.Assert(p.Z >= 0 && p.Z < 256);
				m_depthMap[p2d] = (byte)p.Z;
				newSurfaceLevel = p.Z;
			}
			else if (data.IsEmpty && oldSurfaceLevel == p.Z)
			{
				// surface level has lowered

				if (p.Z == 0)
					throw new Exception();

				for (int z = p.Z - 1; z >= 0; --z)
				{
					if (m_tileGrid.GetTileData(new IntPoint3(p2d, z)).IsEmpty == false)
					{
						Debug.Assert(z >= 0 && z < 256);
						m_depthMap[p2d] = (byte)z;
						newSurfaceLevel = z;
						break;
					}
				}
			}

			MapChanged(p, data);

			if (this.TerrainOrInteriorChanged != null)
				this.TerrainOrInteriorChanged(p, oldData, data);

			if (data.WaterLevel > 0)
				m_waterHandler.AddWater(p);
			else
				m_waterHandler.RemoveWater(p);

			if (newSurfaceLevel > oldSurfaceLevel)
			{
				for (int z = oldSurfaceLevel; z < newSurfaceLevel; ++z)
					SetTileFlags(new IntPoint3(p2d, z), TileFlags.Subterranean, true);
			}
			else if (newSurfaceLevel < oldSurfaceLevel)
			{
				for (int z = oldSurfaceLevel - 1; z >= newSurfaceLevel; --z)
					SetTileFlags(new IntPoint3(p2d, z), TileFlags.Subterranean, false);
			}
		}

		public void SetWaterLevel(IntPoint3 l, byte waterLevel)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetWaterLevel(l, waterLevel);

			var data = m_tileGrid.GetTileData(l);

			MapChanged(l, data);

			if (data.WaterLevel > 0)
				m_waterHandler.AddWater(l);
			else
				m_waterHandler.RemoveWater(l);
		}

		void SetTileFlags(IntPoint3 l, TileFlags flags, bool value)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			if (value)
				m_tileGrid.SetFlags(l, flags);
			else
				m_tileGrid.ClearFlags(l, flags);

			var d = m_tileGrid.GetTileData(l);

			MapChanged(l, d);
		}

		public void ItemBlockChanged(IntPoint3 p)
		{
			bool oldBlocking = GetTileFlags(p, TileFlags.ItemBlocks);
			bool newBlocking = GetContents(p).OfType<ItemObject>().Any(item => item.IsBlocking);

			if (oldBlocking != newBlocking)
				SetTileFlags(p, TileFlags.ItemBlocks, newBlocking);
		}

		public IEnumerable<IMovableObject> GetContents(IntGrid2Z rect)
		{
			var obs = m_contentArray[rect.Z];

			return obs.Where(o => rect.Contains(o.Location));
		}

		IEnumerable<IMovableObject> IEnvironmentObject.GetContents(IntPoint3 l)
		{
			var list = m_contentArray[l.Z];
			return list.Where(o => o.Location == l);
		}

		public IEnumerable<MovableObject> GetContents(IntPoint3 l)
		{
			var list = m_contentArray[l.Z];
			return list.Where(o => o.Location == l);
		}

		public bool HasContents(IntPoint3 l)
		{
			var list = m_contentArray[l.Z];
			return list.Any(o => o.Location == l);
		}

		public override bool OkToAddChild(MovableObject ob, IntPoint3 p)
		{
			Debug.Assert(this.World.IsWritable);

			if (!this.Contains(p))
				return false;

			if (!EnvironmentHelpers.CanEnter(this, p))
				return false;

			return true;
		}

		protected override void OnChildAdded(MovableObject child)
		{
			var list = m_contentArray[child.Z];
			Debug.Assert(!list.Contains(child));
			list.Add(child);
		}

		protected override void OnChildRemoved(MovableObject child)
		{
			var list = m_contentArray[child.Z];
			Debug.Assert(list.Contains(child));
			list.Remove(child);
		}


		public override bool OkToMoveChild(MovableObject ob, Direction dir, IntPoint3 dstLoc)
		{
			return EnvironmentHelpers.CanMoveFromTo(this, ob.Location, dir);
		}

		protected override void OnChildMoved(MovableObject child, IntPoint3 srcLoc, IntPoint3 dstLoc)
		{
			if (srcLoc.Z == dstLoc.Z)
				return;

			var list = m_contentArray[srcLoc.Z];
			Debug.Assert(list.Contains(child));
			list.Remove(child);

			list = m_contentArray[dstLoc.Z];
			Debug.Assert(!list.Contains(child));
			list.Add(child);
		}


		public IEnumerable<Direction> GetDirectionsFrom(IntPoint3 p)
		{
			return EnvironmentHelpers.GetDirectionsFrom(this, p);
		}

		public void AddLargeObject(AreaObject ob)
		{
			Debug.Assert(this.World.IsWritable);

			Debug.Assert(m_largeObjectSet.Any(b => b.Area.IntersectsWith(ob.Area)) == false);
			Debug.Assert(ob.IsInitialized == false);

			m_largeObjectSet.Add(ob);

			if (this.LargeObjectAdded != null)
				LargeObjectAdded(ob);
		}

		public void RemoveLargeObject(AreaObject ob)
		{
			Debug.Assert(this.World.IsWritable);
			Debug.Assert(m_largeObjectSet.Contains(ob));

			m_largeObjectSet.Remove(ob);

			if (this.LargeObjectRemoved != null)
				LargeObjectRemoved(ob);
		}

		public AreaObject GetLargeObjectAt(IntPoint3 p)
		{
			return m_largeObjectSet.SingleOrDefault(b => b.Contains(p));
		}

		public T GetLargeObjectAt<T>(IntPoint3 p) where T : AreaObject
		{
			return m_largeObjectSet.OfType<T>().SingleOrDefault(b => b.Contains(p));
		}

		public IEnumerable<AreaObject> GetLargeObjects()
		{
			return m_largeObjectSet;
		}

		protected override void CollectObjectData(BaseGameObjectData baseData, ObjectVisibility visibility)
		{
			base.CollectObjectData(baseData, visibility);

			var data = (MapData)baseData;

			data.VisibilityMode = this.VisibilityMode;
			data.Size = this.Size;
		}

		public override void SendTo(IPlayer player, ObjectVisibility visibility)
		{
			Debug.Assert(visibility != ObjectVisibility.None);

			var data = new MapData();
			CollectObjectData(data, visibility);
			player.Send(new Messages.ObjectDataMessage(data));

			var sw = Stopwatch.StartNew();
			SendMapTiles(player);
			sw.Stop();
			Trace.TraceInformation("Sending MapTiles took {0} ms", sw.ElapsedMilliseconds);

			foreach (var ob in this.Inventory)
			{
				var vis = player.GetObjectVisibility(ob);

				if (vis != ObjectVisibility.None)
					ob.SendTo(player, vis);
			}

			foreach (var o in m_largeObjectSet)
			{
				o.SendTo(player, ObjectVisibility.All);
			}
		}

		void SendMapTiles(IPlayer player)
		{
			var visionTracker = player.GetVisionTracker(this);

			int w = this.Width;
			int h = this.Height;
			int d = this.Depth;

#if !parallel
			bool useCompression = false;

			if (useCompression == false)
			{
				for (int z = 0; z < d; ++z)
				{
					var bounds = new IntGrid3(0, 0, z, w, h, 1);

					byte[] arr;

					using (var memStream = new MemoryStream(bounds.Volume * TileData.SizeOf))
					{
						WriteTileData(memStream, bounds, visionTracker);
						arr = memStream.ToArray();
					}

					var msg = new Messages.MapDataTerrainsMessage()
					{
						Environment = this.ObjectID,
						Bounds = bounds,
						IsTerrainDataCompressed = false,
						TerrainData = arr,
					};

					player.Send(msg);
					//Trace.TraceError("Sent {0}", z);
				}
			}
			else
			{
				for (int z = 0; z < d; ++z)
				{
					var bounds = new IntGrid3(0, 0, z, w, h, 1);

					byte[] arr;

					using (var memStream = new MemoryStream())
					{
						using (var compressStream = new DeflateStream(memStream, CompressionMode.Compress, true))
						using (var bufferedStream = new BufferedStream(compressStream))
						{
							WriteTileData(bufferedStream, bounds, visionTracker);
						}

						arr = memStream.ToArray();
					}

					var msg = new Messages.MapDataTerrainsMessage()
					{
						Environment = this.ObjectID,
						Bounds = bounds,
						IsTerrainDataCompressed = true,
						TerrainData = arr,
					};

					player.Send(msg);
					//Trace.TraceError("Sent {0}", z);
				}
			}
#else
			var queue = new BlockingCollection<Tuple<int, byte[]>>();

			var writerTask = Task.Factory.StartNew(() =>
			{
				foreach (var tuple in queue.GetConsumingEnumerable())
				{
					int z = tuple.Item1;
					var arr = tuple.Item2;

					var msg = new Messages.MapDataTerrainsMessage()
					{
						Environment = this.ObjectID,
						Bounds = new IntBox(0, 0, z, w, h, 1),
						IsTerrainDataCompressed = true,
						TerrainData = arr,
					};

					player.Send(msg);
					//Trace.TraceError("Sent {0}", z);
				}
			});


			Parallel.For(0, d, z =>
			{
				using (var memStream = new MemoryStream())
				{
					using (var compStream = new System.IO.Compression.DeflateStream(memStream, CompressionMode.Compress))
					using (var bufferStream = new BufferedStream(compStream))
					using (var writer = new BinaryWriter(bufferStream))
					{
						for (int y = 0; y < h; ++y)
						{
							for (int x = 0; x < w; ++x)
							{
								var p = new IntPoint3(x, y, z);

								ulong v;

								if (!visionTracker.Sees(p))
									v = 0;
								else
									v = m_tileGrid.GetTileData(p).Raw;

								writer.Write(v);
							}
						}
					}

					queue.Add(new Tuple<int, byte[]>(z, memStream.ToArray()));
				}
			});

			queue.CompleteAdding();

			writerTask.Wait();
#endif
		}

		void WriteTileData(Stream memStream, IntGrid3 bounds, IVisionTracker visionTracker)
		{
			using (var streamWriter = new BinaryWriter(memStream))
			{
				foreach (var p in bounds.Range())
				{
					ulong v;

					if (!visionTracker.Sees(p))
						v = 0;
					else
						v = m_tileGrid.GetTileData(p).Raw;

					streamWriter.Write(v);
				}
			}
		}

		public override string ToString()
		{
			return String.Format("Environment({0})", this.ObjectID);
		}

		int AStar.IAStarEnvironment.GetTileWeight(IntPoint3 p)
		{
			return 0;
		}

		IEnumerable<Direction> AStar.IAStarEnvironment.GetValidDirs(IntPoint3 p)
		{
			return EnvironmentHelpers.GetDirectionsFrom(this, p);
		}

		bool AStar.IAStarEnvironment.CanEnter(IntPoint3 p)
		{
			return EnvironmentHelpers.CanEnter(this, p);
		}

		void AStar.IAStarEnvironment.Callback(IDictionary<IntPoint3, AStar.AStarNode> nodes)
		{
		}

		sealed class LargeObjectSetConv : Dwarrowdelf.ISaveGameConverter
		{
			public object ConvertToSerializable(object value)
			{
				var set = (HashSet<AreaObject>)value;
				return set.ToArray();
			}

			public object ConvertFromSerializable(object value)
			{
				var arr = (AreaObject[])value;
				return new HashSet<AreaObject>(arr);
			}

			public Type InputType { get { return typeof(HashSet<AreaObject>); } }

			public Type OutputType { get { return typeof(AreaObject[]); } }
		}

		sealed class TileGridReaderWriter : ISaveGameReaderWriter
		{
			public void Write(Newtonsoft.Json.JsonWriter writer, object value)
			{
				var grid = (TileGrid)value;

				int w = grid.Grid.GetLength(2);
				int h = grid.Grid.GetLength(1);
				int d = grid.Grid.GetLength(0);

				writer.WriteStartObject();

				writer.WritePropertyName("Width");
				writer.WriteValue(w);
				writer.WritePropertyName("Height");
				writer.WriteValue(h);
				writer.WritePropertyName("Depth");
				writer.WriteValue(d);

				writer.WritePropertyName("TileData");
				writer.WriteStartArray();

				var queue = new BlockingCollection<Tuple<int, byte[]>>();

				var writerTask = Task.Factory.StartNew(() =>
				{
					foreach (var tuple in queue.GetConsumingEnumerable())
					{
						writer.WriteValue(tuple.Item1);
						writer.WriteValue(tuple.Item2);
					}
				});

				Parallel.For(0, d, z =>
				{
					using (var memStream = new MemoryStream())
					{
						using (var compressStream = new DeflateStream(memStream, CompressionMode.Compress, true))
						using (var bufferedStream = new BufferedStream(compressStream))
						using (var streamWriter = new BinaryWriter(bufferedStream))
						{
							var srcArr = grid.Grid;

							for (int y = 0; y < h; ++y)
								for (int x = 0; x < w; ++x)
									streamWriter.Write(srcArr[z, y, x].Raw);
						}

						queue.Add(new Tuple<int, byte[]>(z, memStream.ToArray()));
					}
				});

				queue.CompleteAdding();

				writerTask.Wait();

				writer.WriteEndArray();
				writer.WriteEndObject();
			}

			static void ReadAndValidate(Newtonsoft.Json.JsonReader reader, Newtonsoft.Json.JsonToken token)
			{
				reader.Read();
				if (reader.TokenType != token)
					throw new Exception();
			}

			static int ReadIntProperty(Newtonsoft.Json.JsonReader reader, string propertyName)
			{
				reader.Read();
				if (reader.TokenType != Newtonsoft.Json.JsonToken.PropertyName || (string)reader.Value != propertyName)
					throw new Exception();

				reader.Read();
				if (reader.TokenType != Newtonsoft.Json.JsonToken.Integer)
					throw new Exception();

				return (int)(long)reader.Value;
			}

			public object Read(Newtonsoft.Json.JsonReader reader)
			{
				if (reader.TokenType != Newtonsoft.Json.JsonToken.StartObject)
					throw new Exception();

				int w = ReadIntProperty(reader, "Width");
				int h = ReadIntProperty(reader, "Height");
				int d = ReadIntProperty(reader, "Depth");

				var grid = new TileGrid(new IntSize3(w, h, d));
				var dstArr = grid.Grid;

				reader.Read();
				if (reader.TokenType != Newtonsoft.Json.JsonToken.PropertyName || (string)reader.Value != "TileData")
					throw new Exception();

				ReadAndValidate(reader, Newtonsoft.Json.JsonToken.StartArray);

				var queue = new BlockingCollection<Tuple<int, byte[]>>();

				var readerTask = Task.Factory.StartNew(() =>
				{
					for (int i = 0; i < d; ++i)
					{
						reader.Read();
						int z = (int)(long)reader.Value;

						byte[] buf = reader.ReadAsBytes();

						queue.Add(new Tuple<int, byte[]>(z, buf));
					}

					queue.CompleteAdding();
				});

				Parallel.For(0, d, i =>
				{
					var tuple = queue.Take();

					int z = tuple.Item1;
					byte[] arr = tuple.Item2;

					using (var memStream = new MemoryStream(arr))
					{
						using (var decompressStream = new DeflateStream(memStream, CompressionMode.Decompress))
						using (var streamReader = new BinaryReader(decompressStream))
						{
							for (int y = 0; y < h; ++y)
								for (int x = 0; x < w; ++x)
									dstArr[z, y, x].Raw = streamReader.ReadUInt64();
						}
					}
				});

				readerTask.Wait();

				ReadAndValidate(reader, Newtonsoft.Json.JsonToken.EndArray);
				ReadAndValidate(reader, Newtonsoft.Json.JsonToken.EndObject);

				return grid;
			}
		}
	}
}