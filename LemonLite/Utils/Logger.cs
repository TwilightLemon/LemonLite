using System;
using System.IO;
using System.Text;

namespace LemonLite.Utils;

/// <summary>
/// 简单的日志记录器
/// </summary>
public static class Logger
{
    private static readonly object _lock = new();
    private static string LogFilePath => Path.Combine(Settings.MainPath, "log.txt");

    public static void Log(LogLevel level, string message, Exception? exception = null)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}");
            if (exception != null)
            {
                sb.AppendLine($"Exception: {exception.GetType().FullName}");
                sb.AppendLine($"Message: {exception.Message}");
                sb.AppendLine($"StackTrace: {exception.StackTrace}");
                if (exception.InnerException != null)
                {
                    sb.AppendLine($"InnerException: {exception.InnerException.Message}");
                    sb.AppendLine($"InnerStackTrace: {exception.InnerException.StackTrace}");
                }
            }
            sb.AppendLine();

            lock (_lock)
            {
                File.AppendAllText(LogFilePath, sb.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // 日志写入失败时忽略，避免循环异常
        }
    }

    public static void Info(string message) => Log(LogLevel.Info, message);
    public static void Warning(string message) => Log(LogLevel.Warning, message);
    public static void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
    public static void Fatal(string message, Exception? exception = null) => Log(LogLevel.Fatal, message, exception);
}

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Fatal
}
