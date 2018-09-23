using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmpyrionChatAutoTranslate.Test
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void TestMethodTranslate()
        {
            var config = new Configuration();
            TranslateAPI.TranslateServiceUrl = config.TranslateServiceUrl;
            TranslateAPI.TanslateRespose     = config.TanslateRespose;

            Assert.AreEqual("This is a test text", TranslateAPI.Translate("de", "en", "Das ist eine test Text"));
        }
    }
}
