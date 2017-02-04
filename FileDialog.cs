using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mekanik.Windows
{
	public static class FileDialog
	{
		public static string Open(params string[] _extensions)
		{
			OpenFileDialog d = new OpenFileDialog();
			d.Filter = "|" + string.Join(";", _extensions.Select(item => "*." + item));
			d.ShowDialog();

			return (d.FileName == "") ? null : d.FileName;
		}

		public static string Save(string _extension)
		{
			SaveFileDialog d = new SaveFileDialog();
			d.Filter = "." + _extension + "-files|*." + _extension;
			d.ShowDialog();

			return (d.FileName == "") ? null : d.FileName;
		}

		public static bool AskYesNo(string _message)
		{
			return MessageBox.Show(_message, "", MessageBoxButtons.YesNo) == DialogResult.Yes;
		}
	}
}