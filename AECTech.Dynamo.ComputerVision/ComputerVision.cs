using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DS = Autodesk.DesignScript.Geometry;
using Dynamo.Graph.Nodes;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Interfaces;

namespace AECTech.Dynamo.ComputerVision
{
    /// <summary>
    /// Image to Dynamo objects (WIP)
    /// </summary>
    public static class DynaVision
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        /// <search>
        /// bitmap, image, pixel, point, points
        /// </search>
        [NodeCategory("Actions")]
        public static List<IGraphicItem> PixelPoints(Bitmap Image)
        {
            if (Image == null)
                throw new Exception("Invalid Image");

            
            using (Image<Rgb, byte> img = Image.ToImage<Rgb, byte>())
            {
                List<DS.Point> points = new List<DS.Point>();
                List<IGraphicItem> colorPts = new List<IGraphicItem>();

                for (int x = 0; x < img.Rows - 1; x++)
                {
                    for (int y = 0; y < img.Cols - 1; y++)
                    {
                        var redVal = img.Data[x, y, 0];
                        var greenVal = img.Data[x, y, 1];
                        var blueVal = img.Data[x, y, 2];
                        DSCore.Color rgb = DSCore.Color.ByARGB(255, (int)redVal, (int)greenVal, (int)blueVal);

                        DS.Point pt = DS.Point.ByCoordinates(x, y);

                        points.Add(pt);
                        //Custom Extension .UpdateColorByGeometry(rgb)
                        //See Geometry preview class for more info
                        colorPts.Add(Modifiers.GeometryColor.ByGeometryColor(pt, rgb));
                    }
                }
                return colorPts;
            }
        }

        /// <summary>
        /// Find Contours Lines
        /// </summary>
        /// <param name="Image"></param>
        /// <returns name="Image">System.Bitmap</returns>
        /// <returns name="Lines[]">Autodesk.DesignScript.Geometry.Polygon</returns>
        /// <search>
        /// bitmap, image, find, contour, lines
        /// </search>
        [MultiReturn(new[] { "Image", "Lines[]" })]
        [NodeCategory("Actions")]
        public static Dictionary<string, dynamic> FindContourLines(Bitmap Image)
        {
            if (Image == null)
                throw new Exception("Invalid Image");

            using (Image<Gray, byte> imgOutput = Image.ToImage<Gray, byte>())
            using (Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint())
            using (Mat hier = new Mat())
            using (Image<Gray, byte> imgout = imgOutput.CopyBlank())
            {
                CvInvoke.FindContours(imgOutput, contours, hier, Emgu.CV.CvEnum.RetrType.List, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

                List<dynamic> lines = new List<dynamic>();

                for (int i = 0; i < contours.Size; i++)
                {
                    List<DS.Point> points = new List<DS.Point>();
                    double perimeter = CvInvoke.ArcLength(contours[i], true);
                    VectorOfPoint approx = new VectorOfPoint();

                    CvInvoke.ApproxPolyDP(contours[i], approx, 0.04 * perimeter, true);

                    for (int j = 0; j < approx.Size; j++)
                    {
                        CvInvoke.DrawContours(imgout, contours, -1, new MCvScalar(255, 0, 0));

                        int x = approx[j].X;
                        int y = approx[j].Y;
                        DS.Point p = DS.Point.ByCoordinates(x, y, 0);

                        points.Add(p);
                    }
                    if (points.Count() > 1)
                    {
                        for (int k = 0; k < points.Count() - 1; k++)
                        {
                            DS.Line l = DS.Line.ByStartPointEndPoint(points[k], points[k + 1]);
                            lines.Add(l);
                        }
                        DS.Line lastLine = DS.Line.ByStartPointEndPoint(points[points.Count() - 1], points[0]);
                        lines.Add(lastLine);
                    }

                    approx.Dispose();
                }

                return new Dictionary<string, dynamic>()
                {
                    { "Image", imgout.ToBitmap() },
                    { "Lines[]", lines }
                };
            }

        }

        /// <summary>
        /// Hough Circles
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="dp"></param>
        /// <param name="minDist"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="minRadius"></param>
        /// <param name="maxRadius"></param>
        /// <returns></returns>
        /// <search>
        /// bitmap, image, hough, circles
        /// </search>
        [MultiReturn(new[] { "Image", "Circles" })]
        [NodeCategory("Actions")]
        public static Dictionary<string, dynamic> HoughCircles(
            Bitmap Image
            , double dp = 1
            , double minDist = 20
            , double param1 = 50
            , double param2 = 30
            , int minRadius = 0
            , int maxRadius = 0)
        {
            if (Image == null)
                throw new Exception("Invalid Image");

            using (Image<Gray, byte> imgInput = Image.ToImage<Gray, byte>())
            using (Image<Gray, byte> imgOutput = imgInput.CopyBlank())
            {
                List<DS.Circle> dsCircles = new List<DS.Circle>();
                var circles = CvInvoke.HoughCircles(imgInput, Emgu.CV.CvEnum.HoughModes.Gradient, dp, minDist, param1, param2, minRadius, maxRadius);
                foreach (var circle in circles)
                {
                    imgOutput.Draw(circle, new Gray(255), 1);
                    using (DS.Point center = DS.Point.ByCoordinates(circle.Center.X, circle.Center.Y))
                    {
                        dsCircles.Add(DS.Circle.ByCenterPointRadius(center, circle.Radius));
                    }
                }

                return new Dictionary<string, dynamic>()
                {
                    { "Image", imgOutput.ToBitmap() },
                    { "Circles", dsCircles }
                };
            }
        }

    }
    /// <summary>
    /// ComputerVision header
    /// </summary>
    public static class ComputerVision
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Image"></param>
        /// <returns name="Image">System.Drawing.Bitmap</returns>
        /// <search>
        /// bitmap, image, convert, gray
        /// </search>
        [NodeCategory("Actions")]
        public static Bitmap ConvertGray(Bitmap Image)
        {
            if (Image == null)
                throw new Exception("Invalid Image");
            using (Image<Rgb, byte> cvImage = Image.ToImage<Rgb, byte>())
            using (Image<Gray, byte> grayImage = cvImage.Convert<Gray, byte>())
            {
                //Cannot use .Bitmap properties of Image as it will be dispose eventually
                return grayImage.ToBitmap();
            }
        }

