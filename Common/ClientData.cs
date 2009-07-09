﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

/*
 * Classes to deliver data to client
 */

namespace MyGame.ClientMsgs
{
	[DataContract,
	KnownType(typeof(ItemsData)),
	KnownType(typeof(MapData)),
	KnownType(typeof(TerrainData)),
	KnownType(typeof(ObjectMove)),
	KnownType(typeof(TurnChange))]
	public abstract class Message
	{
	}

	/* Item in inventory or floor */
	[DataContract]
	public class ItemData
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public int SymbolID { get; set; }
	}

	[DataContract]
	public class ItemsData : Message
	{
		[DataMember]
		public ItemData[] Items { get; set; }
	}

	/* Tile that came visible */
	[DataContract]
	public class MapData
	{
		[DataMember]
		public Location Location { get; set; }
		[DataMember]
		public int Terrain { get; set; }
		[DataMember]
		public ObjectID[] Objects { get; set; }
	}

	[DataContract]
	public class TerrainData : Message
	{
		[DataMember]
		public MapData[] MapDataList { get; set; }
	}

	[DataContract]
	public class ObjectMove : Message
	{
		[DataMember]
		public ObjectID ObjectID { get; set; }
		[DataMember]
		public Location TargetLocation { get; set; }
		[DataMember]
		public Location SourceLocation { get; set; }
		[DataMember]
		public int Symbol { get; set; }

		public ObjectMove(GameObject target, int symbol, Location from, Location to)
		{
			this.ObjectID = target.ObjectID;
			this.Symbol = symbol;
			this.SourceLocation = from;
			this.TargetLocation = to;
		}

		public override string ToString()
		{
			return String.Format("ObjectMove {0} {1}->{2}", this.ObjectID,
				this.SourceLocation, this.TargetLocation);
		}
	}

	[DataContract]
	public class TurnChange : Message
	{
		[DataMember]
		public int TurnNumber { get; set; }
	}



}
