using FirmwareInstaller.Models;
using Octokit;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace FirmwareInstaller.Services.Update
{
    /// <summary>
    /// Allows to interface with a remotely stored version index and handles
    /// downloading and caching of versions.
    /// </summary>
    internal class DownloadService : BaseService
    {
        #region Constructor
        public DownloadService()
        {
            _appName = Assembly.GetExecutingAssembly().GetName().Name;
            _appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _downloadPath = Path.GetTempPath();
            _downloadUrls = new Dictionary<string, string>();
        }
        #endregion

        #region Events
        #endregion

        #region Consts
        private const string _githubUser = "t3knomanzer";
        private const string _githuRepo = "maxmix-software";
        private const string _targetExtension = ".hex";
        #endregion

        #region Read Only
        private readonly string _appName;
        private readonly string _appVersion;
        private readonly string _downloadPath;
        private readonly Dictionary<string, string> _downloadUrls;
        #endregion

        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods
        /// <summary>
        /// Retrieves a list of available versions found in the VersionIndexModel.
        /// </summary>
        /// <returns>List of versions available to download or an empty list if an error occurs.</returns>
        public async Task<IEnumerable<string>> RetrieveVersions()
        {
            var client = new GitHubClient(new ProductHeaderValue(_appName, _appVersion));
            var releases = await client.Repository.Release.GetAll(_githubUser, _githuRepo);

            foreach (var release in releases)
            {
                // Skip if it's a pre-release.
                if (release.Prerelease)
                    continue;

                // Skip if there aren't any assets matching the target file extension.
                if (!release.Assets.Any(o => o.Name.EndsWith(_targetExtension)))
                    continue;

                var version = release.TagName;
                var url = release.Assets.First(o => o.Name.EndsWith(_targetExtension)).BrowserDownloadUrl;
                _downloadUrls.Add(version, url);
            }

            return _downloadUrls.Keys.ToList();
        }

        /// <summary>
        /// Downloads the url for the given version in the VersionIndexModel.
        /// The version should have been returned by calling RetrieveVersions to make sure
        /// that it exists.
        /// </summary>
        /// <param name="version">The version to download.</param>
        /// <returns>The local absolute path to the downloaded file or an empty string if an error occurs.</returns>
        public Task<string> DownloadVersionAsync(string version)
        {

            return Task.Run<string>(() =>
            {
                if (!_downloadUrls.ContainsKey(version))
                {
                    RaiseError($"Version {version} does not exist.");
                    return string.Empty;
                }

                var url = _downloadUrls[version];
                
                var fileName = url.Split('/').LastOrDefault();
                var downloadFilePath = Path.Combine(_downloadPath, fileName);

                if (string.IsNullOrEmpty(fileName))
                {
                    RaiseError($"Error parsing url {url}.");
                    return string.Empty;
                }

                var webClient = new WebClient();
                try
                {
                    webClient.DownloadFile(url, downloadFilePath);
                }
                catch
                {
                    RaiseError($"Error downloading file {url} to {downloadFilePath}");
                    return string.Empty;
                }

                return downloadFilePath;
            });
        }
        #endregion

        #region Event Methods
        #endregion
    }
}
