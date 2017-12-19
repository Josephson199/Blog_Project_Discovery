using System.Collections.Generic;
using System.Linq;
using Blog_Project_Discovery.Models.Home;
using DataStore;
using Infrastructure.Parsers;
using Microsoft.AspNetCore.Mvc;

namespace Blog_Project_Discovery.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlogDataStore _blogDataStore;

        public HomeController(BlogDataStore dataStore)
        {
            _blogDataStore = dataStore;
        }       
        
        public IActionResult Index()
        {
            var blogPosts = _blogDataStore.GetAllPosts().Where(p => !p.IsDeleted && p.IsPublic).ToList();
                        
            var postSummaries = new List<PostSummary>();

            if (!blogPosts.Any())
            {
                return View(postSummaries);
            }

            foreach (var post in blogPosts)
            {
                var postModel = new PostSummary
                {
                    PubDate = post.PubDate,
                    Description = post.Description,
                    OutputStrategy = new MarkdigParser(),
                    Title = post.Title,
                    Url = post.Url
                };

                postSummaries.Add(postModel);
            }

            return View(postSummaries);
        }

        [HttpGet("about")]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet("post/{url}")]
        public IActionResult ViewPost(string url)
        {
            var post = _blogDataStore.GetAllPosts().FirstOrDefault(p => p.Url.Equals(url));

            if(post == null)
            {
                return View("Error");
            }

            var postModel = new PostModel
            {
                PubDate = post.PubDate,
                Body = post.Body,
                OutputStrategy = new MarkdigParser(),
                Title = post.Title,
                Description = post.Description
            };

            

            return View(postModel);
        }
    }
}