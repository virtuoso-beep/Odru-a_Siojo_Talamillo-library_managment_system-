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
            // CHANGE THIS TO FALSE TO SHOW LOGIN SCREEN
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
