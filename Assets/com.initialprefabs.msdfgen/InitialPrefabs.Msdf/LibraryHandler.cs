using SharpFont;

namespace InitialPrefabs.Msdf {

    public class LibraryHandler {
        public static Library LoadLibrary() => new Library();

        public static void FreeLibrary(ref Library library) => library.Dispose();
    }
}
