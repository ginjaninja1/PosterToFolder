
using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI;
using MediaBrowser.Model.Serialization;
using PosterToFolder.Storage;
using PosterToFolder.UI;
using PosterToFolder.UI.Config;
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


        public Plugin(IServerApplicationHost applicationHost, ILogManager logManager)
        {
            this.applicationHost = applicationHost;
            this.logger = logManager.GetLogger(this.Name);
            this.basicsStore = new BasicsOptionsStore(applicationHost, this.logger, this.Name);
        }

        public override string Description => "A Template Emby PLugin";
        public override Guid Id => new Guid("1E0C5960-DF19-4C22-AF9A-FA0FDC3EF649");
        public override string Name => "Emby Template";

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        public Stream GetThumbImage()
            => this.GetType().Assembly.GetManifestResourceStream(this.GetType().Namespace + ".thumb.png");

        public IReadOnlyCollection<IPluginUIPageController> UIPageControllers
        {
            get
            {
                if (this.pages == null)
                {
                    this.pages = new List<IPluginUIPageController>();

                    this.pages.Add(new MainPageController(this.GetPluginInfo(), this.applicationHost, this.basicsStore));

                }

                return this.pages.AsReadOnly();
            }
        }
    }
}