using System;
using System.Linq;
using DocumentFormat.OpenXml;
using ShapeCrawler.Shared;
using A = DocumentFormat.OpenXml.Drawing;

namespace ShapeCrawler.Shapes;

internal sealed record ShapeSize
{
    private readonly Lazy<A.Extents> aExtents;

    internal ShapeSize(OpenXmlCompositeElement sdkPShapeTreeElement)
    {
        this.aExtents = new Lazy<A.Extents>(() => sdkPShapeTreeElement.Descendants<A.Extents>().First());
    }

    internal int Height() => UnitConverter.VerticalEmuToPixel(this.aExtents.Value.Cy!);

    internal void UpdateHeight(int heightPixels) => this.aExtents.Value.Cx = UnitConverter.VerticalPixelToEmu(heightPixels);
    internal int Width() => UnitConverter.HorizontalEmuToPixel(this.aExtents.Value.Cx!);

    internal void UpdateWidth(int widthPixels) => this.aExtents.Value.Cx = UnitConverter.HorizontalPixelToEmu(widthPixels);
}