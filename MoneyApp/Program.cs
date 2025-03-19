// See https://aka.ms/new-console-template for more information
using FinanceApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new FinanceForm());
    }
}