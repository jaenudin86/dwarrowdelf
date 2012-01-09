﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntPoint3DConverter))]
	public struct IntPoint3D : IEquatable<IntPoint3D>
	{
		readonly int m_x;
		readonly int m_y;
		readonly int m_z;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Z { get { return m_z; } }

		public IntPoint3D(int x, int y, int z)
		{
			m_x = x;
			m_y = y;
			m_z = z;
		}

		public IntPoint3D(IntPoint p, int z)
		{
			m_x = p.X;
			m_y = p.Y;
			m_z = z;
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

		public static IntPoint3D operator -(IntPoint3D left, IntVector3D right)
		{
			return new IntPoint3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static IntPoint3D operator +(IntPoint3D left, Direction right)
		{
			return left + new IntVector3D(right);
		}

		public static IntVector3D operator -(IntPoint3D left, IntPoint3D right)
		{
			return new IntVector3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static IntPoint3D operator +(IntPoint3D left, IntVector right)
		{
			return new IntPoint3D(left.X + right.X, left.Y + right.Y, left.Z);
		}

		public bool IsAdjacentTo(IntPoint3D p, DirectionSet positioning)
		{
			var v = p - this;

			if (!v.IsNormal)
				return false;

			var d = v.ToDirection();

			return positioning.Contains(d);
		}

		public override int GetHashCode()
		{
			// 8 bits for Z, 12 bits for X/Y
			return (this.Z << 24) | (this.Y << 12) | (this.X << 0);
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

		public static IEnumerable<IntPoint3D> Range(int width, int height, int depth)
		{
			for (int z = 0; z < depth; ++z)
				for (int y = 0; y < height; ++y)
					for (int x = 0; x < width; ++x)
						yield return new IntPoint3D(x, y, z);
		}

		public IntPoint ToIntPoint()
		{
			return new IntPoint(this.X, this.Y);
		}

		public static IntPoint3D Center(IEnumerable<IntPoint3D> points)
		{
			int x, y, z;
			int count = 0;
			x = y = z = 0;

			foreach (var p in points)
			{
				x += p.X;
				y += p.Y;
				z += p.Z;
				count++;
			}

			return new IntPoint3D(x / count, y / count, z / count);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2}", this.X, this.Y, this.Z);
		}

		public static IntPoint3D Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntPoint3D(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info), Convert.ToInt32(arr[2], info));
		}
	}
}
