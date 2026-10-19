namespace DiGi.GML.Classes
{
    /// <summary>
    /// Represents an abstract base class for one-dimensional (curve) geometric primitives within the GML specification.
    /// <para>The class is deliberately not named <c>Curve</c>: the type manager binds XML element names to class names, and an abstract class cannot be instantiated for the concrete <c>gml:Curve</c> element.</para>
    /// </summary>
    public abstract class AbstractCurve : GeometricPrimitive
    {
    }
}
