using System;
using System.Drawing;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Patagames.Ocr;
using Patagames.Ocr.Enums;

namespace LicencePlateRecognition
{
    public class LicencePlateRecognitionCore
    {
        private const string RESULT_PLATE = "result.jpg";

        public string DetectLicencePlate(Bitmap image)
        {
            Image<Bgr, Byte> originalImage = new Image<Bgr, byte>(image);
            //originalImage = originalImage.Resize(500, 500, Emgu.CV.CvEnum.Inter.Linear, true);

            //Convert the image to grayscale and filter out the noise
            Image<Gray, Byte> grayImage = originalImage.Convert<Gray, Byte>();

            //use image pyr to remove noise
            UMat pyrDown = new UMat();
            CvInvoke.PyrDown(grayImage, pyrDown);
            CvInvoke.PyrUp(pyrDown, grayImage);

            // Noise removal with iterative bilateral filter(removes noise while preserving edges)
            Mat filteredImage = new Mat();
            CvInvoke.BilateralFilter(grayImage, filteredImage, 11, 17, 17);

            ///Find Edges of the grayscale image
            Mat edges = new Mat();
            CvInvoke.Canny(filteredImage, edges, 120, 200);

            filteredImage.Save("medziproj.jpg");

            // Find contours
            Mat hierarchy = new Mat();
            var edgesCopy = edges.Clone();
            var contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(edgesCopy, contours, hierarchy, Emgu.CV.CvEnum.RetrType.List, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
            
            //Create copy of original image to draw all contours
            var copyOriginalImage = originalImage.Clone();
            CvInvoke.DrawContours(copyOriginalImage, contours, -1, new MCvScalar(0, 255, 0, 255), 3);
            

            var newContoursArray = new VectorOfVectorOfPoint();

            int count = contours.Size;
            for (int i = 1; i < count; i++)
            {
                using (VectorOfPoint contour = contours[i])
                {
                    if(CvInvoke.ContourArea(contour, false) > 60)
                    {
                        newContoursArray.Push(contour);
                    }
                }

            }

            //Create copy of original image to draw all contours
            var copyOriginalImage2 = originalImage.Clone();
            CvInvoke.DrawContours(copyOriginalImage2, newContoursArray, -1, new MCvScalar(0, 255, 0, 255), 3);

            var numberPlateVectorArray = new VectorOfVectorOfPoint();
            var resultRectangle = new Rectangle();

            for(int i=0; i<newContoursArray.Size; i++)
            {
                var c = newContoursArray[i];
                var peri = CvInvoke.ArcLength(c, true);
                var approx = new VectorOfPoint();
                CvInvoke.ApproxPolyDP(c, approx, (0.02 * peri), true);

                ////mame 4hranu
                if(approx.Size == 4)
                {
                    //determine if all the angles in the contour are within [80, 100] degree
                    bool isRectangle = true;
                    LineSegment2D[] edgesArr = PointCollection.PolyLine(approx.ToArray(), true);

                    for (int j = 0; j < edgesArr.Length; j++)
                    {
                        double angle = Math.Abs(
                           edgesArr[(j + 1) % edgesArr.Length].GetExteriorAngleDegree(edgesArr[j]));
                        if (angle < 80 || angle > 100)
                        {
                            isRectangle = false;
                            break;
                        }
                    }

                    if (isRectangle)
                    {
                        numberPlateVectorArray.Push(approx);
                        resultRectangle = CvInvoke.BoundingRectangle(c);
                        break;
                    }

                }

            }

            var originalImage3 = originalImage.Clone();
            //# Drawing the selected contour on the original image
            CvInvoke.DrawContours(originalImage3, numberPlateVectorArray, -1, new MCvScalar(0, 255, 0), 3);

            //// save cropped image
            originalImage.ROI = resultRectangle;
            originalImage = originalImage.Copy();
            CvInvoke.cvResetImageROI(originalImage);

            //originalImage = originalImage.Resize(200, 200, Emgu.CV.CvEnum.Inter.Linear, true);
            originalImage.Save(RESULT_PLATE);
            return this.RecognizeText(RESULT_PLATE);
        }

        private string RecognizeText(string path)
        {
            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English, "./", OcrEngineMode.OEM_TESSERACT_CUBE_COMBINED);
                string plainText = api.GetTextFromImage(path);
                plainText = Regex.Replace(plainText, "[^a-zA-Z0-9]", "").ToUpper();
                if(plainText.Length >= 8)
                {
                    plainText = plainText.Remove(2,1).ToString();
                }
                return plainText;
            }
        }
    }
}
