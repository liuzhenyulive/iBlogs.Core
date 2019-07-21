using iBlogs.Site.MetaWeblog.Classes;

namespace iBlogs.Site.MetaWeblog.Wrappers
{
    public interface ICnBlogsWrapper:IMetaWeblogWrapper
    {
        BlogInfo[] GetUsersBlogs();
        Post[] GetRecentPosts(int numberOfPosts);
        bool DeletePost(string postId, bool publish);
        Post GetPost(string postId);
        int EditPost(string postId, Post post, bool publish);
        WpCategory GetCategories(string blogId);
        string NewPost(Post post, bool publish);
    }
}