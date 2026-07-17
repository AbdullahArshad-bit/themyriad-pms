using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Common.Filters
{
    public class AllowdExtensionsAttribute : ValidationAttribute
    {
        /// <summary>  
        /// Gets or sets extensions property.  
        /// </summary>  
        public string Extensions { get; set; } = "png,jpg,jpeg,svg,pdf,gif";


        /// <summary>  
        /// Is valid method.  
        /// </summary>  
        /// <param name="value">Value parameter</param>  
        /// <returns>Returns - true is specify extension matches.</returns>  
        public override bool IsValid(object value)
        {
            // Initialization  
            HttpPostedFileBase file = value as HttpPostedFileBase;
            bool isValid = true;

            // Settings.  
            List<string> allowedExtensions = this.Extensions.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // Verification.  
            if (file != null)
            {
                // Initialization.  
                var fileName = file.FileName;

                // Settings.  
                isValid = allowedExtensions.Any(y => fileName.EndsWith(y));
            }

            // Info  
            return isValid;
        }
    }
}
