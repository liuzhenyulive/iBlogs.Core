﻿
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iBlogs.Site.Core.Option;
using iBlogs.Site.Core.Option.Service;
using LibGit2Sharp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace iBlogs.Site.Core.Git
{
    public class GitFileService : IGitFileService
    {
        private readonly IOptionService _optionService;
        private readonly IGitBlogService _gitBlogService;
        private readonly ILogger<GitFileService> _logger;
        private readonly IHostingEnvironment _environment;
        private const string RepoPath = "Repo";

        public GitFileService(IOptionService optionService, IGitBlogService gitBlogService,
            ILogger<GitFileService> logger, IHostingEnvironment environment)
        {
            _optionService = optionService;
            _gitBlogService = gitBlogService;
            _logger = logger;
            _environment = environment;
        }

        public bool CloneOrPull(string branchName)
        {
            if (!Directory.Exists(RepoPath))
                Directory.CreateDirectory(RepoPath);
            try
            {
                if (!Directory.EnumerateFileSystemEntries(RepoPath).Any())
                {
                    var co = new CloneOptions
                    {
                        CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                        {
                            Username = _optionService.Get(ConfigKey.GitUerName),
                            Password = _optionService.Get(ConfigKey.GitPassword)
                        }
                    };
                    LibGit2Sharp.Repository.Clone(_optionService.Get(ConfigKey.GitProjectCloneUrl), RepoPath, co);
                }
                else
                {
                    Pull();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Clone执行失败,将删除仓库重新拉取");
                Directory.Delete(RepoPath, true);
                Directory.CreateDirectory(RepoPath);
                var co = new CloneOptions
                {
                    CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                    {
                        Username = _optionService.Get(ConfigKey.GitUerName),
                        Password = _optionService.Get(ConfigKey.GitPassword)
                    }
                };
                LibGit2Sharp.Repository.Clone(_optionService.Get(ConfigKey.GitProjectCloneUrl), RepoPath, co);
            }
            finally
            {
                CheckOutAndUpdate(branchName);
            }

            return true;
        }

        public async Task<bool> Handle(List<string> files, string branchName)
        {
            if (files == null || !files.Any())
                return false;
            var changed = false;
            foreach (var file in files)
            {
                var filePath = Path.Combine(_environment.ContentRootPath, RepoPath, file);
                if (!File.Exists(filePath))
                {
                    _logger.LogError($"文件{file}不存在");
                    continue;
                }

                if (await _gitBlogService.Handle(filePath))
                    changed = true;
            }

            if (changed)
                CommitAndPush(branchName);

            return true;
        }

        public bool CommitAndPush(string branchName)
        {
            using (var repo = new LibGit2Sharp.Repository(RepoPath))
            {
                Commands.Stage(repo, "*");

                // Create the committer's signature and commit
                var author = new Signature(_optionService.Get(ConfigKey.GitCommitter, "iBlogs"),
                    _optionService.Get(ConfigKey.GitUerName, "admin@iblogs.site"), DateTime.Now);
                var committer = author;

                // Commit to the repository
                repo.Commit($"iBlogs handle at {DateTime.Now.ToString(CultureInfo.InvariantCulture)}", author,
                    committer);

                var options = new PushOptions
                {
                    CredentialsProvider = (url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials()
                        {
                            Username = _optionService.Get(ConfigKey.GitUerName),
                            Password = _optionService.Get(ConfigKey.GitPassword)
                        }
                };
                repo.Network.Push(repo.Branches[branchName], options);
            }

            return true;
        }

        private void CheckOutAndUpdate(string branchName)
        {
            if (string.IsNullOrEmpty(branchName))
                branchName = "master";

            using (var repo = new LibGit2Sharp.Repository(RepoPath))
            {
                var trackingBranch = repo.Branches["remotes/origin/"+branchName];
                if (trackingBranch!=null&&trackingBranch.IsRemote)
                {
                    var branch = repo.Branches[branchName];
                    if (branch == null)
                    {
                        branch = repo.CreateBranch(branchName, trackingBranch.Tip);
                    }

                    repo.Branches.Update(branch, b => b.TrackedBranch = trackingBranch.CanonicalName);

                    Commands.Checkout(repo, branch, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
                }
            }
        }

        private void Pull()
        {
            using (var repo = new LibGit2Sharp.Repository(RepoPath))
            {
                // Credential information to fetch
                PullOptions options = new PullOptions
                {
                    FetchOptions = new FetchOptions
                    {
                        CredentialsProvider = (url, usernameFromUrl, types) =>
                            new UsernamePasswordCredentials()
                            {
                                Username = _optionService.Get(ConfigKey.GitUerName),
                                Password = _optionService.Get(ConfigKey.GitPassword)
                            }
                    }
                };

                // User information to create a merge commit
                var signature = new Signature(
                    new Identity("MERGE_USER_NAME", "MERGE_USER_EMAIL"), DateTimeOffset.Now);

                // Pull
                Commands.Pull(repo, signature, options);
            }
        }
    }
}
