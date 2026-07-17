using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace PMS.StudentApi.Classes
{
    public static class PdfHelper
    {
        public static MemoryStream GetPdfByHtml(string Html,string font= "ARIAL.TTF")
        {
            FontOverrider fontOverrider = new FontOverrider(Environment.GetEnvironmentVariable("windir") + @"\fonts\"+font);
            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.LETTER, 15f, 15f, 10f, 0f);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();
                using (var msCss = new MemoryStream(Encoding.UTF8.GetBytes("")))
                {
                    using (var msHtml = new MemoryStream(Encoding.UTF8.GetBytes(Html)))
                    {
                        XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, msHtml, msCss, Encoding.UTF8, fontOverrider);
                    }
                }

                pdfDoc.Close();
                return stream;
            }
        }
    }
    public class FontOverrider : FontFactoryImp
    {
        private readonly BaseFont baseFont;
        public FontOverrider(string path, string encoding = BaseFont.IDENTITY_H, bool embedded = BaseFont.EMBEDDED)
        {
            if (!System.IO.File.Exists(path))
            {
                throw new FileNotFoundException("Could not find the supplied font file", path);
            }

            baseFont = BaseFont.CreateFont(path, encoding, embedded);

        }

        public override Font GetFont(string fontname, string encoding, bool embedded, float size, int style, BaseColor color, bool cached)
        {
            return new Font(baseFont, size, style, color);
        }
    }
}