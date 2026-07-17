using nQuant;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Common
{
    public class ImageUpload
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public long Quality { get; set; }

        private readonly string UploadPath = "/Assets/Images/Uploads";

        public ImageUpload()
        {
            Quality = 100;
        }

        public ImageResult RenameUploadFile(HttpPostedFileBase file, Int32 counter = 0)
        {
            var fileName = Path.GetFileName(file.FileName);

            //string prepend = "item_" + DateTime.Now.ToString("dd_MMM_yyyy_HH-mm-ss") + "_";
            //string finalFileName = prepend + ((counter).ToString()) + "_" + fileName;

            string finalFileName = DateTime.Now.ToString("ddMMyyyy-HHmmss") + "-" + Guid.NewGuid() + Path.GetExtension(fileName);

            if (!Directory.Exists(HttpContext.Current.Request.MapPath(UploadPath)))
                Directory.CreateDirectory(HttpContext.Current.Request.MapPath(UploadPath));

            if (System.IO.File.Exists(HttpContext.Current.Request.MapPath(UploadPath + finalFileName)))
            {
                //file exists => add country try again
                return RenameUploadFile(file, ++counter);
            }
            //file doesn't exist, upload item but validate first
            var ret = UploadFile(file, finalFileName);
            ret.ImageName = Common.Helper.GetBaseUrlWeb(HttpContext.Current.Request) + UploadPath + "/" + ret.ImageName;

            return ret;
        }



        public ImageResult RenameUploadFileNew(HttpPostedFileBase file, Int32 counter = 0)
        {
            var fileName = Path.GetFileName(file.FileName);

            //string prepend = "item_" + DateTime.Now.ToString("dd_MMM_yyyy_HH-mm-ss") + "_";
            //string finalFileName = prepend + ((counter).ToString()) + "_" + fileName;

            string finalFileName = DateTime.Now.ToString("ddMMyyyy-HHmmss") + "-" + Guid.NewGuid() + Path.GetExtension(fileName);

            if (!Directory.Exists(HttpContext.Current.Request.MapPath(UploadPath)))
                Directory.CreateDirectory(HttpContext.Current.Request.MapPath(UploadPath));

            if (System.IO.File.Exists(HttpContext.Current.Request.MapPath(UploadPath + finalFileName)))
            {
                //file exists => add country try again
                return RenameUploadFile(file, ++counter);
            }
            //file doesn't exist, upload item but validate first
            var ret = UploadFile(file, finalFileName);
            ret.ImageName =  UploadPath + "/" + ret.ImageName;

            return ret;
        }


        private ImageResult UploadFile(HttpPostedFileBase file, string fileName)
        {
            ImageResult imageResult = new ImageResult { Success = true, ErrorMessage = null };

            var path =
          Path.Combine(HttpContext.Current.Server.MapPath(UploadPath), fileName);
            string extension = Path.GetExtension(file.FileName);

            //make sure the file is valid
            if (!ValidateExtension(extension))
            {
                imageResult.Success = false;
                imageResult.ErrorMessage = "Invalid Extension";
                return imageResult;
            }

            try
            {
                Image imgOriginal = Image.FromStream(file.InputStream, true, true);
                //Image imgActual = Scale(imgOriginal);
                //CpmpressImage(imgActual, path);

                Image img = Resize(imgOriginal, Width, Height);
                SaveImage(img, path);

                imageResult.ImageName = fileName;

                imgOriginal.Dispose();
                //imgActual.Dispose();

                return imageResult;
            }
            catch (Exception ex)
            {
                // you might NOT want to show the exception error for the user
                // this is generaly logging or testing

                imageResult.Success = false;
                imageResult.ErrorMessage = ex.Message;
                return imageResult;
            }
        }

        private void CpmpressImage(Image img, string destPath)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                using (Bitmap bmp1 = new Bitmap(img))
                {

                    ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

                    System.Drawing.Imaging.Encoder QualityEncoder = System.Drawing.Imaging.Encoder.Quality;

                    EncoderParameters myEncoderParameters = new EncoderParameters(1);

                    EncoderParameter myEncoderParameter = new EncoderParameter(QualityEncoder, Quality);

                    myEncoderParameters.Param[0] = myEncoderParameter;
                    //bmp1.MakeTransparent();
                    bmp1.Save(destPath, jpgEncoder, myEncoderParameters);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public static string SaveFile(HttpPostedFileBase file, string path)
        {
            if (file == null)
            {
                return "placeholder.png";
            }
            string FileNameExt = Path.GetExtension(file.FileName);
            string FileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
            string NewFileName = "IMG-" + path + "-" + FileNameWithoutExt + "-" + DateTime.Now.ToString("MMddyyHHmmss") + FileNameExt;
            string folderexist = HttpContext.Current.Server.MapPath("~/Upload/Files/" + path + "/");
            bool isExist = System.IO.Directory.Exists(folderexist);
            if (!isExist)
            {
                System.IO.Directory.CreateDirectory(folderexist);
            }
            file.SaveAs(folderexist + NewFileName);
            return NewFileName.ToString();
        }







        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private bool ValidateExtension(string extension)
        {
            extension = extension.ToLower();
            switch (extension)
            {
                case ".jpg":
                    return true;
                case ".png":
                    return true;
                case ".gif":
                    return true;
                case ".jpeg":
                    return true;
                case ".svg":
                    return true;
                default:
                    return false;
            }
        }


        private Image Scale(Image imgPhoto)
        {
            float sourceWidth = imgPhoto.Width;
            float sourceHeight = imgPhoto.Height;
            float destHeight = 0;
            float destWidth = 0;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            // force resize, might distort image
            if (Width != 0 && Height != 0)
            {
                destWidth = Width;
                destHeight = Height;
            }
            else if (Width == 0 && Height == 0)
            {
                destWidth = sourceWidth;
                destHeight = sourceHeight;
            }
            // change size proportially depending on width or height
            else if (Height != 0)
            {
                destWidth = (float)(Height * sourceWidth) / sourceHeight;
                destHeight = Height;
            }
            else
            {
                destWidth = Width;
                destHeight = (float)(sourceHeight * Width / sourceWidth);
            }

            Bitmap bmPhoto = new Bitmap((int)destWidth, (int)destHeight,
                                        PixelFormat.Format32bppPArgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, (int)destWidth, (int)destHeight),
                new Rectangle(sourceX, sourceY, (int)sourceWidth, (int)sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();

            return bmPhoto;
        }


        private void SaveImage(Image img, string destPath)
        {
            try
            {
                var quantizer = new WuQuantizer();
                using (var quantized = quantizer.QuantizeImage(new Bitmap(img)))
                {
                    quantized.Save(destPath, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private Image Resize(Image imgPhoto, int Width, int Height)
        {
            if (Width != 0 && Height != 0)
            {

            }
            else if (Width == 0 && Height == 0)
            {
                Width = imgPhoto.Width;
                Height = imgPhoto.Height;
            }
            // change size proportially depending on width or height
            else if (Height != 0)
            {
                Width = (Height * imgPhoto.Width) / imgPhoto.Height;
            }
            else
            {
                Height = (imgPhoto.Height * Width / imgPhoto.Width);
            }

            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)Width / (float)sourceWidth);
            nPercentH = ((float)Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = System.Convert.ToInt16((Width -
                              (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = System.Convert.ToInt16((Height -
                              (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap bmPhoto = new Bitmap(Width, Height,
                              PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                             imgPhoto.VerticalResolution);
            bmPhoto.MakeTransparent();

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            //grPhoto.Clear(Color.White);
            grPhoto.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }
    }
}
