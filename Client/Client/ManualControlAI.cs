﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	[SaveGameObjectByRef]
	class ManualControlAI : AI.IAI, INotifyPropertyChanged
	{
		LivingObject m_worker;
		ObservableCollection<GameAction> m_actions;
		public ReadOnlyObservableCollection<GameAction> Actions { get; private set; }
		GameAction m_currentAction;

		public ManualControlAI(LivingObject worker)
		{
			m_worker = worker;
			m_actions = new ObservableCollection<GameAction>();
			this.Actions = new ReadOnlyObservableCollection<GameAction>(m_actions);
		}

		ManualControlAI(SaveGameContext ctx)
		{
			this.Actions = new ReadOnlyObservableCollection<GameAction>(m_actions);
		}

		public string Name { get { return "ManualControlAI"; } }

		public void AddAction(GameAction action)
		{
			m_actions.Add(action);
		}

		#region IAI Members

		public ILivingObject Worker { get { return m_worker; } }

		public GameAction DecideAction(ActionPriority priority)
		{
			if (this.Worker.HasAction)
			{
				if (this.Worker.ActionPriority > priority)
					return this.Worker.CurrentAction;

				if (m_currentAction != null && this.Worker.CurrentAction.MagicNumber == m_currentAction.MagicNumber)
					return this.Worker.CurrentAction;
			}

			if (m_actions.Count == 0)
			{
				if (this.Worker.CurrentAction != null)
					return this.Worker.CurrentAction;

				return new WaitAction(1);
			}

			m_currentAction = m_actions[0];
			return m_currentAction;
		}

		public void ActionStarted(ActionStartedChange change)
		{
			if (m_currentAction == null)
				return;

			if (change.Action.MagicNumber != m_currentAction.MagicNumber)
				m_currentAction = null;
		}

		public void ActionProgress(ActionProgressChange change)
		{
		}

		public void ActionDone(ActionDoneChange change)
		{
			if (m_currentAction == null)
				return;

			if (change.MagicNumber == m_currentAction.MagicNumber)
			{
				Debug.Assert(m_actions[0] == m_currentAction);
				m_currentAction = null;
				m_actions.RemoveAt(0);
			}
		}

		#endregion

		public override string ToString()
		{
			return "ManualControlAI";
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		void Notify(string info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}
}