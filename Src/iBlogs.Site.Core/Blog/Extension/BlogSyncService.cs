﻿using System;
using System.Linq;
using iBlogs.Site.Core.Blog.Content;
using iBlogs.Site.Core.Blog.Extension.Dto;
using iBlogs.Site.Core.Common.Extensions;
using iBlogs.Site.Core.Common.Response;
using iBlogs.Site.Core.Option;
using iBlogs.Site.Core.Option.Service;
using iBlogs.Site.Core.Storage;
using Microsoft.Extensions.Logging;

namespace iBlogs.Site.Core.Blog.Extension
{
    public class BlogSyncService : IBlogSyncService
    {
        private readonly ILogger<BlogSyncService> _logger;
        private readonly IRepository<Contents> _contentRepository;
        private readonly IOptionService _optionService;
        private readonly IBlogsSyncExtension _blogsSyncExtension;

        public BlogSyncService(ILogger<BlogSyncService> logger, IRepository<Contents> contentRepository, IOptionService optionService, IBlogsSyncExtension blogsSyncExtension)
        {
            _logger = logger;
            _contentRepository = contentRepository;
            _optionService = optionService;
            _blogsSyncExtension = blogsSyncExtension;
        }

        public ApiResponse Sync(BlogSyncRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!Validation(request, out string errorMessage))
                return ApiResponse.Fail(errorMessage);

            var contexts = BuildContexts(request);

            foreach (var blogSyncContext in contexts)
            {
                _logger.LogInformation($"Publish Blog Sync Task,Cid:{request.Cid},Targets:{blogSyncContext.Target.ToString()}");
                _blogsSyncExtension.Sync(blogSyncContext);
            }

            return ApiResponse.Ok();
        }

        private bool Validation(BlogSyncRequest request, out string errorMessage)
        {
            errorMessage = null;
            if (request.Targets.Any(u => (u == BlogSyncTarget.All || u == BlogSyncTarget.CnBlogs)) && !ValidationForCnBlogs(out errorMessage))
                return false;
            return true;
        }

        private bool ValidationForCnBlogs(out string errorMessage)
        {
            errorMessage = null;
            var userName = _optionService.Get(ConfigKey.CnBlogsUserName);
            var passWord = _optionService.Get(ConfigKey.CnBlogsPassword);
            if (!userName.IsNullOrWhiteSpace() && !passWord.IsNullOrWhiteSpace()) return true;
            errorMessage = "同步数据到博客园前请先设置博客园用户名和密码";
            _logger.LogError(errorMessage);
            return false;
        }

        private BlogSyncContext[] BuildContexts(BlogSyncRequest request)
        {
            var postSyncDto = new PostSyncDto();
            if (request.Cid != 0)
            {
                var content = _contentRepository.FirstOrDefault(request.Cid);
                if (content == null)
                    throw new ArgumentException("传入的Cid有误");

                postSyncDto = new PostSyncDto
                {
                    Id = content.Id,
                    Categories = content.Categories,
                    Content = content.Content,
                    Status = content.Status,
                    Tags = content.Tags,
                    Title = content.Title
                };
            }

            return request.Targets.Select(t => new BlogSyncContext
            {
                Method = request.Method,
                Post = postSyncDto,
                Target = t
            }).ToArray();
        }
    }
}
