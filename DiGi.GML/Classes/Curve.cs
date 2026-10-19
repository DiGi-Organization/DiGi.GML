using System.Collections.Generic;

namespace DiGi.GML.Classes
{
    /// <summary>
    /// Represents a GML Curve, a one-dimensional geometric primitive composed of one or more curve segments.
    /// </summary>
    public class Curve : AbstractCurve
    {
        /// <summary>
        /// Gets or sets the ordered list of segments that make up the curve.
        /// </summary>
        public List<AbstractCurveSegment>? segments { get; set; }
    }
}
