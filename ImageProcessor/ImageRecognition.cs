using OpenCvSharp.ImgHash;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading;

namespace ImageProcessor
{
    public class ImageRecognition
    {
        #region OpencvSharp version

        public static OpenCvSharp.Window cvWin = new OpenCvSharp.Window("Test window");

        public static List<Point> GetSubPositionsOpenCV(Bitmap main, Bitmap sub, double threshold = 0.96)
        {
            List<Point> possiblepos = new List<Point>();

            using var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(main);
            using var template = OpenCvSharp.Extensions.BitmapConverter.ToMat(sub);

            OpenCvSharp.Cv2.CvtColor(src, src, OpenCvSharp.ColorConversionCodes.BGRA2GRAY);
            OpenCvSharp.Cv2.CvtColor(template, template, OpenCvSharp.ColorConversionCodes.BGRA2GRAY);

            using var result = TplMatch(src, template);

            while (true)
            {
                OpenCvSharp.Cv2.MinMaxLoc(result, out _, out double maxval, out _, out OpenCvSharp.Point maxloc);
                // Debug.WriteLine(maxval);
                if (threshold <= maxval)
                {
                    possiblepos.Add(new Point(maxloc.X, maxloc.Y));

                    // Fill in the res Mat so you don't find the same area again in the MinMaxLoc
                    OpenCvSharp.Cv2.FloodFill(result, maxloc, new OpenCvSharp.Scalar(0), out _, new OpenCvSharp.Scalar(0.1), new OpenCvSharp.Scalar(1.0));
                }
                else
                {
                    break;
                }
            }

            return possiblepos;
        }

        public static List<List<Point>> GetSubPositionsOpenCV(Bitmap main, ObservableCollection<Bitmap> subs, out Bitmap dst, double threshold = 0.96)
        {
            List<List<Point>> possiblepos_list = new List<List<Point>>();

            using var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(main);
            using var copy = src.Clone();
            var templates = new List<OpenCvSharp.Mat>();

            OpenCvSharp.Cv2.CvtColor(copy, copy, OpenCvSharp.ColorConversionCodes.BGRA2GRAY);

            for (int i = 0; i < subs.Count; i++)
            {
                templates.Add(OpenCvSharp.Extensions.BitmapConverter.ToMat(subs[i]));
                OpenCvSharp.Cv2.CvtColor(templates[i], templates[i], OpenCvSharp.ColorConversionCodes.BGRA2GRAY);

                using var result = TplMatch(copy, templates[i]);

                List<Point> possiblepos = new List<Point>();

                while (true)
                {
                    OpenCvSharp.Cv2.MinMaxLoc(result, out _, out double maxval, out _, out OpenCvSharp.Point maxloc);
                    // Debug.WriteLine(maxval);
                    if (threshold <= maxval)
                    {
                        possiblepos.Add(new Point(maxloc.X, maxloc.Y));

                        // Fill in the res Mat so you don't find the same area again in the MinMaxLoc
                        OpenCvSharp.Cv2.FloodFill(result, maxloc, new OpenCvSharp.Scalar(0), out _, new OpenCvSharp.Scalar(0.1), new OpenCvSharp.Scalar(1.0));
                    }
                    else
                    {
                        break;
                    }
                }

                possiblepos_list.Add(possiblepos);
            }

            dst = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(src, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            return possiblepos_list;
        }

        #endregion

        private static OpenCvSharp.Mat TplMatch(OpenCvSharp.Mat img, OpenCvSharp.Mat template)
        {
            var result = new OpenCvSharp.Mat();
            OpenCvSharp.Cv2.MatchTemplate(img, template, result, OpenCvSharp.TemplateMatchModes.CCoeffNormed);
            OpenCvSharp.Cv2.Threshold(result, result, 0.8, 1.0, OpenCvSharp.ThresholdTypes.Tozero);
            // OpenCvSharp.Cv2.Normalize(result, result, 0, 1, OpenCvSharp.NormTypes.MinMax, -1);

            return result;
        }
    }
}
