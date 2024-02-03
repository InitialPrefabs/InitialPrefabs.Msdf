using NUnit.Framework;

namespace InitialPrefabs.Msdf.Tests {

    public class LibraryHandlerTests {
        [Test]
        public void SharpFontInitialized() {
            var sharpfont = LibraryHandler.LoadLibrary();
            Assert.IsNotNull(sharpfont);
            Assert.IsFalse(sharpfont.IsDisposed, "The library interface was not loaded");

            LibraryHandler.FreeLibrary(ref sharpfont);
            Assert.IsTrue(sharpfont.IsDisposed, "The library interface was not disposed.");
        }
    }
}
