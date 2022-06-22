using System;

namespace Fusee.Engine.Common
{
    /// <summary>
    /// Interface for managed mesh
    /// </summary>
    public interface IManagedMesh : IDisposable
    {
        /// <summary>
        /// MeshChanged event notifies observing MeshManager about property changes and the Mesh's disposal.
        /// </summary>
        public event EventHandler<MeshChangedEventArgs> DisposeData;

        /// <summary>
        /// SessionUniqueIdentifier is used to verify a Mesh's uniqueness in the current session.
        /// </summary>
        public Suid SessionUniqueIdentifier { get; }

        /// <summary>
        /// Mesh primitive type
        /// </summary>
        PrimitiveType MeshType { get; set; }
    }
}