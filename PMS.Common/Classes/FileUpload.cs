using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Common.Classes
{
    public class FileUpload
    {
        public FileUploadResult Upload(HttpPostedFileBase postedFile, string uploadDirectory)
        {
            FileUploadResult result = new FileUploadResult();
            result.Success = false;
            try
            {
                string filePath = string.Empty;
                string path = HttpContext.Current.Request.MapPath(uploadDirectory);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                
                string fileName = DateTime.Now.ToString("[dd_MMM_yyyy]_[HH_mm_ss]_") + Path.GetFileName(postedFile.FileName);

                filePath = Path.Combine(path, fileName);
                string extension = Path.GetExtension(postedFile.FileName);
                postedFile.SaveAs(filePath);

                result.Success = true;
                result.LocalFilePath = filePath;
                result.ServerPath = Helper.GetBaseUrlWeb(HttpContext.Current.Request) + uploadDirectory + "/" + fileName;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = ex.Message;
            }

            return result;
        }
        public void RemoveFile(string filename, string uploadDirectory)
        {
            string directoryPath = HttpContext.Current.Request.MapPath(uploadDirectory);
            if (Directory.Exists(directoryPath))
            {
                var filepath = HttpContext.Current.Request.MapPath(uploadDirectory+ "/" + filename);
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }
            }
        }
    }
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string LocalFilePath { get; set; }
        public string ServerPath { get; set; }
        public string Exception { get; set; }
    }
}
