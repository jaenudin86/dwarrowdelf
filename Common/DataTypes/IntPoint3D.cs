﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	public struct IntPoint3D : IEquatable<IntPoint3D>
	{
		[DataMember]
		public int X { get; set; }
		[DataMember]
		public int Y { get; set; }
		[DataMember]
		public int Z { get; set; }

		public IntPoint3D(int x, int y, int z)
			: this()
		{
			X = x;
			Y = y;
			Z = z;
		}

		public IntPoint3D(IntPoint p, int z)
			: this()
		{
			X = p.X;
			Y = p.Y;
			Z = z;
		}

		#region IEquatable<Location3D> Members

		public bool Equals(IntPoint3D other)
		{
			return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntPoint3D))
				return false;

			IntPoint3D l = (IntPoint3D)obj;
			return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
		}

		public static bool operator ==(IntPoint3D left, IntPoint3D right)
		{
			return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
		}

		public static bool operator !=(IntPoint3D left, IntPoint3D right)
		{
			return !(left == right);
		}

		public static IntPoint3D operator +(IntPoint3D left, IntVector3D right)
		{
			return new IntPoint3D(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static IntVector3D operator -(IntPoint3D left, IntPoint3D right)
		{
			return new IntVector3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static IntPoint3D operator +(IntPoint3D left, IntVector right)
		{
			return new IntPoint3D(left.X + right.X, left.Y + right.Y, left.Z);
		}

		public override int GetHashCode()
		{
			return (this.X << 20) | (this.Y << 10) | this.Z;
		}

		public static IEnumerable<IntPoint3D> Range(int x, int y, int z, int width, int height, int depth)
		{
			int max_x = x + width;
			int max_y = y + height;
			int max_z = z + depth;
			for (; z < max_z; ++z)
				for (; y < max_y; ++y)
					for (; x < max_x; ++x)
						yield return new IntPoint3D(x, y, z);
		}

		public override string ToString()
		{
			return String.Format(System.Globalization.CultureInfo.InvariantCulture,
				"IntPoint3D({0}, {1}, {2})", X, Y, Z);
		}
	}
}
