﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iBlogs.Site.Core.Common;
using iBlogs.Site.Core.Option;
using iBlogs.Site.Core.Option.Service;
using iBlogs.Site.Core.Security.DTO;
using iBlogs.Site.Core.Security.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace iBlogs.Site.Core.Git
{
    public class GitEventBus : IGitEventBus
    {
        private readonly ILogger<GitEventBus> _logger;
        private readonly IGitFileService _gitFileService;
        private readonly IOptionService _optionService;
        private readonly IUserService _userService;

        public GitEventBus(ILogger<GitEventBus> logger, IGitFileService gitFileService, IOptionService optionService, IUserService userService)
        {
            _logger = logger;
            _gitFileService = gitFileService;
            _optionService = optionService;
            _userService = userService;
        }

        public async Task<bool> Publish(string message)
        {

            _logger.LogInformation($"send:{message}");
            await Receive(message);
            return true;
        }


        public async Task Receive(string message)
        {
            _logger.LogInformation($"receive:{message}");
            try
            {
                var gitAuthor = _userService.FindUserById(int.Parse(_optionService.Get(ConfigKey.GitAuthorId, "1")));
                _userService.CurrentUsers = new CurrentUser
                {
                    Uid = gitAuthor.Id,
                    Email = gitAuthor.Email,
                    HomeUrl = gitAuthor.HomeUrl,
                    ScreenName = gitAuthor.ScreenName,
                    Username = gitAuthor.Username
                };
                var gitRequest = JsonConvert.DeserializeObject<GitRequest>(message);
                if (gitRequest?.Commits == null)
                    return;
                var needHandleFile = new List<string>();

                foreach (var commit in gitRequest.Commits)
                {
                    if (commit.Committer.Name == _optionService.Get(ConfigKey.GitCommitter, "iBlogs"))
                    {
                        _logger.LogWarning($"commit:{commit.Message},committer:{commit.Committer.Name} will skip");
                        continue;
                    }
                    needHandleFile.AddRange(commit.Added);
                    needHandleFile.AddRange(commit.Modified);
                }
                if (!needHandleFile.Any())
                    return;

                BlogsTimer.AccelerateTo(TimeSpan.FromSeconds(10));

                //var branchName = gitRequest.Ref.Split('/').Last();

                //_gitFileService.CloneOrPull(branchName);
                //await _gitFileService.Handle(needHandleFile.Distinct().ToList(), branchName);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.ToString());
                throw;
            }

        }
    }
}
