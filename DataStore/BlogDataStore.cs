using DataStore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace DataStore
{
    public class BlogDataStore
    {
        const string UploadsFolder = "wwwroot\\Uploads";
        const string PostsFolder = "BlogFiles\\Posts";
        const string DraftsFolder = "BlogFiles\\Drafts";

        private static Object thisLock = new object();

        private readonly IFileSystem _fileSystem;

        public BlogDataStore(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            InitStorageFolders();
        }

        public void InitStorageFolders()
        {
            _fileSystem.CreateDirectory(PostsFolder);
            _fileSystem.CreateDirectory(DraftsFolder);
            _fileSystem.CreateDirectory(UploadsFolder);
        }
        private void SetId(BlogPost post)
        {
            if (post.Id == Guid.Empty)
            {
                post.Id = Guid.NewGuid();
            }
        }

        private static XElement GetCommentsRootNode(XDocument doc)
        {
            XElement commentsNode;
            if (doc.Root.Elements("Comments").Any())
            {
                commentsNode = doc.Root.Element("Comments");
            }
            else
            {
                commentsNode = new XElement("Comments");
                doc.Root.Add(commentsNode);
            }
            return commentsNode;
        }

        private XDocument LoadPostXml(string filePath)
        {
            string text = _fileSystem.ReadFileText(filePath);
            var reader = new StringReader(text);
            return XDocument.Load(reader);
        }

        public IEnumerable<XElement> GetCommentRoot(XDocument doc)
        {
            IEnumerable<XElement> commentRoot = doc.Root.Elements("Comments");
            return commentRoot;
        }

        public void AppendCommentInfo(Comment comment, BlogPost Post, XDocument doc)
        {
            XElement commentsNode = GetCommentsRootNode(doc);
            XElement commentNode = new XElement("Comment");
            commentNode.Add(new XElement("AuthorName", comment.AuthorName));
            commentNode.Add(new XElement("PubDate", comment.PubDate.ToString("o")));
            commentNode.Add(new XElement("CommentBody", comment.Body));
            commentNode.Add(new XElement("IsPublic", true));
            commentNode.Add(new XElement("UniqueId", comment.UniqueId));

            commentsNode.Add(commentNode);
        }

        public void IterateComments(IEnumerable<XElement> comments, List<Comment> listAllComments)
        {
            foreach (XElement comment in comments)
            {
                Comment newComment = new Comment
                {
                    AuthorName = comment.Element("AuthorName").Value,
                    Body = comment.Element("CommentBody").Value,
                    PubDate = DateTimeOffset.Parse(comment.Element("PubDate").Value),
                    IsPublic = Convert.ToBoolean(comment.Element("IsPublic").Value),
                    UniqueId = (Guid.Parse(comment.Element("UniqueId").Value)),

                };
                listAllComments.Add(newComment);
            }
        }
        public List<Comment> GetAllComments(XDocument doc)
        {
            IEnumerable<XElement> commentRoot = GetCommentRoot(doc);
            IEnumerable<XElement> comments;
            List<Comment> listAllComments = new List<Comment>();
            if (commentRoot.Any())
            {
                comments = commentRoot.Elements("Comment");
                IterateComments(comments, listAllComments);
            }
            return listAllComments;
        }

        public Comment FindComment(Guid UniqueId, BlogPost post)
        {
            List<Comment> commentsList = post.Comments;
            foreach (Comment comment in commentsList)
            {
                if (comment.UniqueId.Equals(UniqueId))
                {
                    return comment;
                }
            }
            return null;
        }

        public XElement AddComments(BlogPost post, XElement rootNode)
        {
            XElement commentsNode = new XElement("Comments");

            foreach (Comment comment in post.Comments)
            {
                XElement commentNode = new XElement("Comment");
                commentNode.Add(new XElement("AuthorName", comment.AuthorName));
                commentNode.Add(new XElement("PubDate", comment.PubDate.ToString("o")));
                commentNode.Add(new XElement("CommentBody", comment.Body));
                commentNode.Add(new XElement("IsPublic", comment.IsPublic));
                commentNode.Add(new XElement("UniqueId", comment.UniqueId));
                commentsNode.Add(commentNode);
            }
            rootNode.Add(commentsNode);

            return rootNode;
        }

        public void AppendPostInfo(BlogPost post, XElement rootNode)
        {
            rootNode.Add(new XElement("Id", post.Id.ToString("N")));
            rootNode.Add(new XElement("Title", post.Title));
            rootNode.Add(new XElement("Body", post.Body));
            rootNode.Add(new XElement("PubDate", post.PubDate.ToString("o")));
            rootNode.Add(new XElement("LastModified", post.LastModified.ToString("o")));
            rootNode.Add(new XElement("IsPublic", post.IsPublic.ToString()));
            rootNode.Add(new XElement("IsDeleted", post.IsDeleted.ToString()));
            rootNode.Add(new XElement("Description", post.Description));
            rootNode.Add(new XElement("Url", post.Url));
        }

        public void SavePost(BlogPost post)
        {
            SetId(post);
            string outputFilePath;
            if (post.IsPublic == true)
            {
                string date = post.PubDate.UtcDateTime.ToString("s").Replace(":", "-");
                outputFilePath = $"{PostsFolder}\\{date}_{post.Id.ToString("N")}.xml";
            }
            else
            {
                outputFilePath = $"{DraftsFolder}\\{post.Id.ToString("N")}.xml";
            }
            XDocument doc = new XDocument();
            XElement rootNode = new XElement("Post");

            AppendPostInfo(post, rootNode);
            AddComments(post, rootNode);
            doc.Add(rootNode);

            using (MemoryStream ms = new MemoryStream())
            {
                doc.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                using (StreamReader reader = new StreamReader(ms))
                {
                    string text = reader.ReadToEnd();
                    _fileSystem.WriteFileText(outputFilePath, text);
                }
            }

        }

        public BlogPost CollectPostInfo(string expectedFilePath)
        {
            bool hasIdChanged = false;
            XDocument doc;
            try
            {
                doc = LoadPostXml(expectedFilePath);
            }
            catch
            {
                return null;
            }

            BlogPost post = new BlogPost();
            if (doc.Root.Element("Id") != null && !doc.Root.Element("Id").IsEmpty)
            {
                Guid newGuid;
                if (Guid.TryParse(doc.Root.Element("Id").Value, out newGuid))
                {
                    post.Id = newGuid;
                }
                else
                {
                    string date = post.PubDate.UtcDateTime.ToString("s").Replace(":", "-");
                    _fileSystem.DeleteFile(expectedFilePath);
                    SetId(post);
                    hasIdChanged = true;
                }
            }
            else
            {
                post.Id = Guid.NewGuid();
            }
            
            post.Title = GetValue(doc.Root.Element("Title"), "");
            post.Body = GetValue(doc.Root.Element("Body"), "");
            post.PubDate = GetValue(doc.Root.Element("PubDate"), default(DateTimeOffset));
            post.LastModified = GetValue(doc.Root.Element("LastModified"), default(DateTimeOffset));
            post.IsPublic = GetValue(doc.Root.Element("IsPublic"), true);
            post.IsDeleted = GetValue(doc.Root.Element("IsDeleted"), false);
            post.Description = GetValue(doc.Root.Element("Description"), "");
            post.Url = GetValue(doc.Root.Element("Url"), "");
            post.Comments = GetAllComments(doc);

            if (hasIdChanged)
            {
                SavePost(post);
            }
            return post;
        }

        private static string GetValue(XElement e, string defaultValue)
        {
            if (e != null && !e.IsEmpty)
            {
                return e.Value;
            }
            else
            {
                return defaultValue;
            }
        }

        private static DateTimeOffset GetValue(XElement e, DateTimeOffset defaultValue)
        {
            if (e != null && !e.IsEmpty)
            {
                return DateTimeOffset.Parse(e.Value);
            }
            else
            {
                return defaultValue;
            }
        }

        private static bool GetValue(XElement e, bool defaultValue)
        {
            if (e != null && !e.IsEmpty)
            {
                return Convert.ToBoolean(e.Value);
            }
            else
            {
                return defaultValue;
            }
        }

        public BlogPost GetPost(string id)
        {
            var expectedFilePath = $"{DraftsFolder}\\{id}.xml";
            if (_fileSystem.FileExists(expectedFilePath))
            {
                return CollectPostInfo(expectedFilePath);
            }
            else
            {
                List<string> files = _fileSystem.EnumerateFiles($"{PostsFolder}").ToList();
                foreach (var file in files)
                {
                    var start = file.IndexOf("_");
                    var end = file.IndexOf(".");
                    var element = file.Substring(start + 1, end - start - 1);
                    if (element == id)
                    {
                        return CollectPostInfo(file);
                    }
                }
            }
            return null;
        }

        private List<BlogPost> IteratePosts(List<string> files)
        {
            List<BlogPost> allPosts = new List<BlogPost>();
            foreach (var file in files)
            {
                BlogPost post = CollectPostInfo(file);
                allPosts.Add(post);
            }
            return allPosts;
        }

        public List<BlogPost> GetAllPosts()
        {
            string filePath = $"{PostsFolder}";
            List<string> files = _fileSystem.EnumerateFiles(filePath).OrderByDescending(f => f).ToList();
            return IteratePosts(files);
        }

        public List<BlogPost> GetAllDrafts()
        {
            string filePath = $"{DraftsFolder}";
            List<string> files = _fileSystem.EnumerateFiles(filePath).OrderByDescending(f => f).ToList();
            return IteratePosts(files);
        }

        public void UpdatePost(BlogPost post, bool wasPublic)
        {
            if (wasPublic)
            {
                string date = post.PubDate.UtcDateTime.ToString("s").Replace(":", "-");
                _fileSystem.DeleteFile($"{PostsFolder}\\{date}_{post.Id.ToString("N")}.xml");
            }
            else
            {
                _fileSystem.DeleteFile($"{DraftsFolder}\\{post.Id.ToString("N")}.xml");
            }
            SavePost(post);
        }

        public void SaveFiles(List<IFormFile> files)
        {
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    using (Stream uploadedFileStream = file.OpenReadStream())
                    {
                        string name = Path.GetFileName(file.FileName);
                        string filePath = Path.Combine(UploadsFolder, name);

                        if (CheckFileNameExists(name))
                        {
                            _fileSystem.DeleteFile(filePath);
                            continue;
                        }

                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        do
                        {
                            bytesRead = uploadedFileStream.Read(buffer, 0, 1024);
                            if (bytesRead == 0)
                            {
                                break;
                            }
                            _fileSystem.AppendFile(filePath, buffer, 0, bytesRead);
                        } while (bytesRead > 0);
                    }
                }
            }
        }

        public IEnumerable<string> GetFileNames()
        {
            IEnumerable<string> fileNames = _fileSystem.EnumerateFiles(UploadsFolder);
            return fileNames;
        }

        private bool CheckFileNameExists(string filePath)
        {
            return _fileSystem.FileExists(filePath);
        }
    }
}
