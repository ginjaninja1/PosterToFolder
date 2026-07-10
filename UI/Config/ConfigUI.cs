using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.List;
using MediaBrowser.Model.Attributes;
using System.ComponentModel;

namespace PosterToFolder.UI.Config
{
    public class ConfigUI : EditableOptionsBase
    {
        public override string EditorTitle => "Poster To Folder - Configuration";

        public override string EditorDescription => "Copies each movie/show's poster image to folder.ext when a folder image is missing.";

        public CaptionItem GeneralHeading { get; set; } = new CaptionItem("General");

        [DisplayName("Enable Plugin")]
        [Description("When disabled, the scheduled task exits immediately without processing any items.")]
        public bool EnablePlugin { get; set; } = true;

        public CaptionItem LibraryFilterHeading { get; set; } = new CaptionItem("Library / Path Filters");

        [DisplayName("Library Paths")]
        [Description("Auto-populated from your Emby libraries each time the task runs. Untick a path to exclude items stored under it. If this list is empty, all libraries/paths are processed.")]
        public EditableObjectCollection LibraryPaths { get; set; } = new EditableObjectCollection();
    }
}
