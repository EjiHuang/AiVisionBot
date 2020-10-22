using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace ImageProcessor
{
    public class ImageRecognition
    {
        #region OpencvSharp version

        public static List<Point> GetSubPositionsOpenCV(Bitmap main, Bitmap sub, double threshold = 0.99)
        {
            List<Point> possiblepos = new List<Point>();

            var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(main);
            var template = OpenCvSharp.Extensions.BitmapConverter.ToMat(sub);

            OpenCvSharp.Cv2.CvtColor(src, src, OpenCvSharp.ColorConversionCodes.BGRA2GRAY);
            OpenCvSharp.Cv2.CvtColor(template, template, OpenCvSharp.ColorConversionCodes.BGRA2GRAY);

            using var result = new OpenCvSharp.Mat();
            OpenCvSharp.Cv2.MatchTemplate(src, template, result, OpenCvSharp.TemplateMatchModes.CCoeffNormed);
            OpenCvSharp.Cv2.Threshold(result, result, 0.8, 1.0, OpenCvSharp.ThresholdTypes.Tozero);

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

        public static List<List<Point>> GetSubPositionsOpenCV(Bitmap main, ObservableCollection<Bitmap> subs, double threshold = 0.99)
        {
            List<List<Point>> possiblepos_list = new List<List<Point>>();

            var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(main);
            var templates = new List<OpenCvSharp.Mat>();

            OpenCvSharp.Cv2.CvtColor(src, src, OpenCvSharp.ColorConversionCodes.BGRA2GRAY);
            for (int i = 0; i < subs.Count; i++)
            {
                templates.Add(OpenCvSharp.Extensions.BitmapConverter.ToMat(subs[i]));
                OpenCvSharp.Cv2.CvtColor(templates[i], templates[i], OpenCvSharp.ColorConversionCodes.BGRA2GRAY);

                using var result = new OpenCvSharp.Mat();
                OpenCvSharp.Cv2.MatchTemplate(src, templates[i], result, OpenCvSharp.TemplateMatchModes.CCoeffNormed);
                OpenCvSharp.Cv2.Threshold(result, result, 0.8, 1.0, OpenCvSharp.ThresholdTypes.Tozero);

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

            return possiblepos_list;
        }

        #endregion
    }
}
