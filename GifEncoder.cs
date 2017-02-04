using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Mekanik.Windows
{
	public static class GifEncoder
	{
		private static byte[] GifAnimation = { 33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0 };
		private static byte[] Delay = { 255, 0 };
		
		public static byte[] Encode(Bunch<byte[]> _frames, double _delay)
		{
			MemoryStream s = new MemoryStream();
			BinaryWriter BW = new BinaryWriter(s);
			byte[] B = _frames[0];
			B[10] = (byte)(B[10] & 0X78);
			BW.Write(B, 0, 13);
			BW.Write(GifAnimation);
			WriteGifImg(B, BW, _delay);
			foreach (byte[] frame in _frames.SubBunch(1))
				WriteGifImg(frame, BW, _delay);
			BW.Write(B[B.Length - 1]);
			BW.Close();

			return s.ToArray();
		}

		public static void WriteGifImg(byte[] B, BinaryWriter BW, double _delay)
		{
			B[785] = (byte)Meth.Down(_delay / 10);
			B[786] = Delay[1];
			B[798] = (byte)(B[798] | 0X87);
			BW.Write(B, 781, 18);
			BW.Write(B, 13, 768);
			BW.Write(B, 799, B.Length - 800);
		}

		public static ColorPalette GetColorPalette(uint nColors)
		{
			ColorPalette palette;
			Bitmap bitmap = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
			palette = bitmap.Palette;

			bitmap.Dispose();

			return palette;
		}

		public static byte[] Unuglify(Color[,] _pixels)
		{
			int width = _pixels.GetLength(0);
			int height = _pixels.GetLength(1);

			Dictionary<Color, int> cs = new Dictionary<Color, int>();
			Dictionary<Color, Color> ts = new Dictionary<Color, Color>();
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					Color c = _pixels[x, y];
					if (!cs.ContainsKey(c))
						cs[c] = 0;
					cs[c]++;
				}
			}
			Bunch<Color> fs = cs.OrderBy(item => item.Value).Select(item => item.Key).ToBunch();

			while (fs.Count > 254)
			{
				Color c1 = fs[0];
				int mindis = 1000;
				Color t = fs[1];
				foreach (Color c2 in fs.SubBunch(2))
				{
					int dis = Meth.Abs((int)c2.R - (int)c1.R) + Meth.Abs((int)c2.G - (int)c1.G) + Meth.Abs((int)c2.B - (int)c1.B);
					if (dis < mindis)
					{
						mindis = dis;
						t = c2;
						if (dis == 1)
							break;
					}
				}
				ts[c1] = t;
				fs.RemoveAt(0);
			}
			while (fs.Count < 255)
				fs.Add(new Color(0, 0, 0, 0));

			ColorPalette pal = GetColorPalette((uint)fs.Count);
			for (int i = 0; i < fs.Count; i++)
				pal.Entries[i] = System.Drawing.Color.FromArgb(fs[i].A, fs[i].R, fs[i].G, fs[i].B);
			
			Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
			bitmap.Palette = pal;
			BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

			IntPtr pixels = bitmapData.Scan0;

			unsafe
			{
				byte* pBits;
				if (bitmapData.Stride > 0)
					pBits = (byte*)pixels.ToPointer();
				else
					pBits = (byte*)pixels.ToPointer() + bitmapData.Stride * (height - 1);
				uint stride = (uint)Math.Abs(bitmapData.Stride);

				for (uint y = 0; y < height; y++)
				{
					for (uint x = 0; x < width; x++)
					{
						Color pixel = _pixels[x, y];

						byte* p8bppPixel = pBits + y * stride + x;

						while (!fs.Contains(pixel))
							pixel = ts[pixel];

						*p8bppPixel = (byte)fs.IndexOf(pixel);

					}
				}
			}

			bitmap.UnlockBits(bitmapData);

			byte[] @out = bitmap.GetBytes(ImageFormat.Gif);
			bitmap.Dispose();

			return @out;
		}

		public static Bitmap ImageToBitmap(ImageSource _image)
		{
			Bitmap @out = new Bitmap(_image.Width, _image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			BitmapData bmData = @out.LockBits(new System.Drawing.Rectangle(0, 0, _image.Width, _image.Height), ImageLockMode.ReadWrite, @out.PixelFormat);
			IntPtr pNative = bmData.Scan0;
			System.Runtime.InteropServices.Marshal.Copy(_image.PixelBytesGbra, 0, pNative, _image.Width * _image.Height * 4);
			@out.UnlockBits(bmData);

			return @out;
		}

		public static Bitmap ImageToBitmapRgb(ImageSource _image)
		{
			Bitmap @out = new Bitmap(_image.Width, _image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			BitmapData bmData = @out.LockBits(new System.Drawing.Rectangle(0, 0, _image.Width, _image.Height), ImageLockMode.ReadWrite, @out.PixelFormat);
			IntPtr pNative = bmData.Scan0;
			System.Runtime.InteropServices.Marshal.Copy(_image.PixelBytesGbr, 0, pNative, _image.Width * _image.Height * 3);
			@out.UnlockBits(bmData);

			return @out;
		}

		public static byte[] ImageToBytes(ImageSource _image)
		{
			Bitmap b = ImageToBitmap(_image);
			byte[] @out = b.GetBytes(ImageFormat.Png);
			b.Dispose();
			return @out;
		}

		public static byte[] ImageToBytesRgb(ImageSource _image)
		{
			Bitmap b = ImageToBitmapRgb(_image);
			byte[] @out = b.GetBytes(ImageFormat.Png);
			b.Dispose();
			return @out;
		}
	}
}