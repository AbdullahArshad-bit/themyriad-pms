using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PMS.Classes
{
    public static class RdlcHelper<T>
    {
        public static Byte[] LocalReport(List<T> list, string ReportPath, string DataSetName, out string strMimeType, List<ReportParameter> parameters = null, string format = null)
        {
            byte[] bytes = null;
            string strEncoding = "";
            string strExtension = "";
            string[] strStreams = null;
            Warning[] warnings = null;
            string deviceInfo = "<DeviceInfo>" +
                " <OutputFormat>PDF</OutputFormat>" +
                " <PageWidth>8.27in</PageWidth>" +
                " <PageHeight>11.69in</PageHeight>" +
                " <MarginTop>0.5in</MarginTop>" +
                " <MarginLeft>0.5in</MarginLeft>" +
                " <MarginRight>0.5in</MarginRight>" +
                " <MarginBottom>0.5in</MarginBottom>" +
                " <EmbedFonts>None</EmbedFonts>" +
                "</DeviceInfo>";

            ReportDataSource rd = new ReportDataSource(DataSetName, list);
            LocalReport rptViewer1 = new LocalReport();
            rptViewer1.ReportPath = System.Web.HttpContext.Current.Server.MapPath(ReportPath);
            rptViewer1.DataSources.Add(rd);

            if (parameters != null && parameters.Any())
            {
                rptViewer1.SetParameters(parameters);
            }

            rptViewer1.Refresh();
            bytes = rptViewer1.Render(String.IsNullOrEmpty(format) ? "PDF" : format,
                deviceInfo, out strMimeType, out strEncoding, out strExtension, out strStreams, out warnings);
            return bytes;
        }
        public static byte[] LocalReportMultiDatasouce(ReportDataSource mainDataSource, List<ReportDataSource> subreports, string ReportPath, out string strMimeType, string format = null)
        {
            byte[] bytes = null;
            //string strDeviceInfo = "";
            string strEncoding = "";
            string strExtension = "";
            string[] strStreams = null;
            Warning[] warnings = null;
            string deviceInfo = "<DeviceInfo>" +
                " <OutputFormat>PDF</OutputFormat>" +
                " <PageWidth>8.27in</PageWidth>" +
                " <PageHeight>11.69in</PageHeight>" +
                " <MarginTop>0.5in</MarginTop>" +
                " <MarginLeft>0.5in</MarginLeft>" +
                " <MarginRight>0.5in</MarginRight>" +
                " <MarginBottom>0.5in</MarginBottom>" +
                " <EmbedFonts>None</EmbedFonts>" +
                "</DeviceInfo>";
            LocalReport rptViewer1 = new LocalReport();
            rptViewer1.DataSources.Add(mainDataSource);
            rptViewer1.ReportPath = System.Web.HttpContext.Current.Server.MapPath(ReportPath);
            rptViewer1.Refresh();

            void MySubreportEventHandler(object sender, SubreportProcessingEventArgs e)
            {
                foreach (var item in subreports)
                {
                    e.DataSources.Add(item);

                }
            };
            rptViewer1.SubreportProcessing += new SubreportProcessingEventHandler(MySubreportEventHandler);

            bytes = rptViewer1.Render(String.IsNullOrEmpty(format) ? "PDF" : format,
                deviceInfo, out strMimeType, out strEncoding, out strExtension, out strStreams, out warnings);
            return bytes;
        }



    }


}