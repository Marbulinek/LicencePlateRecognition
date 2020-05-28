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
            var spz = "TO250EL";
            LicencePlateRecognitionCore lpr = new LicencePlateRecognitionCore();
            var ecv = lpr.DetectLicencePlate(Resource.auto);
            Assert.AreEqual(spz, ecv);
        }
    }
}
