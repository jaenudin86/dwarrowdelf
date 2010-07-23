﻿using System;
using System.Windows.Media;

namespace MyGame.Client
{
	static class HSL
	{
		// Given H,S,L in range of 0-1
		// Returns a Color (RGB struct) in range of 0-255
		public static Color HSL2RGB(double h, double s, double l)
		{
			double v;
			double r, g, b;

			r = l;   // default to gray
			g = l;
			b = l;
			v = (l <= 0.5) ? (l * (1.0 + s)) : (l + s - l * s);
			if (v > 0)
			{
				double m;
				double sv;
				int sextant;
				double fract, vsf, mid1, mid2;

				m = l + l - v;
				sv = (v - m) / v;
				h *= 6.0;
				if (h >= 6.0)
					h -= 6.0;
				sextant = (int)h;
				fract = h - sextant;
				vsf = v * sv * fract;
				mid1 = m + vsf;
				mid2 = v - vsf;

				switch (sextant)
				{
					case 0:
						r = v;
						g = mid1;
						b = m;
						break;

					case 1:
						r = mid2;
						g = v;
						b = m;
						break;

					case 2:
						r = m;
						g = v;
						b = mid1;
						break;

					case 3:
						r = m;
						g = mid2;
						b = v;
						break;

					case 4:
						r = mid1;
						g = m;
						b = v;
						break;

					case 5:
						r = v;
						g = m;
						b = mid2;
						break;

					default:
						throw new Exception();

				}

			}

			Color rgb = new Color();
			rgb.R = Convert.ToByte(r * 255.0f);
			rgb.G = Convert.ToByte(g * 255.0f);
			rgb.B = Convert.ToByte(b * 255.0f);
			rgb.A = 255;

			return rgb;

		}

		// Given a Color (RGB Struct) in range of 0-255
		// Return H,S,L in range of 0-1
		public static void RGB2HSL(Color rgb, out double h, out double s, out double l)
		{
			double r = rgb.R / 255.0;
			double g = rgb.G / 255.0;
			double b = rgb.B / 255.0;
			double v;
			double m;
			double vm;
			double r2, g2, b2;

			h = 0; // default to black
			s = 0;
			l = 0;
			v = Math.Max(r, g);
			v = Math.Max(v, b);
			m = Math.Min(r, g);
			m = Math.Min(m, b);
			l = (m + v) / 2.0;

			if (l <= 0.0)
				return;

			vm = v - m;
			s = vm;
			if (s > 0.0)
				s /= (l <= 0.5) ? (v + m) : (2.0 - v - m);
			else
				return;

			r2 = (v - r) / vm;
			g2 = (v - g) / vm;
			b2 = (v - b) / vm;

			if (r == v)
				h = (g == m ? 5.0 + b2 : 1.0 - g2);
			else if (g == v)
				h = (b == m ? 1.0 + r2 : 3.0 - b2);
			else
				h = (r == m ? 3.0 + g2 : 5.0 - r2);

			h /= 6.0;
		}

	}
}
