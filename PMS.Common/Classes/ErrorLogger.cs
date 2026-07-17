using System;
using System.IO;
using System.Web;

namespace PMS.Common.Classes
{
    public static class ErrorLogger
    {
        public static void WriteToErrorLog(string msg, string stkTrace, string Title)
        {
            // SKIP EXCEPTION "Thread was being aborted" From Logging.
            if (msg.Contains("Thread was being aborted"))
                return;
            // Checking Directory If Not Exist Then Create It.
            if (!System.IO.Directory.Exists("C:\\TheMyriadPMSLog\\"))
            {
                System.IO.Directory.CreateDirectory("C:\\TheMyriadPMSLog\\");
            }
            //if (!System.IO.Directory.Exists(HttpContext.Current.Server.MapPath("\\TheMyriadPMSLog\\")))
            //{
            //    System.IO.Directory.CreateDirectory((HttpContext.Current.Server.MapPath("\\TheMyriadPMSLog\\")));
            //}

            // Checking File.
            FileStream fs = new FileStream(HttpContext.Current.Server.MapPath("\\TheMyriadPMSLog\\ErrorLog.txt"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
            StreamWriter s = new StreamWriter(fs);

            s.Close();
            fs.Close();


            // Logging

            FileStream fs1 = new FileStream(HttpContext.Current.Server.MapPath("\\TheMyriadPMSLog\\ErrorLog.txt"), FileMode.Append, FileAccess.Write);
            StreamWriter s1 = new StreamWriter(fs1);

            s1.Write("Title : " + Title + Environment.NewLine);
            s1.Write("Message : " + msg + Environment.NewLine);
            s1.Write("StackTrace : " + stkTrace + Environment.NewLine);
            s1.Write("Date / Time : " + DateTime.Now.ToString() + Environment.NewLine);
            s1.Write("==============================================================================================" + Environment.NewLine);
            s1.Close();
            fs1.Close();
        }

        public static void WriteToExceptionLog(string msg, string stkTrace, string Title)
        {
            // SKIP EXCEPTION "Thread was being aborted" From Logging.
            if (msg.Contains("Thread was being aborted"))
                return;
            // Checking Directory If Not Exist Then Create It.
            if (!System.IO.Directory.Exists(HttpContext.Current.Server.MapPath("\\TheMyriadPMSLog\\")))
            {
                System.IO.Directory.CreateDirectory(HttpContext.Current.Server.MapPath("\\TheMyriadPMSLog\\"));
            }

            // Checking File.
            FileStream fs = new FileStream(HttpContext.Current.Server.MapPath("\\TheMyriadPMSLog\\ErrorLog.txt"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
            StreamWriter s = new StreamWriter(fs);

            s.Close();
            fs.Close();

            // Logging

            FileStream fs1 = new FileStream(HttpContext.Current.Server.MapPath("\\TheMyriadPMSLog\\ErrorLog.txt"), FileMode.Append, FileAccess.Write);
            StreamWriter s1 = new StreamWriter(fs1);

            s1.Write("Title : " + Title + Environment.NewLine);
            s1.Write("Message : " + msg + Environment.NewLine);
            s1.Write("StackTrace : " + stkTrace + Environment.NewLine);
            s1.Write("Date / Time : " + DateTime.Now.ToString() + Environment.NewLine);
            s1.Write("==============================================================================================" + Environment.NewLine);
            s1.Close();
            fs1.Close();
        }
        public static void WriteToUtilityLog(string msg, string Title)
        {
            // SKIP EXCEPTION "Thread was being aborted" From Logging.
            if (msg.Contains("Thread was being aborted"))
                return;
            // Checking Directory If Not Exist Then Create It.
            if (!System.IO.Directory.Exists("C:\\TheMyriadPMSLog\\"))
            {
                System.IO.Directory.CreateDirectory("C:\\TheMyriadPMSLog\\");
            }

            // Checking File.
            FileStream fs = new FileStream("C:\\TheMyriadPMSLog\\UtilityLog.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            StreamWriter s = new StreamWriter(fs);

            s.Close();
            fs.Close();

            // Logging

            FileStream fs1 = new FileStream("C:\\TheMyriadPMSLog\\UtilityLog.txt", FileMode.Append, FileAccess.Write);
            StreamWriter s1 = new StreamWriter(fs1);

            s1.Write("Title : " + Title + Environment.NewLine);
            s1.Write("Message : " + msg + Environment.NewLine);
            s1.Write("Date / Time : " + DateTime.Now.ToString() + Environment.NewLine);
            s1.Write("==============================================================================================" + Environment.NewLine);
            s1.Close();
            fs1.Close();
        }
        public static void WriteToCheetyLog(string msg, string Title)
        {
            // SKIP EXCEPTION "Thread was being aborted" From Logging.
            if (msg.Contains("Thread was being aborted"))
                return;
            // Checking Directory If Not Exist Then Create It.
            if (!System.IO.Directory.Exists("C:\\TheMyriadPMSLog\\"))
            {
                System.IO.Directory.CreateDirectory("C:\\TheMyriadPMSLog\\");
            }

            // Checking File.
            FileStream fs = new FileStream("C:\\TheMyriadPMSLog\\CheetyLog.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            StreamWriter s = new StreamWriter(fs);

            s.Close();
            fs.Close();

            // Logging

            FileStream fs1 = new FileStream("C:\\TheMyriadPMSLog\\CheetyLog.txt", FileMode.Append, FileAccess.Write);
            StreamWriter s1 = new StreamWriter(fs1);

            s1.Write("Title : " + Title + Environment.NewLine);
            s1.Write("Message : " + msg + Environment.NewLine);
            s1.Write("Date / Time : " + DateTime.Now.ToString() + Environment.NewLine);
            s1.Write("==============================================================================================" + Environment.NewLine);
            s1.Close();
            fs1.Close();

        }
        public static void WriteToEatMubarakLog(string msg, string Title)
        {
            // SKIP EXCEPTION "Thread was being aborted" From Logging.
            if (msg.Contains("Thread was being aborted"))
                return;
            // Checking Directory If Not Exist Then Create It.
            if (!System.IO.Directory.Exists("C:\\TheMyriadPMSLog\\"))
            {
                System.IO.Directory.CreateDirectory("C:\\TheMyriadPMSLog\\");
            }

            // Checking File.
            FileStream fs = new FileStream("C:\\TheMyriadPMSLog\\CheetyLog.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            StreamWriter s = new StreamWriter(fs);

            s.Close();
            fs.Close();

            // Logging

            FileStream fs1 = new FileStream("C:\\TheMyriadPMSLog\\CheetyLog.txt", FileMode.Append, FileAccess.Write);
            StreamWriter s1 = new StreamWriter(fs1);

            s1.Write("Title : " + Title + Environment.NewLine);
            s1.Write("Message : " + msg + Environment.NewLine);
            s1.Write("Date / Time : " + DateTime.Now.ToString() + Environment.NewLine);
            s1.Write("==============================================================================================" + Environment.NewLine);
            s1.Close();
            fs1.Close();

        }
        //public static void WriteToTestingLog(string msg, string Title)
        //{
        //    try
        //    {
        //        // SKIP EXCEPTION "Thread was being aborted" From Logging.
        //        if (msg.Contains("Thread was being aborted"))
        //            return;
        //        // Checking Directory If Not Exist Then Create It.
        //        if (!System.IO.Directory.Exists("C:\\TheMyriadPMSLog\\"))
        //        {
        //            System.IO.Directory.CreateDirectory("C:\\TheMyriadPMSLog\\");
        //        }

        //        // Checking File.
        //        FileStream fs = new FileStream("C:\\TheMyriadPMSLog\\TestingLog.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite);
        //        StreamWriter s = new StreamWriter(fs);

        //        s.Close();
        //        fs.Close();

        //        // Logging

        //        FileStream fs1 = new FileStream("C:\\TheMyriadPMSLog\\TestingLog.txt", FileMode.Append, FileAccess.Write);
        //        StreamWriter s1 = new StreamWriter(fs1);

        //        s1.Write("Title : " + Title + Environment.NewLine);
        //        s1.Write("Message : " + msg + Environment.NewLine);
        //        s1.Write("Date / Time : " + DateTime.Now.ToString() + Environment.NewLine);
        //        s1.Write("==============================================================================================" + Environment.NewLine);
        //        s1.Close();
        //        fs1.Close();
        //    }

        //    catch (Exception)
        //    {

        //    }

        //}

        public static void WriteToTestingLog(string msg, string Title)
        {
            try
            {
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                string filePath = Path.Combine(folderPath, "TestingLog.txt");

                // Checking Directory If Not Exist Then Create It.
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Logging
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.WriteLine("Title : " + Title);
                    sw.WriteLine("Message : " + msg);
                    sw.WriteLine("Date / Time : " + DateTime.Now.ToString());
                    sw.WriteLine("==============================================================================================");
                }
            }
            catch (Exception)
            {
                // Handle the exception as needed
            }
        }

    }
}