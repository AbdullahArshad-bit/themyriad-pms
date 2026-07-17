using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace PMS
{
    public static class log
    {
        public static void WriterExceptions(Exception ex)
        {
            string excep = ex.Message;
            string path = "";
            string fileName = $"Error_{DateTime.Now.ToString("yyyyMMdd")}.txt";
            path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory + "\\Assets"));
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string folderexist = Path.Combine(path, fileName);

            using (StreamWriter writer = new StreamWriter(folderexist, true))
            {
                writer.WriteLine("----------Exception----------");
                writer.WriteLine("Date : " + DateTime.Now.ToString());
                writer.WriteLine();
                while (ex != null)
                {
                    writer.WriteLine(ex.GetType().FullName);
                    writer.WriteLine("Message : " + ex.Message);
                    writer.WriteLine("StackTrace : " + ex.StackTrace);
                    ex = ex.InnerException;
                }
                writer.Flush();
                writer.Close();
            }
        }

        public static void Writerlog(string baseurl, string req)
        {
            string path = "";
            string fileName = $"Error_{DateTime.Now.ToString("yyyyMMdd")}.txt";
            path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory + "\\Assets", "logs"));
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string folderexist = Path.Combine(path, fileName);

            using (StreamWriter writer = new StreamWriter(folderexist, true))
            {
                writer.WriteLine("----------Log of Every Request----------");

                writer.WriteLine("Date : " + DateTime.Now.ToString());
                writer.WriteLine("Base Url : " + baseurl);
                writer.WriteLine("Request Url : " + req);
                writer.WriteLine();
                writer.Flush();
                writer.Close();
            }
        }
    }
}