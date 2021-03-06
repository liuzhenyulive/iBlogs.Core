﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using iBlogs.Site.Core.Blog.Content;
using iBlogs.Site.Core.Blog.Meta;
using iBlogs.Site.Core.Storage;

namespace iBlogs.Site.Core.Blog.Relationship
{
    public class Relationships : IEntityBase
    {
        [Key]
        public int Id { get; set; }

        /**
         * 文章主键
         */
        public int Cid;

        [ForeignKey("Cid")]
        public Contents Content { get; set; }

        /**
         * 项目主键
         */
        public int Mid;

        [ForeignKey("Mid")]
        public Metas Meta { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }

        public bool Deleted { get; set; }
    }
}