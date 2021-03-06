﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iBlogs.Site.Core.Blog.Content;
using iBlogs.Site.Core.Blog.Content.DTO;
using iBlogs.Site.Core.Blog.Content.Service;
using iBlogs.Site.Core.Blog.Extension.Dto;
using iBlogs.Site.Core.Common.Extensions;
using iBlogs.Site.Core.Option;
using iBlogs.Site.Core.Option.Service;
using iBlogs.Site.Core.Storage;
using iBlogs.Site.MetaWeblog.Classes;
using iBlogs.Site.MetaWeblog.Wrappers;
using Microsoft.Extensions.Logging;

namespace iBlogs.Site.Core.Blog.Extension
{
    public class CnBlogsSyncExtension : IBlogsSyncExtension
    {
        private readonly IOptionService _optionService;
        private readonly IRepository<BlogSyncRelationship> _repository;
        private readonly ILogger<CnBlogsSyncExtension> _logger;
        private readonly IContentsService _contentsService;
        private ICnBlogsWrapper _cnBlogsWrapper;
        private readonly Lazy<List<CategoryInfo>> _categoryInfos;

        public CnBlogsSyncExtension(IOptionService optionService, IRepository<BlogSyncRelationship> repository, ILogger<CnBlogsSyncExtension> logger, IContentsService contentsService)
        {
            _optionService = optionService;
            _repository = repository;
            _logger = logger;
            _contentsService = contentsService;
            _categoryInfos = new Lazy<List<CategoryInfo>>(() => _cnBlogsWrapper.GetCategories().ToList());
        }
        public async Task Sync(BlogSyncContext context)
        {
            if (context.Target != BlogSyncTarget.All && context.Target != BlogSyncTarget.CnBlogs)
                return;

            if (!_optionService.Get(ConfigKey.CnBlogsSyncSwitch, "false").ToBool())
                return;

            var userName = _optionService.Get(ConfigKey.CnBlogsUserName);
            var passWord = _optionService.Get(ConfigKey.CnBlogsPassword);
            var url = _optionService.Get(ConfigKey.CnBlogsUrl);
            if (userName.IsNullOrWhiteSpace() || passWord.IsNullOrWhiteSpace())
            {
                var errorMessage = "同步数据到博客园前请先设置博客园用户名和密码";
                _logger.LogError(errorMessage);
                context.Success = false;
                context.Message = errorMessage;
                return;
            }

            _cnBlogsWrapper = new CnBlogsWrapper(url, userName, passWord);


            switch (context.Method)
            {
                case BlogSyncMethod.AddOrUpdate:
                    await AddOrUpdate(context);
                    break;
                case BlogSyncMethod.Delete:
                    Delete(context);
                    break;
                case BlogSyncMethod.Download:
                    await SyncToLocal();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task InitializeSync()
        {
            await Task.CompletedTask;
        }

        public BlogExtensionDashboardResponse GetDashBoardData()
        {
            //TODO GetAllIncluding(u => u.Content)
            var allResponse = _repository.GetAll().Where(u => u.Target == BlogSyncTarget.CnBlogs)
                .Select(r => new BlogExtensionContentResponse
                {
                    ContentId = r.ContentId,
                    ContentTitle = r.Content.Title,
                    ExtensionProperty = r.ExtensionProperty,
                    Message = r.Message,
                    Id = r.Id,
                    SyncData = r.SyncData,
                    Target = r.Target,
                    TargetPostId = r.TargetPostId,
                    Successful = r.Successful
                });
            return new BlogExtensionDashboardResponse
            {
                Successful = allResponse.Where(u => u.Successful).ToArray(),
                Failed = allResponse.Where(u => !u.Successful).ToArray()
            };
        }

        private async Task AddOrUpdate(BlogSyncContext context)
        {
            var relationship = _repository.GetAll().Where(u => u.Target == BlogSyncTarget.CnBlogs)
                  .FirstOrDefault(u => u.ContentId == context.Post.Id);
            if (relationship == null)
            {
                await Add(context);
            }
            else
            {
                await Update(context, relationship);
            }
        }

        private async Task Add(BlogSyncContext context)
        {
            var post = context.Post;
            var categories = await Task.FromResult(GetCategories(post));
            var postRequest = new Post { DateCreated = DateTime.Now, Description = post.Content, Title = post.Title };
            if (categories != null)
                postRequest.Categories = categories;

            var postId = _cnBlogsWrapper.NewPost(postRequest, false);
            await _repository.InsertAsync(new BlogSyncRelationship
            { ContentId = post.Id, SyncData = DateTime.Now, Target = BlogSyncTarget.CnBlogs, TargetPostId = postId });
            context.Success = true;
        }

        private async Task Update(BlogSyncContext context, BlogSyncRelationship relationship)
        {
            var post = context.Post;
            var categories = await Task.FromResult(GetCategories(post));
            var postRequest = new Post { DateCreated = DateTime.Now, Description = post.Content, Title = post.Title };
            if (categories != null)
                postRequest.Categories = categories;
            _cnBlogsWrapper.EditPost(relationship.TargetPostId, postRequest, false);
            context.Success = true;
        }

        private void Delete(BlogSyncContext context)
        {
            var relationship = _repository.GetAll().Where(u => u.Target == BlogSyncTarget.CnBlogs).FirstOrDefault(u => u.ContentId == context.Post.Id);
            if (relationship == null)
            {
                var errorMessage = "该博客未同步到博客园,无法删除";
                context.Message = errorMessage;
                _logger.LogError(errorMessage);
                return;
            }

            _cnBlogsWrapper.DeletePost(relationship.TargetPostId, false);
            context.Success = true;

        }

        private async Task SyncToLocal()
        {

            var allPostInfo = _cnBlogsWrapper.GetRecentPosts(Int32.MaxValue);
            foreach (var post in allPostInfo.OrderBy(u => u.DateCreated))
            {
                var relationship = _repository.GetAll().FirstOrDefault(u => u.TargetPostId == post.PostId.ToString());
                var input = new ContentInput()
                {
                    AllowComment = true,
                    AllowFeed = true,
                    AllowPing = true,
                    Categories = post.Categories != null ? string.Join(",", post.Categories) : null,
                    Content = post.Description,
                    Title = post.Title,
                    Tags = post.MtKeywords,
                    Status = ContentStatus.Publish
                };
                if (relationship != null)
                {
                    input.Id = relationship.ContentId;
                }
                else
                {
                    relationship = new BlogSyncRelationship
                    {
                        Target = BlogSyncTarget.CnBlogs,
                        TargetPostId = post.PostId.ToString()
                    };
                }

                relationship.ContentId = _contentsService.UpdateArticle(input);
                await _repository.InsertOrUpdateAsync(relationship);
            }

        }

        private string[] GetCategories(PostSyncDto post)
        {
            string[] categories = null;
            if (!post.Categories.IsNullOrWhiteSpace())
            {
                var titles = post.Categories.Split(',');
                categories = new string[titles.Length];
                for (int i = 0; i < titles.Length; i++)
                {
                    categories[i] = _categoryInfos.Value.FirstOrDefault(u => u.Title == titles[i])?.CategoryId;
                    if (categories[i] == null)
                    {
                        var categoryId = _cnBlogsWrapper.NewCategory(new WpCategory { Description = titles[i], Name = titles[i] }).ToString();
                        categories[i] = categoryId;
                        _categoryInfos.Value.Add(new CategoryInfo
                        {
                            CategoryId = categoryId,
                            Description = titles[i],
                            Title = titles[i]
                        });
                    }
                }

            }
            return categories;
        }
    }
}
