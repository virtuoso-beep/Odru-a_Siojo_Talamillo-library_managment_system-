using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Library_Management_System
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Set to false to show login screen first, true for direct dashboard access (debug mode)
            bool debugMode = false;

            if (debugMode)
            {
                Application.Run(new DashboardForm());
            }
            else
            {
                Application.Run(new SiginForm());
            }
        }
    }
}
