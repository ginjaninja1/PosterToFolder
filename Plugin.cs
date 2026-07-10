using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI;
using PosterToFolder.Storage;
using PosterToFolder.UI;
using System;
using System.Collections.Generic;
using System.IO;

namespace PosterToFolder
{
    public class Plugin : BasePlugin, IHasThumbImage, IHasUIPages
    {
        private readonly IServerApplicationHost applicationHost;
        private readonly ILogger logger;
        private readonly BasicsOptionsStore basicsStore;

        private List<IPluginUIPageController> pages;


        public Plugin(
            IServerApplicationHost applicationHost,
            ILogManager logManager)
        {
            this.applicationHost = applicationHost;

            // Create the plugin logger once.
            this.logger = logManager.GetLogger(this.Name);

            this.basicsStore = new BasicsOptionsStore(
                applicationHost,
                this.logger,
                this.Name);

            Instance = this;
        }


        /// <summary>
        /// Gets the running instance of this plugin.
        /// </summary>
        public static Plugin Instance { get; private set; }


        /// <summary>
        /// Gets the config store.
        /// </summary>
        public BasicsOptionsStore ConfigStore => this.basicsStore;


        public override string Description =>
            "Copies poster.ext to folder.ext for movies and TV shows that are missing a folder image.";


        public override Guid Id =>
            new Guid("1E0C5960-DF19-4C22-AF9A-FA0FDC3EF649");


        public override string Name =>
            "Poster To Folder";


        public ImageFormat ThumbImageFormat =>
            ImageFormat.Png;


        public Stream GetThumbImage()
            => this.GetType()
                .Assembly
                .GetManifestResourceStream(
                    this.GetType().Namespace + ".thumb.png");


        public IReadOnlyCollection<IPluginUIPageController> UIPageControllers
        {
            get
            {
                if (this.pages == null)
                {
                    this.pages = new List<IPluginUIPageController>();

                    this.pages.Add(
                        new MainPageController(
                            this.GetPluginInfo(),
                            this.applicationHost,
                            this.basicsStore,
                            this.logger));
                }

                return this.pages.AsReadOnly();
            }
        }
    }
}