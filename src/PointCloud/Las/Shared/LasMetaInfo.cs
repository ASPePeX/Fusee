namespace Fusee.PointCloud.Las.Desktop
{
    /// <summary>
    /// LAS meta information
    /// </summary>
    public struct LasMetaInfo
    {
        /// <summary>
        /// The filename
        /// </summary>
        public string Filename;

        /// <summary>
        /// The point data format
        /// </summary>
        public byte PointDataFormat;

        /// <summary>
        /// The point count
        /// </summary>
        public long PointCount { get; set; }

        /// <summary>
        /// Scale factor in x
        /// </summary>
        public double ScaleFactorX;

        /// <summary>
        /// Scale factor in y
        /// </summary>
        public double ScaleFactorY;

        /// <summary>
        /// Scale factor in z
        /// </summary>
        public double ScaleFactorZ;

        /// <summary>
        /// Offset in x
        /// </summary>
        public double OffsetX;

        /// <summary>
        /// Offset in y
        /// </summary>
        public double OffsetY;

        /// <summary>
        /// Offset in z
        /// </summary>
        public double OffsetZ;
    }
}