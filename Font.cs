using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mekanik
{
	public class Font : FontBase
	{
		private System.Drawing.Font _Font;
		private System.Drawing.Bitmap _Bitmap;
		private System.Drawing.Graphics _Graphics;
		private bool _Initialized;

		public Font(string _name, double _charsize)
			: base(_name, _charsize)
		{
			this._Font = new System.Drawing.Font(_name, (float)(_charsize / 16 * 12));

			this._Bitmap = new System.Drawing.Bitmap(200, 50);
			this._Graphics = System.Drawing.Graphics.FromImage(this._Bitmap);
		}

		protected override void _ChangeCharSize(double _charsize)
		{
			this._Font = new System.Drawing.Font(this.Name, (float)(_charsize / 16 * 12));
		}

		public override Point GetSize(string _content, int _tablength = 4)
		{
			Point @out = this._GetSize(_content, _tablength);
			if (_content == "" || _content == null)
				@out.X = 0;
			return @out;
		}

		private Point _GetSize(string _content, int _tablength = 4)
		{
			Point @out;

			string c = _content ?? "";

			if (c == "")
				@out = this.GetSize("X") * new Vector(0.2, 1);
			else
			{
				string tab = "";
				for (int i = 0; i < _tablength; i++)
					tab += " ";

				while (c.Contains('\t'))
					c = c.Replace("\t", tab);

				Bunch<string> lines = c.Split('\n');
				double maxwidth = 0;
				for (int i = 0; i < lines.Count; i++)
				{
					string l = lines[i];
					if (l.Length > 0 && l[l.Length - 1] == ' ')
						l = l.Substring(0, l.Length - 1) + '_';
					double width = this._Graphics.MeasureString(l, this._Font).Width;
					if (lines[i].Length == 0)
						width /= 4;
					if (width > maxwidth)
						maxwidth = width;
				}

				System.Drawing.SizeF s = new System.Drawing.SizeF((float)maxwidth, (float)(this.CharSize * lines.Count));
				@out = new Point(Meth.Up(s.Width), Meth.Up(s.Height + (this._Graphics.MeasureString("X", this._Font).Height - this.CharSize)));
			}

			return @out;
		}

		public override ImageSource GetImage(string _content, Color _color, int _tablength = 4, string _symboltab = null, char? _symbolspace = null)
		{
			Point s = this.GetSize(_content);

			if (!this._Initialized || this._Bitmap.Size.ToMekanik() != new Point(Meth.Up(s.X), Meth.Up(s.Y)))
			{
				this._Initialized = true;

				this._Bitmap.Dispose();
				this._Bitmap = new System.Drawing.Bitmap(Meth.Max(Meth.Up(s.X), 1), Meth.Max(Meth.Up(s.Y), 1));
				this._Graphics.Dispose();
				this._Graphics = System.Drawing.Graphics.FromImage(this._Bitmap);
			}

			ImageSource @out = new ImageSource(s.X, s.Y);

			this._Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			this._Graphics.Clear(System.Drawing.Color.Transparent);
			
			string c = _content ?? "";

			if (_symbolspace != null)
			{
				while (c.Contains(" "))
					c = c.Replace(' ', _symbolspace.Value);
			}

			string tab = "";
			int len = 0;
			if (_symboltab != null)
			{
				tab = _symboltab;
				len = _symboltab.Length;
			}
			for (int i = 0; i < _tablength - len; i++)
				tab += " ";
			while (c.Contains('\t'))
				c = c.Replace("\t", tab);



			this._Graphics.DrawString(c, this._Font, new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(_color.A, _color.R, _color.G, _color.B)), 0, 0);

			System.Drawing.Imaging.BitmapData data = this._Bitmap.LockBits(new System.Drawing.Rectangle(0, 0, this._Bitmap.Width, this._Bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, @out.TextureId);
			OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, this._Bitmap.Width, this._Bitmap.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, data.Scan0);

			this._Bitmap.UnlockBits(data);

			return @out;
		}
	}
}