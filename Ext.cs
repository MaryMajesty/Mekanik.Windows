using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mekanik
{
	public static class Ext
	{
		public static Point ToMekanik(this System.Drawing.Point @this) => new Point(@this.X, @this.Y);

		public static System.Drawing.Point ToOpenTK(this Point @this) => new System.Drawing.Point(@this.X, @this.Y);

		public static Point ToMekanik(this System.Drawing.Size @this) => new Point(@this.Width, @this.Height);

		public static byte[] GetBytes(this System.Drawing.Bitmap @this, System.Drawing.Imaging.ImageFormat _format)
		{
			System.IO.MemoryStream s = new System.IO.MemoryStream();
			@this.Save(s, _format);
			return s.ToArray();
		}
	}
}