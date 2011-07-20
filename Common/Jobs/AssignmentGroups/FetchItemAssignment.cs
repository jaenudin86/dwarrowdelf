﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	[SaveGameObject(UseRef = true)]
	public class FetchItemAssignment : AssignmentGroup
	{
		[SaveGameProperty]
		public IItemObject Item { get; private set; }
		[SaveGameProperty]
		IntPoint3D m_location;
		[SaveGameProperty]
		IEnvironment m_environment;
		[SaveGameProperty("State")]
		int m_state;

		public FetchItemAssignment(IJob parent, ActionPriority priority, IEnvironment env, IntPoint3D location, IItemObject item)
			: base(parent, priority)
		{
			this.Item = item;
			m_environment = env;
			m_location = location;
		}

		protected FetchItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override JobStatus AssignOverride(ILiving worker)
		{
			m_state = 0;
			return JobStatus.Ok;
		}

		protected override void OnAssignmentDone()
		{
			if (m_state == 3)
				SetStatus(Jobs.JobStatus.Done);
			else
				m_state = m_state + 1;
		}

		protected override IAssignment PrepareNextAssignment()
		{
			IAssignment assignment;

			switch (m_state)
			{
				case 0:
					assignment = new MoveAssignment(this, this.Priority, this.Item.Environment, this.Item.Location, DirectionSet.Exact);
					break;

				case 1:
					assignment = new GetItemAssignment(this, this.Priority, this.Item);
					break;

				case 2:
					assignment = new MoveAssignment(this, this.Priority, m_environment, m_location, DirectionSet.Exact);
					break;

				case 3:
					assignment = new DropItemAssignment(this, this.Priority, this.Item);
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}

		public override string ToString()
		{
			return "FetchItemAssignment";
		}
	}
}