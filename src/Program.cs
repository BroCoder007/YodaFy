using System;
using System.Threading;
using System.Windows.Forms;

namespace YodaFy
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length >= 2 && args[0] == "--transform")
            {
                Console.WriteLine(YodaTransformer.Transform(args[1]));
                return;
            }
            bool created;
            using (Mutex single = new Mutex(true, "Local\\YodaFy.Desktop", out created))
            {
                if (!created) { MessageBox.Show("YodaFy is already running. Open it from the system tray.", "YodaFy"); return; }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
