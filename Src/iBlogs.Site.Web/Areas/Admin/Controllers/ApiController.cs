﻿using iBlogs.Site.Core.Common;
using iBlogs.Site.Core.Common.Extensions;
using iBlogs.Site.Core.Common.Request;
using iBlogs.Site.Core.Common.Response;
using iBlogs.Site.Core.Log;
using iBlogs.Site.Core.Option.DTO;
using iBlogs.Site.Core.Option.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using iBlogs.Site.Core.Blog.Attach;
using iBlogs.Site.Core.Blog.Attach.Service;
using iBlogs.Site.Core.Blog.Comment;
using iBlogs.Site.Core.Blog.Comment.DTO;
using iBlogs.Site.Core.Blog.Comment.Service;
using iBlogs.Site.Core.Blog.Content;
using iBlogs.Site.Core.Blog.Content.DTO;
using iBlogs.Site.Core.Blog.Content.Service;
using iBlogs.Site.Core.Blog.Meta;
using iBlogs.Site.Core.Blog.Meta.DTO;
using iBlogs.Site.Core.Blog.Meta.Service;
using iBlogs.Site.Core.Option;
using iBlogs.Site.Core.Security.Service;
using ConfigKey = iBlogs.Site.Core.Option.ConfigKey;

namespace iBlogs.Site.Web.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class ApiController : Controller
    {
        private readonly IMetasService _metasService;
        private readonly IContentsService _contentsService;
        private readonly IUserService _userService;
        private readonly IHostingEnvironment _env;
        private readonly IAttachService _attachService;
        private readonly IOptionService _optionService;
        private readonly ICommentsService _commentsService;

        public ApiController(IMetasService metasService, IContentsService contentsService, IUserService userService, IHostingEnvironment env, IAttachService attachService, IOptionService optionService, ICommentsService commentsService)
        {
            _metasService = metasService;
            _contentsService = contentsService;
            _userService = userService;
            _env = env;
            _attachService = attachService;
            _optionService = optionService;
            _commentsService = commentsService;
        }
        
        [AdminApiRoute("logs")]
        public ApiResponse<List<Logs>> SysLogs(PageParam pageParam)
        {
            return ApiResponse<List<Logs>>.Ok(new List<Logs>());
        }

        // @SysLog("删除页面")
        // @PostRoute("page/Delete/:cid")
        [AdminApiRoute("page/Delete/{cid}")]
        public ApiResponse DeletePage(int cid)
        {
            _contentsService.Delete(cid);
            return ApiResponse.Ok();
        }

        [AdminApiRoute("articles/{cid}")]
        public ApiResponse<ContentResponse> Article(string cid)
        {
            var contents = _contentsService.GetContents(cid);
            contents.Content = "";
            return ApiResponse<ContentResponse>.Ok(contents);
        }

        [AdminApiRoute("articles/content/{cid}")]
        public ApiResponse<string> ArticleContent(string cid)
        {
            var contents = _contentsService.GetContents(cid);
            return ApiResponse<string>.Ok(contents.Content);
        }

        [AdminApiRoute("article/new")]
        public ApiResponse<int> NewArticle([FromBody]ContentInput contents)
        {
            var user = _userService.CurrentUsers;
            contents.Type = ContentType.Post;
            contents.AuthorId = user.Uid;
            //将点击数设初始化为0
            contents.Hits = 0;
            //将评论数设初始化为0
            contents.CommentsNum = 0;
            if (StringKit.IsBlank(contents.Categories))
            {
                contents.Categories = "默认分类";
            }
            var cid = _contentsService.Publish(contents);
            return ApiResponse<int>.Ok(cid, cid);
        }

        //@PostRoute("article/Delete/:cid")
        [AdminApiRoute("article/Delete/{cid}")]
        public ApiResponse DeleteArticle(int cid)
        {
            _contentsService.Delete(cid);
            return ApiResponse.Ok();
        }

        [AdminApiRoute("article/update")]
        public ApiResponse<int> UpdateArticle([FromBody]ContentInput contents)
        {
            if (contents?.Id == null)
            {
                return ApiResponse<int>.Fail("缺少参数，请重试");
            }
            contents.Type = ContentType.Post;
            var cid = contents.Id.Value;
            _contentsService.UpdateArticle(contents);
            return ApiResponse<int>.Ok(cid, cid);
        }

        // @GetRoute("articles")
        public ApiResponse ArticleList(ArticleParam articleParam)
        {
            articleParam.Type = ContentType.Post;
            articleParam.Page--;
            var articles = _contentsService.FindArticles(articleParam);
            return ApiResponse<Page<ContentResponse>>.Ok(articles);
        }

        [AdminApiRoute("pages")]
        public ApiResponse<Page<ContentResponse>> PageList(ArticleParam articleParam)
        {
            articleParam.Type = ContentType.Page;
            articleParam.Page--;
            var articles = _contentsService.FindArticles(articleParam);
            return ApiResponse<Page<ContentResponse>>.Ok(articles);
        }

        //@SysLog("发布页面")
        [AdminApiRoute("page/new")]
        public ApiResponse NewPage([FromBody]ContentInput contents)
        {
            var users = _userService.CurrentUsers;
            contents.Type = ContentType.Page;
            contents.AllowPing = true;
            contents.AuthorId = users.Uid;
            _contentsService.Publish(contents);
            return ApiResponse.Ok();
        }

        //@SysLog("修改页面")
        [AdminApiRoute("page/update")]
        public ApiResponse UpdatePage([FromBody]ContentInput contents)
        {
            if (null == contents.Id)
            {
                return ApiResponse.Fail("缺少参数，请重试");
            }
            int cid = contents.Id.Value;
            contents.Type = ContentType.Page;
            _contentsService.UpdateArticle(contents);
            return ApiResponse.Ok(cid);
        }

        // @SysLog("保存分类")
        [HttpPost("/admin/api/category/save")]
        public ApiResponse SaveCategory([FromBody]MetaParam metaParam)
        {
            _metasService.SaveMeta(MetaType.Category, metaParam.Cname, metaParam.Id);
            return ApiResponse.Ok();
        }

        // @SysLog("删除分类/标签")
        // @PostRoute()
        [AdminApiRoute("category/Delete/{id}")]
        public ApiResponse DeleteMeta(int id)
        {
            _metasService.Delete(id);
            return ApiResponse.Ok();
        }

        [AdminApiRoute("comments")]
        public ApiResponse<Page<CommentResponse>> CommentList(CommentPageParam commentParam)
        {
            if (commentParam == null)
                commentParam = new CommentPageParam();

            return ApiResponse<Page<CommentResponse>>.Ok(_commentsService.GetComments(commentParam));
        }

        // @SysLog("删除评论")
        [AdminApiRoute("comment/Delete/{coid}")]
        public ApiResponse DeleteComment(int? coid)
        {
            _commentsService.Delete(coid);
            return ApiResponse.Ok();
        }

        // @SysLog("修改评论状态")
        [AdminApiRoute("comment/status")]
        public ApiResponse UpdateStatus([FromBody]CommentParam param)
        {
            _commentsService.UpdateComment(param);
            return ApiResponse.Ok();
        }

        // @SysLog("回复评论")
        // @PostRoute("comment/reply")
        public ApiResponse ReplyComment(Comments comments)
        {
            throw new NotImplementedException();
        }

        [AdminApiRoute("attaches")]
        public ApiResponse<Page<Attachment>> AttachList(PageParam pageParam)
        {
            return ApiResponse<Page<Attachment>>.Ok(_attachService.GetPage(pageParam));
        }

        //    @SysLog("删除附件")
        //@PostRoute("attach/Delete/:id")
        [AdminApiRoute("attach/Delete/{id}")]
        public ApiResponse DeleteAttach(int id)
        {
            _attachService.Delete(id);
            return ApiResponse.Ok();
        }

        // @GetRoute("categories")
        public ApiResponse CategoryList()
        {
            return ApiResponse<List<Metas>>.Ok(_metasService.GetMetas(MetaType.Category, int.Parse(ConfigData.Get(ConfigKey.MaxPage))));
        }

        // @GetRoute("tags")
        public ApiResponse TagList()
        {
            return ApiResponse<List<Metas>>.Ok(_metasService.GetMetas(MetaType.Tag, int.Parse(ConfigData.Get(ConfigKey.MaxPage))));
        }

        // @GetRoute("options")
        [AdminApiRoute("options")]
        public ApiResponse<IDictionary<ConfigKey, string>> Options()
        {
            return ApiResponse<IDictionary<ConfigKey, string>>.Ok(ConfigData.GetAll());
        }

        //@SysLog("保存系统配置")
        [AdminApiRoute("options/save")]
        public ApiResponse SaveOptions(IDictionary<ConfigKey, string> options)
        {
            foreach (var keyValuePair in options)
            {
                _optionService.Set(keyValuePair.Key, keyValuePair.Value);
            }
            return ApiResponse.Ok();
        }

        //@SysLog("保存高级选项设置")
        // @PostRoute("advanced/save")
        [AdminApiRoute("advanced/save")]
        public ApiResponse SaveAdvance(AdvanceParam advanceParam)
        {
            // 要过过滤的黑名单列表
            if (!advanceParam.BlockIps.IsNullOrWhiteSpace())
            {
                _optionService.Set(ConfigKey.BlockIpList, advanceParam.BlockIps);
            }

            if (!advanceParam.CdnUrl.IsNullOrWhiteSpace())
            {
                _optionService.Set(ConfigKey.CdnUrl, advanceParam.CdnUrl);
            }

            // 是否允许重新安装
            if (!advanceParam.AllowInstall.IsNullOrWhiteSpace())
            {
                _optionService.Set(ConfigKey.AllowInstall, advanceParam.AllowInstall);
            }

            // 评论是否需要审核
            if (!advanceParam.AllowCommentAudit.IsNullOrWhiteSpace())
            {
                _optionService.Set(ConfigKey.AllowCommentAudit, advanceParam.AllowCommentAudit);
            }

            // 是否允许公共资源CDN
            if (!advanceParam.AllowCloudCdn.IsNullOrWhiteSpace())
            {
                _optionService.Set(ConfigKey.AllowCloudCdn, advanceParam.AllowCloudCdn);
            }
            return ApiResponse.Ok();
        }

        /**
        * 上传文件接口
        */

        [AdminApiRoute("attach/upload")]
        public async Task<ApiResponse<List<Attachment>>> UploadAsync(List<IFormFile> files)
        {
            //log.info("UPLOAD DIR = {}", TaleUtils.UP_DIR);

            var users = _userService.CurrentUsers;
            var uid = users.Uid;
            List<Attachment> errorFiles = new List<Attachment>();
            List<Attachment> urls = new List<Attachment>();

            var fileItems = HttpContext.Request.Form.Files;
            if (null == fileItems || fileItems.Count == 0)
            {
                return ApiResponse<List<Attachment>>.Fail("请选择文件上传");
            }

            foreach (var fileItem in fileItems)
            {
                string fname = fileItem.FileName;
                if ((fileItem.Length / 1024) <= int.Parse(ConfigData.Get(ConfigKey.MaxFileSize, 204800.ToString())))
                {
                    var fkey = BlogsUtils.GetFileKey(fname, _env.WebRootPath);

                    var ftype = fileItem.ContentType.Contains("image") ? Types.IMAGE : Types.FILE;
                    var filePath = _env.WebRootPath + fkey;

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await fileItem.CopyToAsync(stream);
                    }
                    if (BlogsUtils.IsImage(filePath))
                    {
                        var newFileName = BlogsUtils.GetFileName(fkey);
                        var thumbnailFilePath = _env.WebRootPath + fkey.Replace(newFileName, "thumbnail_" + newFileName);
                        BlogsUtils.CutCenterImage(_env.WebRootPath + fkey, thumbnailFilePath, 270, 380);
                    }

                    Attachment attachment = new Attachment();
                    attachment.FName = fname;
                    attachment.AuthorId = uid;
                    attachment.FKey = fkey;
                    attachment.FType = ftype;
                    if (await _attachService.Save(attachment))
                    {
                        urls.Add(attachment);
                    }
                    else
                    {
                        errorFiles.Add(attachment);
                    }
                }
                else
                {
                    Attachment attachment = new Attachment();
                    attachment.FName = fname;
                    errorFiles.Add(attachment);
                }
            }

            if (errorFiles.Count > 0)
            {
                return ApiResponse<List<Attachment>>.Fail().SetPayload(errorFiles);
            }
            return ApiResponse<List<Attachment>>.Ok(urls);
        }
    }
}