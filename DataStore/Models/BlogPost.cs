using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataStore.Models
{
    public class BlogPost
    {
        public Guid Id { get; set; }
        [Required(ErrorMessage = "Title required")]
        public string Title { get; set; }
        [Required(ErrorMessage = "Post text required")]
        public string Body { get; set; }
        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset PubDate { get; set; }
        public bool IsPublic { get; set; }
        public bool IsDeleted { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
        [Required(ErrorMessage = "Description text required")]
        public string Description { get; set; }
        [Required(ErrorMessage = "Url required")]
        public string Url { get; set; }

    }
}
