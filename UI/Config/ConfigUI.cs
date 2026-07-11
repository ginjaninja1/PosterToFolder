using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.List;
using MediaBrowser.Model.Attributes;
using System.Collections.Generic;
using System.ComponentModel;

namespace PosterToFolder.UI.Config
{
    public class ConfigUI : EditableOptionsBase
    {
        public override string EditorTitle => "Poster To Folder - Configuration";

        public override string EditorDescription =>
            "Copies each movie/show's poster image to folder.ext when a folder image is missing.";

        public CaptionItem GeneralHeading { get; set; } = new CaptionItem("General");

        [DisplayName("Enable Plugin")]
        [Description("When disabled, the scheduled task exits immediately without processing any items.")]
        [AutoPostBack("updateconfig", nameof(EnablePlugin))]
        public bool EnablePlugin { get; set; } = true;

        public CaptionItem LibraryFilterHeading { get; set; } =
            new CaptionItem("Library / Path Filters");


        /// <summary>
        /// Persistent configuration data.
        /// </summary>
        [Browsable(false)]
        public List<LibraryPathFilterItem> LibraryPaths { get; set; } =
            new List<LibraryPathFilterItem>();


        /// <summary>
        /// GenericUI representation of LibraryPaths.
        /// </summary>
        public GenericItemList LibraryList { get; set; } =
            new GenericItemList();
    }
}