        /// <summary>
        /// http://www.emgu.com/wiki/files/3.2.0/document/html/56daa340-abb8-ea31-8509-987b730b366e.htm
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="kernelSize"></param>
        /// <returns name="Image">System.Drawing.Bitmap</returns>
        /// <search>
        /// bitmap, image, morphology, close
        /// </search>
        [NodeCategory("Actions")]
        public static Bitmap MorphologyClose(Bitmap Image, int kernelSize = 10)
        {
            if (Image == null)
                throw new Exception("Invalid Image");
            using (Image<Rgb, byte> grayImage = Image.ToImage<Rgb, byte>())
            using (Mat kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(kernelSize, kernelSize), new Point(-1, -1)))
            using (Image<Rgb, byte> morphed = grayImage.MorphologyEx(Emgu.CV.CvEnum.MorphOp.Close, kernel, new Point(-1, -1), -1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(1.0)))
            {
                //Cannot use .Bitmap properties of Image as it will be dispose eventually
                return morphed.ToBitmap();
            }
        }

        /// <summary>
        /// http://www.emgu.com/wiki/files/3.2.0/document/html/56daa340-abb8-ea31-8509-987b730b366e.htm
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="kernelSize"></param>
        /// <returns name="Image">System.Drawing.Bitmap</returns>
        /// <search>
        /// bitmap, image, morphology erode
        /// </search>
        [NodeCategory("Actions")]
        public static Bitmap MorphologyErode(Bitmap Image, int kernelSize = 10)
        {
            if (Image == null)
                throw new Exception("Invalid Image");
            using (Image<Rgb, byte> grayImage = Image.ToImage<Rgb, byte>())
            using (Mat kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(kernelSize, kernelSize), new Point(-1, -1)))
            using (Image<Rgb, byte> morphed = grayImage.MorphologyEx(Emgu.CV.CvEnum.MorphOp.Erode, kernel, new Point(-1, -1), -1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(1.0)))
            {
                //Cannot use .Bitmap properties of Image as it will be dispose eventually
                return morphed.ToBitmap();
            }
        }

        /// <summary>
        /// http://www.emgu.com/wiki/files/3.2.0/document/html/56daa340-abb8-ea31-8509-987b730b366e.htm
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="kernelSize"></param>
        /// <returns name="Image">System.Drawing.Bitmap</returns>
        /// <search>
        /// bitmap, image, morphology, dilate
        /// </search>
        [NodeCategory("Actions")]
        public static Bitmap MorphologyDilate(Bitmap Image, int kernelSize = 10)
        {
            if (Image == null)
                throw new Exception("Invalid Image");
            using (Image<Rgb, byte> grayImage = Image.ToImage<Rgb, byte>())
            using (Mat kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(kernelSize, kernelSize), new Point(-1, -1)))
            using (Image<Rgb, byte> morphed = grayImage.MorphologyEx(Emgu.CV.CvEnum.MorphOp.Dilate, kernel, new Point(-1, -1), -1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(1.0)))
            {
                //Cannot use .Bitmap properties of Image as it will be dispose eventually
                return morphed.ToBitmap();
            }
        }

        /// <summary>
        /// http://www.emgu.com/wiki/files/3.2.0/document/html/57c39007-52be-264e-ff3a-5fb13dd54ff2.htm
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="edgeThreshold"></param>
        /// <param name="linkThreshold"></param>
        /// <param name="apertureSize"></param>
        /// <param name="l2Gradient"></param>
        /// <returns name="Image">System.Drawing.Bitmap</returns>
        /// <search>
        /// bitmap, image, canny
        /// </search>
        [NodeCategory("Actions")]
        public static Bitmap Canny(Bitmap Image, double edgeThreshold = 20, double linkThreshold = 50, int apertureSize = 3, bool l2Gradient = true)
        {
            if (Image == null)
                throw new Exception("Invalid Image");
            using (Image<Gray, byte> grayImage = Image.ToImage<Gray, byte>())
            using (Image<Gray, byte> imgCanny = grayImage.Canny(edgeThreshold, linkThreshold, apertureSize, l2Gradient))
            {
                //Cannot use .Bitmap properties of Image<> as it will be dispose eventually
                return imgCanny.ToBitmap();
            }
        }

        /// <summary>
        /// http://www.emgu.com/wiki/files/3.2.0/document/html/6a5bee61-41d2-a4a7-2c32-c489b78b90d9.htm
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="xOrder"></param>
        /// <param name="yOrder"></param>
        /// <param name="apertureSize"></param>
        /// <returns name="Image">System.Drawing.Bitmap</returns>
        /// <search>
        /// bitmap, image, sobel
        /// </search>
        [NodeCategory("Actions")]
        public static Bitmap Sobel(Bitmap Image, int xOrder = 1, int yOrder = 0, int apertureSize = 3)
        {
            if (Image == null)
                throw new Exception("Invalid Image");
            using (Image<Rgb, float> cvImage = Image.ToImage<Rgb, float>())
            using (Image<Rgb, float> imgSobel = cvImage.Sobel(xOrder, yOrder, apertureSize))
            {
                //Cannot use .Bitmap properties of Image as it will be dispose eventually
                return imgSobel.ToBitmap();
            }
        }

        /// <summary>
        /// http://www.emgu.com/wiki/files/3.2.0/document/html/f84e3cc0-0624-36a0-7281-cd2d08bec4bc.htm
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="apertureSize">Aperture Size</param>
        /// <returns name="Image">System.Drawing.Bitmap</returns>
        /// <search>
        /// bitmap, image, laplacian
        /// </search>
        [NodeCategory("Actions")]
        public static Bitmap Laplacian(Bitmap Image, int apertureSize = 3)
        {
            if (Image == null)
                throw new Exception("Invalid Image");
            using (Image<Rgb, float> cvImage = Image.ToImage<Rgb, float>())
            using (Image<Rgb, float> imgLaplace = cvImage.Laplace(apertureSize))
            {
                //Cannot use .Bitmap properties of Image as it will be dispose eventually
                return imgLaplace.ToBitmap();
            }
        }

        /// <summary>
        /// http://www.emgu.com/wiki/files/1.4.0.0/html/15f22443-ef80-1e33-4f68-a1dd9fc1f370.htm
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="kernelSize"></param>
        /// <param name="sigma1"></param>
        /// <param name="sigma2"></param>
        /// <returns name="Image">System.Drawing.Bitmap</returns>
        /// <search>
        /// bitmap, image, smooth, gaussian
        /// </search>
        [NodeCategory("Actions")]
        public static Bitmap SmoothGaussian(Bitmap Image, int kernelSize = 3, double sigma1 = 1, double sigma2 = 1)
        {
            if (Image == null)
                throw new Exception("Invalid Image");
            using (Image<Rgb, float> cvImage = Image.ToImage<Rgb, float>())
            using (Image<Rgb, float> imgSmoothGaussian = cvImage.SmoothGaussian(kernelSize, kernelSize, sigma1, sigma2))
            {
                //Cannot use .Bitmap properties of Image as it will be dispose eventually
                return imgSmoothGaussian.ToBitmap();
            }
        }

        ///// <summary>
        ///// http://www.emgu.com/wiki/files/3.2.0/document/html/cb24a129-d9ce-57f3-19ad-0eaa27a77317.htm
        ///// </summary>
        ///// <param name="Image"></param>
        ///// <returns name="Image">System.Drawing.Bitmap</returns>
        //public static Bitmap FindContours(Bitmap Image)
        //{
        //    if (Image == null)
        //        throw new Exception("Invalid Image");
        //    using (Image<Gray, byte> imgOutput = new Image<Gray, byte>(Image))
        //    using (Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint())
        //    using (Mat hier = new Mat())
        //    using (Image<Gray, byte> imgout = imgOutput.CopyBlank())
        //    {
        //        CvInvoke.FindContours(imgOutput, contours, hier, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
        //        CvInvoke.DrawContours(imgout, contours, -1, new MCvScalar(255, 0, 0));
        //        //Cannot use .Bitmap properties of Image as it will be dispose eventually
        //        return imgout.ToBitmap();
        //    }
        //}


        //
        //public static List<DS.Line> HoughLine(Bitmap Image, int threshold = 150 )
        //{
        //    if (Image == null)
        //        throw new Exception("Invalid Image");
        //    using (Image<Gray, byte> imgInput = new Image<Gray, byte>(Image))
        //    {
        //        //No need to dispose
        //        LineSegment2D[] houghP = CvInvoke.HoughLinesP(imgInput, 1, Math.PI / 180, threshold);

        //        var lines = houghP.Select(x =>
        //        {
        //            using (DS.Point sp = DS.Point.ByCoordinates(x.P1.X, x.P1.Y, 0))
        //            using (DS.Point ep = DS.Point.ByCoordinates(x.P2.X, x.P2.Y, 0))
        //            {
        //                return DS.Line.ByStartPointEndPoint(sp, ep);
        //            }
        //        }).ToList();
        //        return lines;
        //        /*//LineSegment2D[][] hough = imgInput.HoughLines(1,1,1,1,5,1,1);
        //        //List<DS.Line> lines = new List<DS.Line>();
        //        int[] spX = houghP.Select(x => x.P1.X).Cast<int>().ToArray();
        //        foreach (var h in houghP)
        //        {
        //            var xx = h.P1.X;

        //            lines.Add(DS.Line.ByStartPointEndPoint(DS.Point.ByCoordinates(h.P1.X, h.P1.Y, 0), DS.Point.ByCoordinates(h.P2.X, h.P2.Y, 0)));
        //        }
        //        return lines;*/
        //    }
        //}


        /// <summary>
        /// Threshold Binary Inverted
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="Threshold"></param>
        /// <param name="MaxValue"></param>
        /// <returns name="Image">System.Bitmap</returns>
        /// <search>
        /// bitmap, image, threshold, binary, inv
        /// </search>
        public static Bitmap ThresholdBinaryInv(Bitmap Image, double Threshold = 233, double MaxValue = 255)
        {
            if (Image == null)
                throw new Exception("Invalid Image");

            using (Image<Gray, byte> imgOutput = Image.ToImage<Gray, byte>().ThresholdBinaryInv(new Gray(Threshold), new Gray(MaxValue)))
            {
                return imgOutput.ToBitmap();
            }
        }

        /// <summary>
        /// Get Bitmap image height in pixels
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        /// <search>
        /// bitmap, image, height
        /// </search>
        [NodeCategory("Query")]
        public static int Height(Bitmap Image)
        {
            if (Image == null)
                throw new Exception("Invalid Image");
            return Image.Height;
        }

        /// <summary>
        /// Get Bitmap image width in pixels
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        /// <search>
        /// bitmap, image, width
        /// </search>
        [NodeCategory("Query")]
        public static int Width(Bitmap Image)
        {
            if (Image == null)
                throw new Exception("Invalid Image");
            return Image.Width;
        }

        /// <summary>
        /// Region of Interest
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="MinX"></param>
        /// <param name="MinY"></param>
        /// <param name="MaxX"></param>
        /// <param name="MaxY"></param>
        /// <returns name= "Image">System.Drawing.Bitmap</returns>
        /// <search>
        /// bitmap, image, roi
        /// </search>
        [NodeCategory("Query")]
        public static Bitmap ROI(Bitmap Image, int MinX = 0, int MinY = 0, int MaxX = 1, int MaxY = 1)
        {
            if (Image == null)
                throw new Exception("Invalid Image");
            if (MaxX > Image.Width)
                throw new Exception("X value cannot be more than Image's Width");
            if (MaxY > Image.Height)
                throw new Exception("Y value cannot be more than Image's Height");
            if (MinX < 0 || MinY < 0)
                throw new Exception("Min value cannot be lesser than 0");
            Size sz = new Size(MaxX - MinX, MaxY - MinY);
            Point p = new Point(MinX, 0);

            Rectangle rect = new Rectangle(p, sz);

            using (Image<Rgba, byte> img = Image.ToImage<Rgba, byte>())
            {
                img.ROI = rect;
                return img.ToBitmap();
            }
        }

    }
}
