﻿using iBlogs.Site.Core.Common;
using iBlogs.Site.Core.Content;
using iBlogs.Site.Core.Content.Service;
using iBlogs.Site.Core.EntityFrameworkCore;
using iBlogs.Site.Core.Install.DTO;
using iBlogs.Site.Core.Meta.Service;
using iBlogs.Site.Core.Option.Service;
using iBlogs.Site.Core.User;
using iBlogs.Site.Core.User.Service;
using System;
using System.Threading;
using System.Threading.Tasks;
using iBlogs.Site.Core.Content.DTO;
using iBlogs.Site.Core.Meta;
using iBlogs.Site.Core.Option;
using Microsoft.AspNetCore.Hosting;
using ConfigKey = iBlogs.Site.Core.Option.ConfigKey;

namespace iBlogs.Site.Core.Install.Service
{
    public class InstallService : IInstallService
    {
        private readonly IOptionService _optionService;
        private readonly IUserService _userService;
        private readonly iBlogsContext _blogsContext;
        private readonly ITransactionProvider _transactionProvider;
        private readonly IContentsService _contentsService;
        private readonly IMetasService _metasService;
        private InstallParam _param;
        private Users _users;
        private readonly IApplicationLifetime _applicationLifetime;

        public InstallService(IOptionService optionService, IUserService userService, iBlogsContext blogsContext, ITransactionProvider transactionProvider, IContentsService contentsService, IMetasService metasService, IApplicationLifetime applicationLifetime)
        {
            _optionService = optionService;
            _userService = userService;
            _blogsContext = blogsContext;
            _transactionProvider = transactionProvider;
            _contentsService = contentsService;
            _metasService = metasService;
            _applicationLifetime = applicationLifetime;
        }

        public void WriteInstallInfo(InstallParam param)
        {
            var connectString = $"Server={param.DbUrl};Database={param.DbName};uid={param.DbUserName};pwd={param.DbUserPwd}";
            ConfigDataHelper.UpdateConnectionString("iBlogs", connectString);
            ConfigDataHelper.SaveInstallParam(param);
            Task.Run(() =>
            {
                Thread.Sleep(1000);
                _applicationLifetime.StopApplication();
            });
        }

        public async Task<bool> InitializeDb()
        {

            try
            {
                _param = ConfigDataHelper.ReadInstallParam();
                await _blogsContext.Database.EnsureCreatedAsync();
                Seed();
                ConfigDataHelper.UpdateDbInstallStatus(true);
                ConfigDataHelper.DeleteInstallParamFile();
                _optionService.Load();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }

        private void Seed()
        {
            using (var tra = _transactionProvider.CreateTransaction())
            {
                var result = CreateUser() && InitOptions() && InitContent();
                if (result)
                    tra.Commit();
                else
                    tra.Rollback();
            }
        }

        private bool CreateUser()
        {
            _users = new Users
            {
                Username = _param.AdminUser,
                Password = _param.AdminPwd,
                Email = _param.AdminEmail
            };
            return _userService.InsertUser(_users);
        }

        private bool InitOptions()
        {
            _optionService.Set(ConfigKey.AllowInstall, "false", "是否允许重新安装博客");
            _optionService.Set(ConfigKey.AllowCommentAudit, "true", "评论需要审核");
            _optionService.Set(ConfigKey.SiteKeywords, "博客系统,asp.net core,iBlogs");
            _optionService.Set(ConfigKey.SiteDescription, "博客系统,asp.net core,iBlogs");
            var siteUrl = BlogsUtils.BuildUrl(_param.SiteUrl);
            _optionService.Set(ConfigKey.SiteTitle, _param.SiteTitle);
            _optionService.Set(ConfigKey.SiteUrl, siteUrl);
            return true;
        }

        private bool InitContent()
        {
            _contentsService.Publish(new ContentInput
            {
                Title = "关于",
                Slug = "about",
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Content = "### Hello World\r\n\r\n这是我的关于页面\r\n\r\n### 当然还有其他\r\n\r\n具体你来写点什么吧",
                AuthorId = _users.Id,
                Type = ContentType.Page,
                Status = ContentStatus.Publish,
                Categories = "默认分类",
                Hits = 0,
                CommentsNum = 0,
                AllowComment = true,
                AllowPing = true,
                AllowFeed = true
            });

            var firstArticle = _contentsService.Publish(new ContentInput
            {
                Title = "第一篇文章",
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Content = "## Hello  World.\r\n\r\n> 第一篇文章总得写点儿什么?...\r\n\r\n----------\r\n\r\n\r\n<!--more-->\r\n\r\n```java\r\npublic static void main(string[] args){\r\n    System.out.println(\\\"Hello Tale.\\\");\r\n}\r\n```",
                AuthorId = _users.Id,
                Type = ContentType.Post,
                Status = ContentStatus.Publish,
                Categories = "默认分类",
                Hits = 10,
                CommentsNum = 0,
                AllowComment = true,
                AllowPing = true,
                AllowFeed = true
            });

            _contentsService.Publish(new ContentInput
            {
                Title = "友情链接",
                Slug = "links",
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Content = "## 友情链接\r\n\r\n- :lock: [王爵的技术博客]()\r\n- :lock: [cyang.tech]()\r\n- :lock: [Bakumon''s Blog]()\r\n\r\n## 链接须知\r\n\r\n> 请确定贵站可以稳定运营\r\n> 原创博客优先，技术类博客优先，设计、视觉类博客优先\r\n> 经常过来访问和评论，眼熟的\r\n\r\n备注：默认申请友情链接均为内页（当前页面）\r\n\r\n## 基本信息\r\n\r\n                网站名称：Tale博客\r\n                网站地址：https://tale.biezhi.me\r\n\r\n请在当页通过评论来申请友链，其他地方不予回复\r\n\r\n暂时先这样，同时欢迎互换友链，这个页面留言即可。 ^_^\r\n\r\n还有，我会不定时对无法访问的网址进行清理，请保证自己的链接长期有效。'",
                AuthorId = _users.Id,
                Type = ContentType.Page,
                Status = ContentStatus.Publish,
                Categories = "默认分类",
                Hits = 10,
                CommentsNum = 0,
                AllowComment = true,
                AllowPing = true,
                AllowFeed = true
            });
            _metasService.SaveMetas(firstArticle, "默认分类", MetaType.Category);
            return true;
        }
    }
}