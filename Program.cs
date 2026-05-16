using ClinicSystem;

namespace ClinicSystem
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            // This ensures Form1 is the starting point
            Application.Run(new Form1());
        }
    }
}