﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class World
	{
		public event Action TickStarting;
		public event Action TickEnded;

		public event Action<LivingObject> TurnStarting;
		public event Action<LivingObject> TurnEnded;

		public event Action WorkEnded;

		// XXX hackish
		public event Action HandleMessagesEvent;

		[SaveGameProperty]
		public int TickNumber { get; private set; }

		enum WorldState
		{
			Idle,
			TickOngoing,
			TickDone,
			TickEnded,
		}

		[SaveGameProperty("State")]
		WorldState m_state;

		public bool IsTickOnGoing { get { return m_state == WorldState.TickOngoing; } }

		bool m_okToStartTick = false;
		bool m_proceedTurn = false;

		public void SetOkToStartTick()
		{
			trace.TraceVerbose("SetOkToStartTick");
			VerifyAccess();
			m_okToStartTick = true;
		}

		public void SetProceedTurn()
		{
			trace.TraceVerbose("SetProceedTurn");
			VerifyAccess();
			m_proceedTurn = true;
		}

		public bool Work()
		{
			// Hack
			if (m_worldThread == null)
				m_worldThread = Thread.CurrentThread;

			VerifyAccess();

			EnterWriteLock();

			m_instantInvokeList.ProcessInvokeList();

			if (HandleMessagesEvent != null)
				HandleMessagesEvent();

			bool again = true;

			if (m_state == WorldState.Idle)
			{
				m_preTickInvokeList.ProcessInvokeList();
				m_livings.Process();

				if (m_okToStartTick)
					StartTick();
				else
					again = false;
			}

			if (m_state == WorldState.TickOngoing)
			{
				if (this.TickMethod == WorldTickMethod.Simultaneous)
					again = SimultaneousWork();
				else if (this.TickMethod == WorldTickMethod.Sequential)
					again = SequentialWork();
				else
					throw new NotImplementedException();
			}

			if (m_state == WorldState.TickDone)
				EndTick();

			ExitWriteLock();

			// no point in entering read lock here, as this thread is the only one that can get a write lock

			if (WorkEnded != null)
				WorkEnded();

			if (m_state == WorldState.TickEnded)
				m_state = WorldState.Idle;

			return again;
		}

		bool SimultaneousWork()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			trace.TraceVerbose("SimultaneousWork");

			if (!m_proceedTurn)
				return false;

			m_proceedTurn = false;

			foreach (var living in m_livings.List)
				living.TurnPreRun();

			foreach (var living in m_livings.List.Where(l => l.HasAction))
				living.ProcessAction();

			EndTurnSimultaneous();

			m_state = WorldState.TickDone;

			trace.TraceVerbose("SimultaneousWork Done");

			return true;
		}


		bool SequentialWork()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			if (m_livings.List.Count == 0)
			{
				trace.TraceVerbose("no livings to handled");
				m_state = WorldState.TickDone;
				return true;
			}

			bool again = true;

			while (true)
			{
				var living = m_livingEnumerator.Current;

				trace.TraceVerbose("work loop, current {0}, HasAction {1}", living, living.HasAction);

				bool proceed = m_proceedTurn ||
					m_livings.RemoveList.Contains(living) ||
					living.Controller == null;

				if (!proceed)
				{
					again = false;
					break;
				}

				m_proceedTurn = false;

				living.TurnPreRun();

				if (living.HasAction)
					living.ProcessAction();

				EndTurnSequential(living);

				bool ok = m_livingEnumerator.MoveNext();
				if (ok)
				{
					StartTurnSequential(m_livingEnumerator.Current);
				}
				else
				{
					trace.TraceVerbose("last living handled");
					m_state = WorldState.TickDone;
					break;
				}
			}

			trace.TraceVerbose("SequentialWork Done");

			return again;
		}

		void StartTick()
		{
			VerifyAccess();

			this.TickNumber++;
			MyTraceContext.ThreadTraceContext.Tick = this.TickNumber;
			AddChange(new TickStartChange(this.TickNumber));

			trace.TraceVerbose("-- Tick {0} started --", this.TickNumber);

			m_state = WorldState.TickOngoing;

			if (TickStarting != null)
				TickStarting();

			if (this.TickMethod == WorldTickMethod.Simultaneous)
			{
				StartTurnSimultaneous();
			}
			else if (this.TickMethod == WorldTickMethod.Sequential)
			{
				m_livingEnumerator.Reset();

				bool ok = m_livingEnumerator.MoveNext();
				if (ok)
					StartTurnSequential(m_livingEnumerator.Current);
			}
		}

		void StartTurnSimultaneous()
		{
			foreach (var living in m_livings.List)
				living.TurnStarted();

			AddChange(new TurnStartChange(null));

			if (TurnStarting != null)
				TurnStarting(null);
		}

		void EndTurnSimultaneous()
		{
			AddChange(new TurnEndChange(null));

			if (TurnEnded != null)
				TurnEnded(null);
		}

		void StartTurnSequential(LivingObject living)
		{
			trace.TraceVerbose("StartTurnSeq {0}", living);

			living.TurnStarted();

			AddChange(new TurnStartChange(living));

			if (TurnStarting != null)
				TurnStarting(living);
		}


		void EndTurnSequential(LivingObject living)
		{
			trace.TraceVerbose("EndTurnSeq {0}", living);

			AddChange(new TurnEndChange(living));

			if (TurnEnded != null)
				TurnEnded(living);
		}

		void EndTick()
		{
			VerifyAccess();

			trace.TraceVerbose("-- Tick {0} ended --", this.TickNumber);
			m_state = WorldState.TickEnded;

			m_okToStartTick = false;

			if (TickEnded != null)
				TickEnded();
		}
	}
}
