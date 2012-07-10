﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Globalization;

namespace Dwarrowdelf
{
	public sealed class IntPointConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return ((destinationType == typeof(string)) || base.CanConvertTo(context, destinationType));
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if ((destinationType == null) || !(value is IntPoint2) || destinationType != typeof(string))
				return base.ConvertTo(context, culture, value, destinationType);

			var rect = (IntPoint2)value;
			return rect.ToString();
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
				throw base.GetConvertFromException(value);

			string source = value as string;

			if (source == null)
				return base.ConvertFrom(context, culture, value);

			return IntPoint2.Parse(source);
		}
	}

	public sealed class IntPoint3DConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return ((destinationType == typeof(string)) || base.CanConvertTo(context, destinationType));
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if ((destinationType == null) || !(value is IntPoint3) || destinationType != typeof(string))
				return base.ConvertTo(context, culture, value, destinationType);

			var p = (IntPoint3)value;
			return p.ToString();
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
				throw base.GetConvertFromException(value);

			string source = value as string;

			if (source == null)
				return base.ConvertFrom(context, culture, value);

			return IntPoint3.Parse(source);
		}
	}

	public sealed class IntGrid2Converter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return ((destinationType == typeof(string)) || base.CanConvertTo(context, destinationType));
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if ((destinationType == null) || !(value is IntGrid2) || destinationType != typeof(string))
				return base.ConvertTo(context, culture, value, destinationType);

			var rect = (IntGrid2)value;
			return rect.ToString();
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
				throw base.GetConvertFromException(value);

			string source = value as string;

			if (source == null)
				return base.ConvertFrom(context, culture, value);

			return IntGrid2.Parse(source);
		}
	}

	public sealed class IntGrid2ZConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return ((destinationType == typeof(string)) || base.CanConvertTo(context, destinationType));
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if ((destinationType == null) || !(value is IntGrid2Z) || destinationType != typeof(string))
				return base.ConvertTo(context, culture, value, destinationType);

			var rect = (IntGrid2Z)value;
			return rect.ToString();
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
				throw base.GetConvertFromException(value);

			string source = value as string;

			if (source == null)
				return base.ConvertFrom(context, culture, value);

			return IntGrid2Z.Parse(source);
		}
	}

	public sealed class IntBoxConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return ((destinationType == typeof(string)) || base.CanConvertTo(context, destinationType));
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if ((destinationType == null) || !(value is IntBox) || destinationType != typeof(string))
				return base.ConvertTo(context, culture, value, destinationType);

			var rect = (IntBox)value;
			return rect.ToString();
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
				throw base.GetConvertFromException(value);

			string source = value as string;

			if (source == null)
				return base.ConvertFrom(context, culture, value);

			return IntBox.Parse(source);
		}
	}

	public sealed class IntSize3DConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return ((destinationType == typeof(string)) || base.CanConvertTo(context, destinationType));
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if ((destinationType == null) || !(value is IntSize3) || destinationType != typeof(string))
				return base.ConvertTo(context, culture, value, destinationType);

			var rect = (IntSize3)value;
			return rect.ToString();
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
				throw base.GetConvertFromException(value);

			string source = value as string;

			if (source == null)
				return base.ConvertFrom(context, culture, value);

			return IntSize3.Parse(source);
		}
	}

	public sealed class ObjectIDConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return ((destinationType == typeof(string)) || base.CanConvertTo(context, destinationType));
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if ((destinationType == null) || !(value is ObjectID) || destinationType != typeof(string))
				return base.ConvertTo(context, culture, value, destinationType);

			var oid = (ObjectID)value;
			return oid.RawValue.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
				throw base.GetConvertFromException(value);

			string source = value as string;

			if (source == null)
				return base.ConvertFrom(context, culture, value);

			return new ObjectID(Convert.ToUInt32(source, System.Globalization.NumberFormatInfo.InvariantInfo));
		}
	}
}
