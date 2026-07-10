using Emby.Web.GenericEdit;
using System.ComponentModel;

namespace PosterToFolder.UI.Config
{
    /// <summary>
    /// One row of the "Library Paths" filter list: a single physical path belonging to
    /// one of the server's libraries, with a toggle controlling whether items stored
    /// under it are processed by the Poster To Folder scheduled task.
    /// This list is auto-populated/synced from <c>ILibraryManager.GetVirtualFolders()</c>
    /// every time the task runs, so it always reflects the server's current libraries.
    /// </summary>
    public class LibraryPathFilterItem : EditableOptionsBase
    {
        public override string EditorTitle => this.LibraryName;

        [DisplayName("Library")]
        [Description("The Emby library this path belongs to.")]
        public string LibraryName { get; set; }

        [DisplayName("Path")]
        [Description("A physical folder path configured for this library.")]
        public string Path { get; set; }

        [DisplayName("Enabled")]
        [Description("When enabled, movies/shows stored under this path are included when the task runs. When disabled, they are skipped.")]
        public bool Enabled { get; set; } = true;
    }
}
