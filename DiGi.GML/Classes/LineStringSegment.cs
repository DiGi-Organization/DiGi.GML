using System.Collections.Generic;

namespace DiGi.GML.Classes
{
    /// <summary>
    /// Represents a GML LineStringSegment, a curve segment defined by a linear sequence of points with linear interpolation between them.
    /// </summary>
    public class LineStringSegment : AbstractCurveSegment
    {
        /// <summary>
        /// Gets or sets the list of coordinate positions that define the segment.
        /// </summary>
        public List<double> posList { get; set; } = [];
    }
}
