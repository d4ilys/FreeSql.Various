using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreeSql.Various.Dashboard
{
    public class VariousDashboardCustomExecutor
    {
        /// <summary>
        /// 执行器Id
        /// </summary>
        public required string ExecutorId { get; set; }

        /// <summary>
        /// 执行器标题
        /// </summary>
        public required string ExecutorTitle { get; set; }

        /// <summary>
        /// 执行动作
        /// </summary>
        public required Func<VariousDashboardCustomExecutorUiElements, Task<bool>> Executor { get; set; }
    }

    public class VariousDashboardCustomExecutorUiElements
    {
        public Func<string, Task> SendMessageFunc { get; init; }

        /// <summary>
        /// 确认
        /// </summary>
        public void Notification(string title, string message, VariousExecutorNotificationType type,
            int duration = 3000)
        {
            string? name = Enum.GetName(type);
            //首字母小写
            name = name?.Substring(0, 1).ToLower() + name?.Substring(1);
            SendMessageFunc(GenerateUiElements(VariousDashboardCustomExecutorUiElementsType.Notification, new
            {
                type = name,
                title = title,
                message = message,
                duration = duration
            }));
        }

        public void Message(string message, VariousExecutorNotificationType type,
            int duration = 3000)
        {
            string? name = Enum.GetName(type);
            //首字母小写
            name = name?.Substring(0, 1).ToLower() + name?.Substring(1);
            SendMessageFunc(GenerateUiElements(VariousDashboardCustomExecutorUiElementsType.Message, new
            {
                type = name,
                message = message,
                duration = duration
            }));
        }

        public void ShowLoading(string progressMessage)
        {
            SendMessageFunc(GenerateUiElements(VariousDashboardCustomExecutorUiElementsType.ShowLoading,
                progressMessage));
        }

        public void HideLoading()
        {
            SendMessageFunc(GenerateUiElements(VariousDashboardCustomExecutorUiElementsType.HideLoading, null));
        }

        private string GenerateUiElements(VariousDashboardCustomExecutorUiElementsType type, object? message)
        {
            var json = type switch
            {
                VariousDashboardCustomExecutorUiElementsType.Notification => new
                    { type = "Notification", body = message },
                VariousDashboardCustomExecutorUiElementsType.Message => new { type = "Message", body = message },
                VariousDashboardCustomExecutorUiElementsType.Dialog => new { type = "Dialog", body = message },
                VariousDashboardCustomExecutorUiElementsType.ShowLoading =>
                    new { type = "ShowLoading", body = message },
                VariousDashboardCustomExecutorUiElementsType.HideLoading =>
                    new { type = "HideLoading", body = message },
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            return JsonSerializer.Serialize(json, new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }

    public enum VariousDashboardCustomExecutorUiElementsType
    {
        Notification,
        Message,
        Dialog,
        ShowLoading,
        HideLoading
    }

    public enum VariousExecutorNotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}