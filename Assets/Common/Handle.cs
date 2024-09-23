using System;

namespace FallShadow.Common {
    public struct Handle<T> : IComparable<Handle<T>>, IEquatable<Handle<T>> {
        public int index;
        public int version;

        public Handle(int index, int version) {
            this.index = index;
            this.version = version;
        }

        public int CompareTo(Handle<T> other) {
            var indexComparison = index.CompareTo(other.index);

            if(indexComparison != 0) {
                return indexComparison;
            }

            return version.CompareTo(other.version);
        }

        public bool Equals(Handle<T> other) {
            return index == other.index && version == other.version;
        }

        public override string ToString() {
            return $"Index: {index} Version: {version}";
        }
    }
}