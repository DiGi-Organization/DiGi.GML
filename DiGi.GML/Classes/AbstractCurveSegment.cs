namespace DiGi.GML.Classes
{
    /// <summary>
    /// Represents an abstract base class for the segments of a GML Curve.
    /// <para>GML 3.2 curve segments carry no <c>gml:id</c>; the class derives from <see cref="AbstractGML"/> only because the type manager creates <see cref="Interfaces.IAbstractGML"/> types.</para>
    /// </summary>
    public abstract class AbstractCurveSegment : AbstractGML
    {
    }
}
