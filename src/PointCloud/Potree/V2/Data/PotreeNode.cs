using Fusee.Math.Core;
using System;

namespace Fusee.PointCloud.Potree.V2.Data
{
    public class PotreeNode
    {
        public PotreeNode()
        {
        }

        public string Name = "";

        public AABBd Aabb { get; set; }
        public PotreeNode Parent { get; set; }
        public PotreeNode[] Children = new PotreeNode[8];
        public NodeType NodeType = NodeType.UNSET;
        public long ByteOffset { get; set; }
        public long ByteSize { get; set; }
        public long NumPoints { get; set; }

        public bool IsLoaded = false;

        public int Level()
        {
            return Name.Length - 1;
        }

        public void Traverse(Action<PotreeNode> callback)
        {
            callback(this);

            foreach (var child in Children)
            {
                if (child != null)
                {
                    child.Traverse(callback);
                }
            }
        }
    }

    /// <summary>
    /// Specify node type
    /// </summary>
    public enum NodeType : int
    {
        /// <summary>
        /// Normal
        /// </summary>
        NORMAL = 0,
        /// <summary>
        /// Leaf
        /// </summary>
        LEAF = 1,
        /// <summary>
        /// Proxy
        /// </summary>
        PROXY = 2,
        /// <summary>
        /// Unset
        /// </summary>
        UNSET = -1
    }
}