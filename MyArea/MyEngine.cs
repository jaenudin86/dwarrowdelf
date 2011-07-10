﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;


namespace MyArea
{
	public class MyEngine : GameEngine
	{
		public MyEngine(string gameDir)
			: base(gameDir)
		{
		}

		protected override void InitializeWorld()
		{
			var area = new Area();
			area.InitializeWorld(this.World);
		}

		Random m_random = new Random();

		public override Living[] CreateControllables(Player player)
		{
			const int NUM_DWARVES = 5;

			var env = this.World.Environments.First(); // XXX entry location

			var list = new List<Living>();

			for (int i = 0; i < NUM_DWARVES; ++i)
			{
				IntPoint3D p;
				do
				{
					p = new IntPoint3D(m_random.Next(env.Width), m_random.Next(env.Height), 9);
				} while (env.GetInteriorID(p) != InteriorID.Empty);

				var l = CreateDwarf(i);

				if (!l.MoveTo(env, p))
					throw new Exception();

				list.Add(l);
			}

			return list.ToArray();
		}

		Living CreateDwarf(int i)
		{
			var l = new Living(String.Format("Dwarf{0}", i))
			{
				SymbolID = SymbolID.Player,
				Color = (GameColor)m_random.Next((int)GameColor.NumColors - 1) + 1,
			};
			l.SetAI(new DwarfAI(l));

			switch (i)
			{
				case 0:
					l.Name = "Miner";
					l.SetSkillLevel(SkillID.Mining, 100);
					break;

				case 1:
					l.Name = "Carpenter";
					l.SetSkillLevel(SkillID.Carpentry, 100);
					break;

				case 2:
					l.Name = "Wood Cutter";
					l.SetSkillLevel(SkillID.WoodCutting, 100);
					break;

				case 3:
					l.Name = "Mason";
					l.SetSkillLevel(SkillID.Masonry, 100);
					break;

				case 4:
					l.Name = "Fighter";
					l.SetSkillLevel(SkillID.Fighting, 100);
					break;
			}

			l.Initialize(this.World);

			return l;
		}
	}
}
