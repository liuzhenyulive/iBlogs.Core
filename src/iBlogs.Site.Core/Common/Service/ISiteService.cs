﻿using System.Collections.Generic;
using iBlogs.Site.Core.Comment;
using iBlogs.Site.Core.Common.Response;
using iBlogs.Site.Core.Content;
using iBlogs.Site.Core.Content.DTO;
using iBlogs.Site.Core.Meta;

namespace iBlogs.Site.Core.Common.Service
{
    public interface ISiteServiceRe
    {
        List<Comments> recentComments(int limit);
        List<Contents> getContens(ContentType type, int limit);
        List<Archive> getArchives();
        Comments getComment(int coid);
        List<Metas> getMetas(MetaType type, int limit);
        Contents getNhContent(ContentType type, long created);
        Page<Comments> getComments(int cid, int page, int limit);
        long getCommentCount(int cid);
    }
}