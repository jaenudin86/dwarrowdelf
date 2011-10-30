﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.AI
{
	[SaveGameObjectByRef]
	public class Herd
	{
		[SaveGameProperty]
		List<IAI> m_members = new List<IAI>();

		public Herd()
		{
		}

		Herd(SaveGameContext ctx)
		{
		}

		public void AddMember(IAI ai)
		{
			Debug.Assert(!m_members.Contains(ai));

			m_members.Add(ai);
			ai.Worker.Destructed += OnDestructed;
		}

		public void RemoveMember(IAI ai)
		{
			Debug.Assert(m_members.Contains(ai));

			m_members.Remove(ai);
		}

		void OnDestructed(IBaseGameObject ob)
		{
			var a = m_members.Single(ai => ai.Worker == ob);

			RemoveMember(a);
		}

		public int HerdSize
		{
			get { return m_members.Count; }
		}

		public IntPoint3D GetCenter()
		{
			var locations = m_members.Select(ai => ai.Worker.Location);

			return IntPoint3D.Center(locations);
		}
	}
}
