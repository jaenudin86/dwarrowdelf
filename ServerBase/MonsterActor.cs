﻿//#define STAYSTILL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class MonsterActor : IActor
	{
		ServerGameObject m_object;
		GameAction m_currentAction;
		Random m_random;

		public MonsterActor(ServerGameObject ob)
		{
			m_random = new Random(GetHashCode());
			m_object = ob;
			m_currentAction = GetNewAction();
		}

		GameAction GetNewAction()
		{
#if STAYSTILL
			return null;
#else
			GameAction action;

			if (m_random.Next(4) == 0)
				action = new WaitAction(0, m_object, m_random.Next(3) + 1);
			else
			{
				IntVector v = new IntVector(1, 1);
				v.Rotate(45 * m_random.Next(8));
				Direction dir = v.ToDirection();

				if (dir == Direction.None)
					throw new Exception();

				action = new MoveAction(0, m_object, dir);
			}

			return action;
#endif
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
