using System;
using System.Drawing;
using System.Text.RegularExpressions;
using Emgu.CV;
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
            Image<Hsv, Byte> originalImage = new Image<Hsv, byte>(image);
            Image<Gray, Byte> grayImage = originalImage.Convert<Gray, Byte>();

            // Noise removal with iterative bilateral filter(removes noise while preserving edges)
            Mat filteredImage = new Mat();
            CvInvoke.BilateralFilter(grayImage, filteredImage, 11, 17, 17);

            ///Find Edges of the grayscale image
            Mat edges = new Mat();
            CvInvoke.Canny(filteredImage, edges, 170, 200);

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
                    numberPlateVectorArray.Push(approx);
                    //x, y, w, h = cv2.boundingRect(c)
                    resultRectangle = CvInvoke.BoundingRectangle(c);
                    break;
                }

            }

            var originalImage3 = originalImage.Clone();
            //# Drawing the selected contour on the original image
            CvInvoke.DrawContours(originalImage3, numberPlateVectorArray, -1, new MCvScalar(0, 255, 0), 3);

            //// save cropped image
            originalImage.ROI = resultRectangle;
            originalImage = originalImage.Copy();
            CvInvoke.cvResetImageROI(originalImage);


            originalImage.Save(RESULT_PLATE);
            return this.RecognizeText(RESULT_PLATE);
        }

        private string RecognizeText(string path)
        {
            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English, "./");
                string plainText = api.GetTextFromImage(path);
                plainText = Regex.Replace(plainText, "[^a-zA-Z0-9]", "").ToUpper();
                return plainText;
            }
        }
    }
}
