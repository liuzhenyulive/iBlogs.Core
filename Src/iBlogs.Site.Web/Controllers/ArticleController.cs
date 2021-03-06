﻿using System.Linq;
using iBlogs.Site.Core.Blog.Comment;
using iBlogs.Site.Core.Blog.Comment.DTO;
using iBlogs.Site.Core.Blog.Comment.Service;
using iBlogs.Site.Core.Blog.Content.Service;
using iBlogs.Site.Core.Common;
using iBlogs.Site.Core.Common.Response;
using iBlogs.Site.Core.Security.DTO;
using iBlogs.Site.Core.Security.Service;
using iBlogs.Site.Web.Attribute;
using iBlogs.Site.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace iBlogs.Site.Web.Controllers
{
    public class ArticleController : Controller
    {
        private readonly IContentsService _contentsService;
        private readonly IUserService _userService;
        private readonly ICommentsService _commentsService;

        public ArticleController(IContentsService contentsService, IUserService userService, ICommentsService commentsService)
        {
            _contentsService = contentsService;
            _userService = userService;
            _commentsService = commentsService;
        }

        [HttpGet("/article/{url}")]
        [ViewLayout("~/Views/Layout/Layout.cshtml")]
        public IActionResult Index(string url, int cp)
        {
            var content = _contentsService.GetContents(url);
            var pre = _contentsService.GetPre(content.Id);
            var next = _contentsService.GetNext(content.Id);
            var commentPage = new CommentPageParam { Page = cp == 0 ? 1 : cp, Cid = content.Id,Status = CommentStatus.Approved};
            var pageComments = _commentsService.GetComments(commentPage);
            CurrentUser currentUser = null;
            if (HttpContext.User.Claims.Any())
            {
                var uid = int.Parse(HttpContext.User.FindFirst(ClaimTypes.Uid)?.Value);
                var token = HttpContext.User.FindFirst(ClaimTypes.Token)?.Value;
                if (LoginToken.CheckToken(uid, token))
                {

                    var user = _userService.FindUserById(uid);
                    currentUser = new CurrentUser
                    {
                        Uid = uid,
                        Email = user.Email,
                        Username = user.Username,
                        ScreenName = user.ScreenName,
                        HomeUrl = user.HomeUrl
                    };

                }
            }
            return View("Index", new ArticleViewModel
            {
                Content = content,
                Pre = pre,
                Next = next,
                User = currentUser,
                Comments = pageComments,
            });
        }

        [HttpGet("/links")]
        [ViewLayout("~/Views/Layout/Layout.cshtml")]
        public IActionResult Links(int cp)
        {
            return Index("links", cp);
        }

        [HttpGet("/about")]
        [ViewLayout("~/Views/Layout/Layout.cshtml")]
        public IActionResult About(int cp)
        {
            return Index("about", cp);
        }


        public ApiResponse Comment(CommentRequest input)
        {
            var comment = new Comments
            {
                Parent = input.Coid ?? 0,
                Cid = input.Cid,
                Author = input.Author,
                Mail = input.Mail,
                Url = input.Url,
                Content = input.Content,
                Ip = HttpContext.Connection.RemoteIpAddress.ToString(),
                Agent = Request.Headers["User-Agent"].ToString()
            };
            _commentsService.SaveComment(comment);
            return ApiResponse.Ok();
        }
    }
}