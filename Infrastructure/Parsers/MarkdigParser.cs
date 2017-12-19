using Infrastructure.Interfaces;
using System;
using Markdig;

namespace Infrastructure.Parsers
{
    public class MarkdigParser : IOutputStrategy
    {
        private static MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
                //.UseDiagrams()
                //.UseAdvancedExtensions()
                //.UseYamlFrontMatter()
                //.DisableHtml()
                //.UseAutoLinks()
                //.UseSoftlineBreakAsHardlineBreak()
                //.Build();
                .UseAbbreviations()
                .UseAutoIdentifiers()
                .UseCitations()
                .UseCustomContainers()
                .UseDefinitionLists()
                .UseEmphasisExtras()
                .UseFigures()
                .UseFooters()
                .UseFootnotes()
                .UseGridTables()
                .UseMathematics()
                .UseMediaLinks()
                .UsePipeTables()
                .UseListExtras()
                .UseTaskLists()
                .UseDiagrams()
                .UseAutoLinks()
                .UseGenericAttributes()
                .Build();

        public string Transform(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                throw new ArgumentNullException(nameof(markdown));
            }

            var result = Markdown.ToHtml(markdown, pipeline);

            return result;
        }
    }
      

  
}
