using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Elements;
using MediaBrowser.Model.Attributes;
using System.ComponentModel;

namespace PosterToFolder.UI.Config
{
    public class ConfigUI : EditableOptionsBase
    {
        public override string EditorTitle => "Configuration Settings";

        public override string EditorDescription => "Configurations Settings description";

        public CaptionItem GeneralHeading { get; set; } = new CaptionItem("System Diagnostics");

        [DisplayName("Hourly Engine Monitor Loop")]
        [Description("Run structural lock tasks cleanly across background cycles.")]
        public bool EnableSync { get; set; } = false;

        [DisplayName("Backup Destination Path")]
        [Description("Storage path where playlist configuration history logs are output.")]
        public string BackupPath { get; set; } = "C:\\Backups";
    }
}
