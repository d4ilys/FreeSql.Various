using System.Text.Json.Serialization;

namespace FreeSql.Various.Dashboard.Models
{
    public class ModalFormRequestConfig
    {
        public string Title { get; set; }

        public string Router { get; set; }
        
        public Dictionary<string, string> ContentStyle { get; set; } = new();

        public Dictionary<string, string>? Headers { get; set; } = null;

        public List<ModalFormComponent> Components { get; set; } = new();
    }

    public class ModalFormComponent
    {
        public ModalFormComponentType Type { get; set; }

        public string Label { get; set; }

        public string Name { get; set; }

        public object? DefaultValue { get; set; }

        public List<object> Options { get; set; } = new();

        public FormRules Rules { get; set; } = new();
    }

    public class FormRules
    {
        [JsonPropertyName("required")] public bool Required { get; set; } = false;

        [JsonPropertyName("message")] public string? Message { get; set; } = string.Empty;

        [JsonPropertyName("trigger")] public string? Trigger { get; set; } = "blur";
    }

    public enum ModalFormComponentType
    {
        Text,
        TextArea,
        Select
    }
}