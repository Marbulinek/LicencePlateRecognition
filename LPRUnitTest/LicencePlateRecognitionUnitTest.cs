using System;
using LicencePlateRecognition;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LPRUnitTest
{
    [TestClass]
    public class LicencePlateRecognitionUnitTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            /// spz TO250EL
            var spz = "BL190MC";
            LicencePlateRecognitionCore lpr = new LicencePlateRecognitionCore();
            var ecv = lpr.DetectLicencePlate(Resource.auto);
            Assert.AreEqual(spz, ecv);
        }
    }
}
