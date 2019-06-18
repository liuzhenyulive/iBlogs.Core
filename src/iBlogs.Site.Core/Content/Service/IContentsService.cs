﻿using System.Collections.Generic;
using System.Threading.Tasks;
using iBlogs.Site.Core.Common.Request;
using iBlogs.Site.Core.Common.Response;
using iBlogs.Site.Core.Content.DTO;
using iBlogs.Site.Core.Meta;

namespace iBlogs.Site.Core.Content.Service
{
    public interface IContentsService
    {
        ContentResponse GetContents(string id);

        int Publish(ContentInput contents);

        void UpdateArticle(ContentInput contents);

        void Delete(int cid);

        Page<ContentResponse> GetArticles(int mid, int page, int limit);

        Page<ContentResponse> FindArticles(ArticleParam articleParam);

        ContentResponse GetPre(int id);

        ContentResponse GetNext(int id);

        Page<ContentResponse> FindContentByMeta(MetaType type, string value, ArticleParam articleParam);

        Page<Archive> GetArchive(PageParam pageParam);

        void UpdateCommentCount(int cid,int updateCount);
        Task<List<ContentResponse>> GetContent(int limit);
    }
}