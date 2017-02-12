using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Meka;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace Mekanik
{
	public class Game : GameBase
	{
		public bool AllowDebug = true;
		public bool AllowF8 = true;
		public bool AllowF9 = true;
		public bool AllowF10 = true;
		public bool AllowF11 = true;
		public bool AllowF12 = true;


		private int _SkipF11;
		public int MouseStillCount;
		public bool HideMouseOnFullscreen = true;



		public bool IgnoreMouseWithoutFocus = true;
		
		private Point _PositionBeforeFullscreen;
		private Point _SizeBeforeFullscreen;
		private bool _MaximizedBeforeFullscreen;
		
		private GameWindow _GameWindow;
		
		public Mekanik.Windows.Screen CurrentScreen
		{
			get
			{
				Point p = this.Position + this.Size / 2;
				return Mekanik.Windows.Screen.AllScreens.First(item => item.Rect.Contains(p));
			}
		}
		public Point Position
		{
			get { return this._GameWindow.Location.ToMekanik(); }
			set { this._GameWindow.Location = value.ToOpenTK(); }
		}
		public bool Maximized
		{
			get { return this._GameWindow.WindowState == WindowState.Maximized; }
			set { this._GameWindow.WindowState = value ? WindowState.Maximized : WindowState.Normal; }
		}
		private bool _Fullscreen;

		protected override bool _GetFullscreen()
		{
			return this._Fullscreen;
		}

		protected override void _SetFullscreen(bool _value)
		{
			if (_value != this._Fullscreen)
			{
				this._Fullscreen = _value;

				if (_value)
				{
					this._MaximizedBeforeFullscreen = this.Maximized;
					this.Maximized = false;
					this._GameWindow.WindowBorder = WindowBorder.Hidden;

					Mekanik.Windows.Screen s = this.CurrentScreen;

					this._PositionBeforeFullscreen = this.Position;
					this.Position = new Point((int)s.Rect.X, (int)s.Rect.Y);

					this._SizeBeforeFullscreen = this.Size;
					this.Size = new Point((int)s.Rect.Width, (int)s.Rect.Height + 1);

					if (HideMouseOnFullscreen)
						this.MouseStillCount = 60;
				}
				else
				{
					this.Size = this._SizeBeforeFullscreen;
					this.Position = this._PositionBeforeFullscreen;

					this.Maximized = this._MaximizedBeforeFullscreen;
					this._GameWindow.WindowBorder = WindowBorder.Resizable;
				}
			}
		}

		//private bool _IsFocused;
		//public bool IsFocused
		//{
		//	get { return this._IsFocused; }
		//}
		//public Bunch<File> Files
		//{
		//	get { return File.GetAllFiles(this.Location, _relativeto: this.Location).Where(item => item.FileName != "Thumbs.db"); }
		//}
		private bool _EnableVSync;
		public bool EnableVSync
		{
			get { return this.EnableVSync; }

			set
			{
				this._GameWindow.VSync = value ? VSyncMode.On : VSyncMode.Off;
				this._EnableVSync = value;
			}
		}
		private bool _CursorVisible = true;
		public bool CursorVisible
		{
			get { return this._CursorVisible; }

			set
			{
				if (value != this._CursorVisible)
				{
					this._CursorVisible = value;
					if (value)
						Cursor.Show();
					else
						Cursor.Hide();
				}
			}
		}

		private Point _RealSize;
		protected override Point _GetSize() => (this._GameWindow.Width == 0) ? this._RealSize : new Point(this._GameWindow.Width, this._GameWindow.Height);
		protected override void _SetSize(Point _size) => this._GameWindow.ClientSize = new System.Drawing.Size(_size.X, _size.Y);

		protected override void _SetTitle(string _value) => this._GameWindow.Title = _value;

		protected override void OnError(MekanikalError _error)
		{
			File.Write(this.Path + "\\CRASHLOG.txt", "Type: " + _error.Type + "\n\n" + _error.Message + "\n\nStackTrace:" + _error.Stack);
			MessageBox.Show("An error has occurred. Please contact the developers and send them the CRASHLOG.txt file along with a description of what you did prior to the crash.");
		}

		public Game(int _width, int _height, bool _fixedresolution = false, int _scale = 1, int _antialias = 0, bool _visible = true, bool _isgame = true)
			: base(_width, _height, _fixedresolution, _antialias, _isgame, Platform.Windows)
		{
			GameBase.LoadImageSource = bytes =>
				{
					ImageSource @out = new ImageSource();
					System.Drawing.Bitmap b = new System.Drawing.Bitmap(new System.IO.MemoryStream(bytes));
					System.Drawing.Imaging.BitmapData data = b.LockBits(new System.Drawing.Rectangle(0, 0, b.Width, b.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					@out.SetData(b.Width, b.Height, data.Scan0);
					b.UnlockBits(data);
					return @out;
				};

			GameBase.LoadFont = (name, charsize) => new Font(name, charsize);
			GameBase.GifUnuglify = cs => Mekanik.Windows.GifEncoder.Unuglify(cs);
			GameBase.GifEncode = fs => Mekanik.Windows.GifEncoder.Encode(fs, 50);
			GameBase.ImageToBytes = img => Mekanik.Windows.GifEncoder.ImageToBytes(img);
			//GameBase.ImageToBytesRgb = img => Mekanik.Windows.GifEncoder.ImageToBytesRgb(img);

			this._GameWindow = this._GetWindow(_width, _height);
			this.Title = "Mekanik Game";

			if (!_isgame)
			{
				this.AllowDebug = false;
				this.AllowF8 = false;
				this.AllowF9 = false;
				this.AllowF10 = false;
				this.AllowF11 = false;
				this.AllowF12 = false;
				this.IgnoreMouseWithoutFocus = false;
			}

			this._Initialize();
		}

		private bool _Closed;
		private bool _Closing;
		private int _SkipUpdatesBeyond60;

		public virtual bool OnCloseRequest() => true;

		private GameWindow _GetWindow(int _width, int _height)
		{
			GameWindow @out = new GameWindow(_width, _height, new GraphicsMode(new ColorFormat(32), 0, 0, 0));

			@out.Load += (sender, args) =>
				{
					this._LoadAreasets();

					this._OnStart();
				};
			
			@out.Closing += (sender, args) =>
				{
					if (!this._Closed)
					{
						if (this.OnCloseRequest())
							this._Closing = true;
						args.Cancel = true;
					}
					else
						this._Running = false;
				};

			@out.Resize += (sender, args) =>
				{
					if (this._GameWindow.Width != 0)
						this._RealSize = new Point(this._GameWindow.Width, this._GameWindow.Height);
					this._RendererOutput.Resolution = new Point(this._GameWindow.Width, this._GameWindow.Height);
				};

			int runtime = 0;
			
			@out.UpdateFrame += (sender, args) =>
				{
					if (this._SkipUpdatesBeyond60 > 0)
						runtime++;

					if (this._SkipUpdatesBeyond60 > 0 && runtime % 2 == 0)
						this._SkipUpdatesBeyond60--;
					else
					{
						if (!this._Closing)
							this.UpdateAll();
						else
						{
							this._Closed = true;
							this._GameWindow.Close();
						}
					}

					if (this._RenderSync)
						this.Render();
				};

			@out.RenderFrame += (sender, e) =>
				{
					if (!this._RenderSync)
						this.Render();
				};

			@out.MouseDown += (object sender, MouseButtonEventArgs args) => this._HandleKeyPress(args.Button.ToMekanik(), _pressed: true);
			@out.MouseUp += (object sender, MouseButtonEventArgs args) => this._HandleKeyPress(args.Button.ToMekanik(), _pressed: false);

			@out.MouseWheel += (object sender, MouseWheelEventArgs args) =>
				{
					if (!this._MouseWheelUsed)
					{
						this._MouseWheelUsed = true;
						this._MouseWheelCount += args.Delta;
					}
				};

			@out.KeyDown += (object sender, KeyboardKeyEventArgs args) =>
				{
					Key k = args.Key.ToMekanik();

					if (k == Key.F1 && this.AllowDebug && this._DebugOverlayVisible)
						this.DoDebugAction();
					if (k == Key.F2 && this.AllowDebug && this._DebugOverlayVisible)
						this._DebugPage = (this._DebugPage + this._DebugPageMax - 1) % this._DebugPageMax;
					if (k == Key.F3 && this.AllowDebug)
						this._DebugOverlayVisible = !this._DebugOverlayVisible;
					if (k == Key.F4 && this.AllowDebug && this._DebugOverlayVisible)
						this._DebugPage = (this._DebugPage + 1) % this._DebugPageMax;

					if (k == Key.F8 && this.AllowF8)
						this.DoRecord();

					if (k == Key.F9 && this.AllowF9)
						this.AutoRegulateRenderSync = !this.AutoRegulateRenderSync;
					if (k == Key.F10 && this.AllowF10)
					{
						int sc = ((int)this.ScaleMode + 1) % 4;
						if (sc == 0)
							sc = 1;
						this.ScaleMode = (ScaleMode)sc;
					}
					if (k == Key.F11 && this.AllowF11)
						this.Fullscreen = !this.Fullscreen;
					if (k == Key.F12 && this.AllowF12)
						this._ScreenshotRequested = true;

					this._HandleKeyPress(k, _pressed: true);
				};

			@out.KeyUp += (object sender, KeyboardKeyEventArgs args) => this._HandleKeyPress(args.Key.ToMekanik(), _pressed: false);

			@out.KeyPress += (object sender, OpenTK.KeyPressEventArgs args) =>
				{
					Entity focused = this.RealEntities.First(item => item.IsFocused);
					if (focused != null)
						focused.OnTextInput(args.KeyChar.ToString());
				};

			return @out;
		}

		private bool _MouseWheelUsed;
		private int _MouseWheelCount;

		private void _LoadAreasets()
		{
			if (this.AreasetFolder != null)
			{
				Renderer r = new Renderer(1, 1);
				foreach (string folder in File.GetFolders(this.AreasetFolder))
				{
					Areaset a = new Areaset(folder, this.TileSize, this.TileCollisionResolution);
					this.Areasets[a.Name] = a;
					r.Draw(a.Tiles.Select(item => (Graphic)(new Image(item))));
				}
			}
		}
		
		private void _UpdateSize(Point _size)
		{
			this._RendererOutput.Resolution = _size;

			if (this.UseMultipleRenderers && !this.FixedResolution)
			{
				foreach (Renderer r in this.Renderers)
					r.Resolution = _size;
			}
		}
		
		protected override Point _GetMousePositionRaw()
		{
			if (this.IsRunning)
			{
				MouseState state = Mouse.GetCursorState();
				System.Drawing.Point pt = this._GameWindow.PointToClient(new System.Drawing.Point(state.X, state.Y));
				return (new Vector(pt.X, pt.Y) - this._GetScreenPosition()) / this._GetScreenScale();
			}
			else
				return new Point(0, 0);
		}

		protected override void _UpdateEvents()
		{
			this._GameWindow.ProcessEvents();

			if (this._SkipF11 > 0)
				this._SkipF11--;
			
			if (this._MouseWheelCount != 0)
			{
				this._HandleKeyPress((this._MouseWheelCount > 0) ? Key.MouseUp : Key.MouseDown, _pressed: true, _once: true);
				this._MouseWheelCount -= Meth.Sign(this._MouseWheelCount);
				this._MouseWheelUsed = false;
			}
		}
		
		protected override void _AfterRender()
		{
			if (this.IsRunning)
				this._GameWindow.SwapBuffers();
		}
		
		protected override bool _DoSettingsExist() => File.Exists("Internal\\Settings.meka");

		protected override List<MekaItem> _GetPlatformSettingsDefault()
		{
			List<MekaItem> @out = new List<MekaItem>();
			@out.Add(new MekaItem("Fullscreen", "Normal"));
			return @out;
		}

		protected override List<MekaItem> _GetPlatformSettings()
		{
			List<MekaItem> @out = new List<MekaItem>();

			MekaItem fullscreen = new MekaItem("Fullscreen", "Normal");
			if (this.Maximized)
				fullscreen.Content = "Maximized";
			if (this.Fullscreen)
				fullscreen.Content = "Fullscreen";
			@out.Add(fullscreen);

			return @out;
		}

		protected override void _SetPlatformSettings(MekaItem _settings)
		{
			if (_settings["Fullscreen"].Content == "Maximized")
				this.Maximized = true;
			else if (_settings["Fullscreen"].Content == "Fullscreen")
				this.Fullscreen = true;
		}

		protected override MekaItem _LoadSettingsFile() => MekaItem.LoadFromFile(this.Path + "\\Internal\\Settings.meka");

		protected override void _SaveSettingsFile(MekaItem _settings)
		{
			File.CreateFolder(this.Path + "\\Internal");
			_settings.SaveToFile(this.Path + "\\Internal\\Settings.meka");
		}
		
		public sealed override void Close()
		{
			this._Closed = true;
			this._Closing = true;
			this._GameWindow.Close();
		}
		
		//public Vector GetPointOnScreen(Vector _pos) => _pos - (Camera - Resolution / 2) * this.RealZoom;

		public void Run()
		{
			try
			{
				this._GameWindow.Run(60);
				this._OnClose();
			}
			catch (Exception _ex) when (this.ThrowError(_ex)) { }
		}
		
		public override File SaveFile(byte[] _bytes, string _extension)
		{
			DateTime d = DateTime.Now;
			IGraphicsContext c = GraphicsContext.CurrentContext;

			string p = Mekanik.Windows.FileDialog.Save(_extension);
			if (p != null)
				File.Write(this.SavePath = p, _bytes);

			this._SkipUpdatesBeyond60 = (int)(Meth.Pow(0.9, Meth.Max((DateTime.Now - d).TotalSeconds, 0.5)) * 60);
			c.MakeCurrent(this._GameWindow.WindowInfo);

			return (p == null) ? null : new File(p);
		}
		
		public override File OpenFile(params string[] _extensions)
		{
			DateTime d = DateTime.Now;
			IGraphicsContext c = GraphicsContext.CurrentContext;

			File @out = null;
			
			string p = Mekanik.Windows.FileDialog.Open(_extensions);

			if (p != null)
				@out = new File(p);

			this._SkipUpdatesBeyond60 = (int)(Meth.Pow(0.9, Meth.Max((DateTime.Now - d).TotalSeconds, 0.5)) * 60);
			c.MakeCurrent(this._GameWindow.WindowInfo);

			return @out;
		}

		public override object GetClipboard() => Clipboard.GetText();
		public override void SetClipboard(object _value) => Clipboard.SetText((string)_value);
	}
}