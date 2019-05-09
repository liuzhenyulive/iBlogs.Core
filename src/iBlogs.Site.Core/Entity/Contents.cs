﻿using System;

namespace iBlogs.Site.Core.Entity
{
    public class Contents : EntityBase
    {

        /**
         * 文章表主键
         */
        public int Cid { get; set; }

        /**
         * 文章标题
         */
        public string Title { get; set; }

        /**
         * 文章缩略名
         */
        public string Slug { get; set; }

        /**
         * 文章创建时间戳
         */
        public int Created { get; set; }

        /**
         * 文章修改时间戳
         */
        public int Modified { get; set; }

        /**
         * 文章内容
         */
        public string Content { get; set; }

        /**
         * 文章创建用户
         */
        public int AuthorId { get; set; }

        /**
         * 文章点击次数
         */
        public int Hits { get; set; }

        /**
         * 文章类型： PAGE、POST
         */
        public string Type { get; set; }

        /**
         * 内容类型，markdown或者html
         */
        public string FmtType { get; set; }

        /**
         * 文章缩略图
         */
        public string ThumbImg { get; set; }

        /**
         * 标签列表
         */
        public string Tags { get; set; }

        /**
         * 分类列表
         */
        public string Categories { get; set; }

        /**
         * 内容状态
         */
        public string Status { get; set; }

        /**
         * 内容所属评论数
         */
        public int CommentsNum { get; set; }

        /**
         * 是否允许评论
         */
        public Boolean AllowComment { get; set; }

        /**
         * 是否允许ping
         */
        public Boolean AllowPing { get; set; }

        /**
         * 允许出现在Feed中
         */
        public Boolean AllowFeed { get; set; }

        public string Url { get; set; }
    }
}