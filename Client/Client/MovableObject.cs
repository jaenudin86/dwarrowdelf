﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Dwarrowdelf.Client
{
	delegate void ObjectMoved(MovableObject ob, ContainerObject dst, IntPoint3D loc);

	[SaveGameObjectByRef(ClientObject = true)]
	abstract class MovableObject : ContainerObject, IMovableObject
	{
		public event ObjectMoved ObjectMoved;

		public bool IsLiving { get; protected set; }

		public MovableObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
		}

		public override void Destruct()
		{
			if (this.Parent != null)
				throw new Exception();

			base.Destruct();
		}

		public void MoveTo(ContainerObject parent, IntPoint3D location)
		{
			var oldParent = this.Parent;

			if (oldParent != null)
				oldParent.RemoveChild(this);

			this.Parent = parent;
			this.Location = location;

			if (parent != null)
				parent.AddChild(this);

			if (ObjectMoved != null)
				ObjectMoved(this, this.Parent, this.Location);
		}

		public void MoveTo(IntPoint3D location)
		{
			var oldLocation = this.Location;

			this.Location = location;

			this.Parent.MoveChild(this, oldLocation, location);

			if (ObjectMoved != null)
				ObjectMoved(this, this.Parent, this.Location);
		}

		public EnvironmentObject Environment
		{
			get { return this.Parent as EnvironmentObject; }
		}

		IEnvironmentObject IMovableObject.Environment
		{
			get { return this.Parent as IEnvironmentObject; }
		}

		public override string ToString()
		{
			return String.Format("Object({0})", this.ObjectID);
		}

		ContainerObject m_parent;
		public ContainerObject Parent
		{
			get { return m_parent; }
			private set { m_parent = value; Notify("Parent"); }
		}

		IContainerObject IMovableObject.Parent { get { return this.Parent; } }

		IntPoint3D m_location;
		public IntPoint3D Location
		{
			get { return m_location; }
			private set { m_location = value; Notify("Location"); }
		}
	}
}