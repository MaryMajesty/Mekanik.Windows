using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mekanik;

namespace Mekanik.Windows
{
	public class Screen
	{
		//public static int Width
		//{
		//	get { return (int)SFML.Window.VideoMode.DesktopMode.Width; }
		//}
		//public static int Height
		//{
		//	get { return (int)SFML.Window.VideoMode.DesktopMode.Height; }
		//}
		//public static Point Size
		//{
		//	get { return new Point(Screen.Width, Screen.Height); }
		//}

		public Rect Rect;

		internal Screen(System.Windows.Forms.Screen _screen)
		{
			this.Rect = new Rect(_screen.Bounds.X, _screen.Bounds.Y, _screen.Bounds.Width, _screen.Bounds.Height);
		}

		public static Screen[] AllScreens
		{
			get { return System.Windows.Forms.Screen.AllScreens.Select(item => new Screen(item)).ToArray(); }
		}
	}
}