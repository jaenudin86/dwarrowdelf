﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public static class MyMath
	{
		/// <summary>
		/// Wrap integer with the given maximum. Handles negative wrapping also.
		/// </summary>
		public static int Wrap(int i, int i_max)
		{
			return ((i % i_max) + i_max) % i_max;
		}

		/// <summary>
		/// Divide signed integer m with positive integer n, rounding up for positive m and and down for negative m
		/// </summary>
		/// <param name="m">Dividend</param>
		/// <param name="n">Divisor</param>
		/// <returns>Quotient</returns>
		public static int DivRoundUp(int m, int n)
		{
			return m > 0 ?
				(m + n - 1) / n :
				(m - n + 1) / n;
		}

		/// <summary>
		/// Divide signed integer m with positive integer n, rounding to nearest integer
		/// </summary>
		/// <param name="m">Dividend</param>
		/// <param name="n">Divisor</param>
		/// <returns>Quotient</returns>
		public static int DivRoundNearest(int m, int n)
		{
			return m > 0 ?
				(m + n / 2) / n :
				(m - n / 2) / n;
		}

		/// <summary>
		/// Clamp an integer between two values
		/// </summary>
		/// <param name="value">Value to be clamped</param>
		/// <param name="max">Maximum value</param>
		/// <param name="min">Minimum value</param>
		/// <returns>Clamped value</returns>
		public static int Clamp(int value, int max, int min)
		{
			return value > max ? max : (value < min ? min : value);
		}

		/// <summary>
		/// Clamp a double between two values
		/// </summary>
		/// <param name="value">Value to be clamped</param>
		/// <param name="max">Maximum value</param>
		/// <param name="min">Minimum value</param>
		/// <returns>Clamped value</returns>
		public static double Clamp(double value, double max, double min)
		{
			return value > max ? max : (value < min ? min : value);
		}

		/// <summary>
		/// Clamp a double between 0 and 1
		/// </summary>
		public static double Clamp(double value)
		{
			return value > 1 ? 1 : (value < 0 ? 0 : value);
		}

		/// <summary>
		/// Clamp a float between 0 and 1
		/// </summary>
		public static float Clamp(float value)
		{
			return value > 1 ? 1 : (value < 0 ? 0 : value);
		}

		/// <summary>
		/// 2^n
		/// </summary>
		public static int Pow2(int n)
		{
			return 1 << n;
		}

		/// <summary>
		/// n * n
		/// </summary>
		public static int Square(int n)
		{
			return n * n;
		}

		/// <summary>
		/// Return integer Log2 of an integer
		/// </summary>
		public static int Log2(int value)
		{
			int r = 0;

			while ((value >>= 1) != 0)
				r++;

			return r;
		}

		/// <summary>
		/// Is the number a power of two (2^n)
		/// </summary>
		public static bool IsPowerOfTwo(int x)
		{
			return x != 0 && (x & (x - 1)) == 0;
		}

		/// <summary>
		/// Return a zigzag number of i: 0, 1, -1, 2, -2, 3, -3, ...
		/// </summary>
		public static int ZigZag(int i)
		{
			return ((i + 1) >> 1) * (((i & 1) << 1) - 1);
		}

		/// <summary>
		/// Shuffle the items in an array
		/// </summary>
		public static void ShuffleArray<T>(T[] array, Random random)
		{
			if (array.Length == 0)
				return;

			for (int i = array.Length - 1; i >= 1; i--)
			{
				int randomIndex = random.Next(i + 1);

				//Swap elements
				var tmp = array[i];
				array[i] = array[randomIndex];
				array[randomIndex] = tmp;
			}
		}

		public static int Max(int v1, int v2, int v3)
		{
			return v1 > v2 ? (v1 > v3 ? v1 : v3) : (v2 > v3 ? v2 : v3);
		}

		public static int Min(int v1, int v2, int v3)
		{
			return v1 < v2 ? (v1 < v3 ? v1 : v3) : (v2 < v3 ? v2 : v3);
		}

		public static int Min(int v1, int v2, int v3, int v4)
		{
			return Math.Min(Min(v1, v2, v3), v4);
		}

		/// <summary>
		/// Round double to integer.
		/// Note: uses different midpoint rounding than System.Math.Round()
		/// </summary>
		public static int Round(double val)
		{
			return val >= 0 ? (int)(val + 0.5) : (int)(val - 0.5);
		}

		/// <summary>
		/// Round value to nearest integer away from zero, i.e. up for positive numbers and down for negative numbers.
		/// </summary>
		public static int RoundAway(double val)
		{
			return (int)val + (1 - (int)((int)(val + 1) - val));
		}

		/// <summary>
		/// Round value to nearest integer towards zero, i.e. down for positive numbers and up for negative numbers.
		/// Basically a truncate.
		/// </summary>
		public static int RoundTowards(double val)
		{
			return (int)val;
		}

		/// <summary>
		/// Ceiling
		/// </summary>
		public static int Ceiling(double val)
		{
			return (int)Math.Ceiling(val);
		}

		/// <summary>
		/// Floor
		/// </summary>
		public static int Floor(double val)
		{
			return (int)Math.Floor(val);
		}

		/// <summary>
		/// Quadratic Bezier with control points [0, x1, 1]
		/// </summary>
		public static double QuadraticBezier(double x1, double t)
		{
			t = Clamp(t);

			var b = 2 * (1 - t) * t * x1;
			var c = t * t;

			return b + c;
		}

		/// <summary>
		/// Cubic Bezier with control points [0, x1, x2, 1]
		/// </summary>
		public static double CubicBezier(double x1, double x2, double t)
		{
			t = Clamp(t);

			var b = 3 * (1 - t) * (1 - t) * t * x1;
			var c = 3 * (1 - t) * t * t * x2;
			var d = t * t * t;

			return b + c + d;
		}

		/// <summary>
		/// Linear Interpolation, t between 0 and 1, returns value between y0 and y1
		/// </summary>
		public static double LinearInterpolation(double y0, double y1, double t)
		{
			t = Clamp(t);

			// Imprecise method which does not guarantee v = v1 when t = 1,
			// due to floating-point arithmetic error.
			return y0 + t * (y1 - y0);
		}

		/// <summary>
		/// Linear Interpolation, x between x0 and x1, returns value between y0 and y1
		/// </summary>
		public static double LinearInterpolation(double x0, double x1, double y0, double y1, double x)
		{
			x = Normalize(x0, x1, x);
			return y0 + (y1 - y0) * x;
		}

		/// <summary>
		/// Smooth Step, x between x0 and x1, returns between 0 and 1
		/// </summary>
		public static double SmoothStep(double x0, double x1, double x)
		{
			x = Normalize(x0, x1, x);
			return x * x * (3 - 2 * x);
		}

		/// <summary>
		/// Scale, bias and saturate x to 0..1 range
		/// </summary>
		public static double Normalize(double x0, double x1, double x)
		{
			return Clamp((x - x0) / (x1 - x0));
		}

		/// <summary>
		/// Scale, bias and saturate x to 0..1 range
		/// </summary>
		public static float Normalize(float x0, float x1, float x)
		{
			return Clamp((x - x0) / (x1 - x0));
		}

		/// <summary>
		/// Linear Interpolation, t must be between 0 and 1, returns value between y0 and y1
		/// </summary>
		public static float Lerp(float t, float v0, float v1)
		{
			return (1 - t) * v0 + t * v1;
		}

		/// <summary>
		/// Bilinear Interpolation
		/// </summary>
		public static float BiLerp(float x, float y,
			float x0y0, float x0y1, float x1y0, float x1y1,
			float x1, float x2, float y1, float y2)
		{
			x = Normalize(x1, x2, x);
			y = Normalize(y1, y2, y);

			float c0 = Lerp(x, x0y0, x1y0);
			float c1 = Lerp(x, x0y1, x1y1);

			return Lerp(y, c0, c1);
		}

		/// <summary>
		/// Trilinear Interpolation
		/// </summary>
		public static float TriLerp(float x, float y, float z,
			float x0y0z0, float x0y0z1, float x0y1z0, float x0y1z1,
			float x1y0z0, float x1y0z1, float x1y1z0, float x1y1z1,
			float x1, float x2, float y1, float y2, float z1, float z2)
		{
			x = Normalize(x1, x2, x);
			y = Normalize(y1, y2, y);
			z = Normalize(z1, z2, z);

			float c00 = Lerp(x, x0y0z0, x1y0z0);
			float c10 = Lerp(x, x0y1z0, x1y1z0);
			float c01 = Lerp(x, x0y0z1, x1y0z1);
			float c11 = Lerp(x, x0y1z1, x1y1z1);
			float c0 = Lerp(y, c00, c10);
			float c1 = Lerp(y, c01, c11);

			return Lerp(z, c0, c1);
		}
	}
}
