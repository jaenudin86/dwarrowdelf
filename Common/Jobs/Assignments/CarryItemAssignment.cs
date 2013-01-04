﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject]
	public sealed class CarryItemAssignment : Assignment
	{
		[SaveGameProperty("Item")]
		readonly IItemObject m_item;

		public CarryItemAssignment(IJobObserver parent, IItemObject item)
			: base(parent)
		{
			m_item = item;
		}

		CarryItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new CarryItemAction(m_item);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "CarryItemAssignment";
		}
	}
}
