﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject]
	public sealed class GetItemAssignment : Assignment
	{
		[SaveGameProperty("Item")]
		readonly IItemObject m_item;

		public GetItemAssignment(IJobObserver parent, IItemObject item)
			: base(parent)
		{
			m_item = item;
		}

		GetItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new GetItemAction(m_item);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "GetItemAssignment";
		}
	}
}
