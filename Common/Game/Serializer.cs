﻿using System;
using System.IO;
using System.Linq;
using Dwarrowdelf.Messages;

namespace Dwarrowdelf
{
	static class Serializer
	{
		static NetSerializer.Serializer s_serializer;

		static Serializer()
		{
			var dataTypes = new Type[] { typeof(IntGrid2), typeof(IntGrid2Z) };
			var messageTypes = Helpers.GetSubclasses(typeof(Message));
			var objectDataTypes = Helpers.GetSubclasses(typeof(BaseGameObjectData));
			var changeTypes = Helpers.GetSubclasses(typeof(ChangeData));
			var actionTypes = Helpers.GetSubclasses(typeof(GameAction));
			var events = Helpers.GetSubclasses(typeof(GameEvent));
			var extra = new Type[] { typeof(GameColor), typeof(LivingGender), typeof(GameSeason) };
			var reports = Helpers.GetSubclasses(typeof(GameReport));
			var tileDataEnums = typeof(TileData).GetFields().Select(fi => fi.FieldType);
			var types = dataTypes.Concat(messageTypes).Concat(objectDataTypes).Concat(changeTypes).Concat(actionTypes).Concat(events)
				.Concat(extra).Concat(reports).Concat(tileDataEnums);

			s_serializer = new NetSerializer.Serializer(types.ToArray());
		}

		public static void Serialize(Stream stream, Message msg)
		{
			s_serializer.Serialize(stream, msg);
		}

		public static Message Deserialize(Stream stream)
		{
			object ob = s_serializer.Deserialize(stream);
			return (Message)ob;
		}
	}
}
