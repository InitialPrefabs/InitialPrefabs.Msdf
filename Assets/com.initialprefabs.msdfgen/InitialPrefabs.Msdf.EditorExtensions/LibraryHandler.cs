using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace InitialPrefabs.Msdf.EditorExtensions {

    internal readonly ref struct LibraryScope {

        private readonly bool success;
        private readonly IntPtr lib;

        public LibraryScope(string libraryName) {
            success = LibraryHandler.GetLibraryPath(libraryName, out var path);
            lib = success ? LibraryHandler.InitializeLibrary(path) : IntPtr.Zero;
        }

        public void Dispose() {
            if (success) {
                LibraryHandler.ReleaseLibrary(lib);
            }
        }
    }

    internal static class LibraryHandler {
#if UNITY_EDITOR_WIN
        private const string Kernel32 = "kernel32";

        [DllImport(Kernel32)]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport(Kernel32)]
        private static extern IntPtr GetProcAddress(IntPtr libraryHandle, string symbolName);

        [DllImport(Kernel32)]
        private static extern bool FreeLibrary(IntPtr libraryHandle);

        public static bool GetLibraryPath(string libraryName, out string path) {
            var guids = AssetDatabase.FindAssets($"t: DefaultAsset {libraryName}");
            if (guids.Length == 0) {
                path = string.Empty;
                return false;
            }

            path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return true;
        }

        public static IntPtr InitializeLibrary(string path) {
            var handle = LoadLibrary(path);
            return handle == IntPtr.Zero ? IntPtr.Zero : handle;
        }

        public static void ReleaseLibrary(IntPtr libraryPtr) {
            Debug.Log("Closing external library");
            Assert.IsTrue(FreeLibrary(libraryPtr), "The DLL should have been unloaded!");
        }

        public static T GetDelegate<T>(IntPtr libraryPtr, string functionName) where T : class {
            var symbol = GetFunctionPointer(libraryPtr, functionName);
            return Marshal.GetDelegateForFunctionPointer(symbol, typeof(T)) as T;
        }

        public static IntPtr GetFunctionPointer(IntPtr libraryPtr, string functionName) {
            var symbol = GetProcAddress(libraryPtr, functionName);

            if (symbol == IntPtr.Zero) {
                Debug.LogError($"Could not find function: {functionName}");
                throw new InvalidOperationException($"Cannot find function: {functionName}");
            }
            return symbol;
        }
#endif
    }
}

