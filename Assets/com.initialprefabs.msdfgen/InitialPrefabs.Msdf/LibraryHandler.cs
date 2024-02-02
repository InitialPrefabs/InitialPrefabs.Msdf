using SharpFont;

namespace InitialPrefabs.Msdf {

    public class LibraryHandler {
        public static Library LoadLibrary() => new Library();

        public static void FreeLibrary(Library library) => library.Dispose();
    }
}