using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;

namespace PMS.Common
{
    public class Helper
    {
        public static string GetBaseUrl(HttpRequestMessage Request)
        {
            if (Request != null)
            {
                var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);

                return baseUrl;
            }
            else
                return string.Empty;
        }
        public static string GetBaseUrlWeb(HttpRequest request)
        {
            return request.Url.Scheme + "://" + request.Url.Authority;
        }
        public static string GenerateResourcePath(string baseUrl, string relativePath)
        {
            return baseUrl + relativePath;
        }
        public static string ReadFile(string fileName)
        {
            string ret = "";
            try
            {
                string file = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
                FileInfo fi = new FileInfo(file);
                string baseDirectory = fi.DirectoryName;


                var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);



                var filePath = baseDirectory + fileName;
                ret = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {

            }
            return ret;
        }

        public static string GetModelError(System.Web.Mvc.ModelStateDictionary modelState)
        {
            string message = "";

            try
            {
                var errors = modelState.Select(x => x.Value.Errors)
                         .Where(y => y.Count > 0)
                         .ToList();
                foreach (var error in errors)
                {
                    foreach (var msg in error)
                    {
                        if (!string.IsNullOrEmpty(msg.ErrorMessage))
                            message += msg.ErrorMessage + Environment.NewLine;
                        else
                        {
                            if (!string.IsNullOrEmpty(msg.Exception.Message))
                                message += msg.Exception.Message + Environment.NewLine;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return message;
        }

        public static void ExportToExcel(HttpResponseBase Response, object data, string fileName)
        {
            var grid = new System.Web.UI.WebControls.GridView();
            grid.DataSource = data;
            grid.DataBind();
            Response.ClearContent();
            fileName = "filename=" + fileName + ".xls";
            Response.AddHeader("content-disposition", "attachment; " + fileName);
            Response.ContentType = "application/excel";
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);

            grid.RenderControl(htw);
            Response.Write(sw.ToString());
            Response.End();
        }
    }
}
