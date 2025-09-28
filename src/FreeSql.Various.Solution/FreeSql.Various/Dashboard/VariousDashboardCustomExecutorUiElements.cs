using System.Text.Encodings.Web;
using System.Text.Json;
using FreeSql.Various.Dashboard.Models;

namespace FreeSql.Various.Dashboard
{
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

        /// <summary>
        /// 确认发送Post请求
        /// </summary>
        public void AfterConfirmRequest(ConfirmRequestConfig config)
        {
            SendMessageFunc(GenerateUiElements(VariousDashboardCustomExecutorUiElementsType.AfterConfirmRequest, new
            {
                router = config.Router,
                jsonBody = config.JsonBody,
                headers = config.Headers,
                contentStyle = config.ContentStyle,
                payload = config.Payload,
                title = config.ConfirmDialogTitle,
                content = config.ConfirmDialogContent,
            }));
        }

        /// <summary>
        /// 打开弹窗表单
        /// </summary>
        /// <param name="config"></param>
        public void ModalFromRequest(ModalFormRequestConfig config)
        {
            SendMessageFunc(GenerateUiElements(VariousDashboardCustomExecutorUiElementsType.ModalFromRequest, config));
        }

        public void Alert(string confirmDialogTitle, string confirmDialogContent, string confirmPayload)
        {
            SendMessageFunc(GenerateUiElements(VariousDashboardCustomExecutorUiElementsType.Alert, new
            {
                title = confirmDialogTitle,
                content = confirmDialogContent,
                payload = confirmPayload
            }));
        }

        public void Dialog(VariousExecutorDialogType type, string confirmDialogTitle, string confirmDialogContent)
        {
            string? name = Enum.GetName(type);
            //首字母小写
            name = name?.Substring(0, 1).ToLower() + name?.Substring(1);
            SendMessageFunc(GenerateUiElements(VariousDashboardCustomExecutorUiElementsType.Dialog, new
            {
                type = name,
                title = confirmDialogTitle,
                content = confirmDialogContent
            }));
        }

        public void ShowLoading(string progressMessage)
        {
            SendMessageFunc(GenerateUiElements(VariousDashboardCustomExecutorUiElementsType.ShowLoading,
                progressMessage));
        }

        public void OpenUrl(string url)
        {
            SendMessageFunc(GenerateUiElements(VariousDashboardCustomExecutorUiElementsType.OpenUrl,
                url));
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
                VariousDashboardCustomExecutorUiElementsType.OpenUrl => new
                    { type = "OpenUrl", body = message },
                VariousDashboardCustomExecutorUiElementsType.ModalFromRequest => new
                    { type = "ModalFromRequest", body = message },
                VariousDashboardCustomExecutorUiElementsType.Message => new { type = "Message", body = message },
                VariousDashboardCustomExecutorUiElementsType.Alert => new { type = "Alert", body = message },
                VariousDashboardCustomExecutorUiElementsType.AfterConfirmRequest => new
                    { type = "AfterConfirmRequest", body = message },
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
}