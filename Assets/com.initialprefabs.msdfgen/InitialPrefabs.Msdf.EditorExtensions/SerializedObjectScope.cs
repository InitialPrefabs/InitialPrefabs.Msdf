using UnityEditor;

namespace InitialPrefabs.Msdf.EditorExtensions {
    public readonly ref struct SerializedObjectScope {
        private readonly SerializedObject serializedObject;

        public SerializedObjectScope(SerializedObject serializedObject) {
            this.serializedObject = serializedObject;
            this.serializedObject.Update();
        }

        public void Dispose() {
            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}

