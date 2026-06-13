using System;
using System.Windows.Forms;
using Reproductor_musical.Forms;
using Reproductor_musical.Controllers;

namespace Reproductor_musical
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var controller = new PlayerController();
            var view = new MainForm(controller);

            Application.Run(view);
        }
    }
}
