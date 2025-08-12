using System.Runtime.InteropServices;

namespace FreeSql.Various.Utilitys;

internal static class ConsoleHelper
{
    private const string Red = "\u001b[31m";
    private const string Normal = "\u001b[0m";
    private const string Green = "\u001b[32m";
    private const string Yellow = "\u001b[33m";
    private const string Magenta = "\u001b[35m";

    static ConsoleHelper()
    {
        // 自动按平台启用支持
        TryEnableVirtualTerminal();
    }

    public static void Info<T>(string text)
    {
        Console.Write(
            $"{Red}info: {Normal}{typeof(T).FullName}{Environment.NewLine}      {Normal}{text}{Environment.NewLine}");
    }

    public static void Warning<T>(string text)
    {
        Console.Write(
            $"{Yellow}warn: {Normal}{typeof(T).FullName}{Environment.NewLine}      {Normal}{text}{Environment.NewLine}");
    }

    public static void Error<T>(string text)
    {
        Console.Write(
            $"{Magenta}error: {Normal}{typeof(T).FullName}{Environment.NewLine}      {Normal}{text}{Environment.NewLine}");
    }


    private static void TryEnableVirtualTerminal()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            EnableWindowsVirtualTerminal();
        }
        // 其他平台无需操作
    }

    private static void EnableWindowsVirtualTerminal()
    {
        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        var handle = GetStdHandle(STD_OUTPUT_HANDLE);
        if (GetConsoleMode(handle, out uint mode))
        {
            SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}