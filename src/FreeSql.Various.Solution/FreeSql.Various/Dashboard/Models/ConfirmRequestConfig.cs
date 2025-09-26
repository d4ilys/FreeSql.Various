using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql.Various.Dashboard.Models
{
    public class ConfirmRequestConfig
    {
        public string ConfirmDialogTitle { get; set; }
        public string ConfirmDialogContent { get; set; }
        public Dictionary<string, string> ContentStyle { get; set; } = new();
        public string Router { get; set; }
        public string JsonBody { get; set; }
        public Dictionary<string, string>? Headers { get; set; } = null;
        public string Payload { get; set; }
    }
}