using InitialPrefabs.Msdf.EditorExtensions;
using NUnit.Framework;
using System;

namespace InitialPrefabs.Msdf.Tests {
    public class LibraryHandlerTests {

        [Test]
        public void LoadAndUnloadTests() {
            Assert.IsTrue(LibraryHandler.GetLibraryPath("msdf_atlas", out var path), "msdf_atlas.dll was not found!");
            var lib = LibraryHandler.InitializeLibrary(path);
            Assert.AreNotEqual(IntPtr.Zero, lib);

            LibraryHandler.ReleaseLibrary(lib);
        }
    }
}
