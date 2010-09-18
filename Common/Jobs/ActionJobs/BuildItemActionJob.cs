﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.ActionJobs
{
	public class BuildItemActionJob : ActionJob
	{
		IItemObject[] m_items;

		public BuildItemActionJob(IJob parent, ActionPriority priority, IItemObject[] items)
			: base(parent, priority)
		{
			m_items = items;
		}

		protected override void Cleanup()
		{
			m_items = null;
		}

		protected override Progress PrepareNextActionOverride()
		{
			var action = new BuildItemAction(m_items, this.Priority);
			this.CurrentAction = action;
			return Progress.Ok;
		}

		protected override Progress ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return Progress.Ok;

				case ActionState.Done:
					return Progress.Done;

				case ActionState.Fail:
					return Progress.Fail;

				case ActionState.Abort:
					return Progress.Abort;

				default:
					throw new Exception();
			}
		}

		public override string ToString()
		{
			return "BuildItemActionJob";
		}
	}
}
