using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.TagHelpers;
using StyleCopAnalyzers.Status.Common;

namespace StyleCopAnalyzers.Status.Website.TagHelpers
{
    // You may need to install the Microsoft.AspNet.Razor.Runtime package into your project
    [HtmlTargetElement("status", TagStructure = TagStructure.WithoutEndTag)]
    public class StyleCopTagHelper : TagHelper
    {
        public bool? Status { get; set; }
        public CodeFixStatus? CurrentCodeFixStatus { get; set; }
        public FixAllStatus? CurrentFixAllStatus { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            string color = null;
            string gliphClass = null;

            if (Status == true)
            {
                color = "green";
                gliphClass = "fa-check";
            }
            else if (Status == false)
            {
                color = "red";
                gliphClass = "fa-times";
            }

            switch (this.CurrentCodeFixStatus)
            {
            case CodeFixStatus.Implemented:
                color = "green";
                gliphClass = "fa-check";
                break;
            case CodeFixStatus.NotImplemented:
                color = "red";
                gliphClass = "fa-times";
                break;
            case CodeFixStatus.NotYetImplemented:
                color = "blue";
                gliphClass = "fa-exclamation";
                break;
            default:
                break;
            }

            switch (this.CurrentFixAllStatus)
            {
            case FixAllStatus.BatchFixer:
                color = "blue";
                gliphClass = "fa-hourglass";
                break;
            case FixAllStatus.CustomImplementation:
                color = "green";
                gliphClass = "fa-check";
                break;
            case FixAllStatus.None:
                color = "red";
                gliphClass = "fa-times";
                break;
            default:
                break;
            }

            output.TagName = "i";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.Add("class", "fa " + gliphClass);
            output.Attributes.Add("style", $"color: {color};");
        }
    }
}
