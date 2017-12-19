using Infrastructure.Interfaces;
using System;

namespace Blog_Project_Discovery.Models.Home
{
    public class PostModel
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTimeOffset PubDate { get; set; }
        public string Description { get; set; }
        public IOutputStrategy OutputStrategy { get; set; }
        public string ParseBody()
        {
            return OutputStrategy.Transform(this.Body);
        }
        public string ParseDescription()
        {
            return OutputStrategy.Transform(this.Description);
        }
    }   
}
