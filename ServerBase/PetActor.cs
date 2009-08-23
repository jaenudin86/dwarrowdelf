﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class PetActor : IActor
	{
		ServerGameObject m_object;
		Living m_player;
		GameAction m_currentAction;
		Random m_random = new Random();

		public PetActor(ServerGameObject ob, Living player)
		{
			m_object = ob;
			m_player = player;
		}

		GameAction GetNewAction()
		{
			return GetNewActionAstar();
		}

		Queue<Direction> m_pathDirs;
		IntPoint m_pathDest;

		GameAction GetNewActionAstar()
		{
			GameAction action;

			var v = m_player.Location - m_object.Location;

			if (v.ManhattanLength < 3)
				return new WaitAction(0, m_object, 1);

			if (m_pathDirs == null || (m_player.Location - m_pathDest).ManhattanLength > 3)
			{
				IEnumerable<Direction> dirs = AStar.FindPath(m_object.Location, m_player.Location,
					l => m_object.Environment.Bounds.Contains(l) && m_object.Environment.IsWalkable(l));

				m_pathDirs = new Queue<Direction>(dirs);
				m_pathDest = m_player.Location;
			}

			if (m_pathDirs.Count == 0)
				return new WaitAction(0, m_object, 1);

			Direction dir = m_pathDirs.Dequeue();
			if (m_pathDirs.Count == 0)
				m_pathDirs = null;

			action = new MoveAction(0, m_object, dir);

			return action;
		}

		GameAction GetNewActionNoAstar()
		{
			GameAction action;

			var v = m_player.Location - m_object.Location;

			if (v.ManhattanLength < 3)
				return new WaitAction(0, m_object, 1);

			v.Normalize();

			if (v == new IntVector(0, 0))
				return new WaitAction(0, m_object, 1);

			var env = m_object.Environment;

			action = null;
			int angle = 45;
			IntVector ov = v;
			for (int i = 0; i < 8; ++i)
			{
				v = ov;
				// 0, 45, -45, 90, -90, 135, -135, 180
				angle = ((i + 1) / 2) * (i % 2 * 2 - 1);
				v.FastRotate(angle);

				if (env.IsWalkable(m_object.Location + v))
				{
					Direction dir = v.ToDirection();
					if (dir == Direction.None)
						throw new Exception();
					action = new MoveAction(0, m_object, dir);
					break;
				}
			}

			if (action == null)
				return new WaitAction(0, m_object, 1);

			return action;
		}

		#region IActor Members

		public void RemoveAction(GameAction action)
		{
			m_currentAction = null;
		}

		public GameAction GetCurrentAction()
		{
			if (m_currentAction == null)
				m_currentAction = GetNewAction();

			return m_currentAction;
		}

		public bool HasAction
		{
			get { return true; }
		}

		public bool IsInteractive
		{
			get { return false; }
		}

		public void ReportAction(bool done, bool success)
		{
		}

		// Disable "event not used"
#pragma warning disable 67
		public event Action ActionQueuedEvent;

		#endregion
	}
}
